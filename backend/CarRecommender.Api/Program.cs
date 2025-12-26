using CarRecommender;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

// #region agent log
static void DebugLog(string location, string message, object? data = null, string hypothesisId = "")
{
    try
    {
        var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".cursor", "debug.log");
        var logDir = Path.GetDirectoryName(logPath);
        if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
            Directory.CreateDirectory(logDir);
        var logEntry = new
        {
            id = $"log_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Guid.NewGuid():N}",
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            location = location,
            message = message,
            data = data ?? new { },
            sessionId = "debug-session",
            runId = "startup",
            hypothesisId = hypothesisId
        };
        File.AppendAllText(logPath, JsonSerializer.Serialize(logEntry) + Environment.NewLine);
    }
    catch { }
}
// #endregion

var builder = WebApplication.CreateBuilder(args);

// #region agent log
DebugLog("Program.cs:4", "Application startup begin", new { environment = builder.Environment.EnvironmentName }, "A");
// #endregion

// ============================================================================
// CONFIGURATIE LEZEN
// ============================================================================
// Lees configuratie uit appsettings.json (lokaal) of appsettings.Production.json (Azure)
// Dit zorgt ervoor dat de applicatie werkt zonder harde paden
var csvFileName = builder.Configuration["DataSettings:CsvFileName"] ?? "Cleaned_Car_Data_For_App_Fully_Enriched.csv";
var dataDirectory = builder.Configuration["DataSettings:DataDirectory"] ?? "data";

// ============================================================================
// DEPENDENCY INJECTION CONFIGURATIE
// ============================================================================
// Deze sectie registreert alle services die gebruikt worden door de applicatie.
// Via dependency injection kunnen controllers en andere services deze gebruiken.

// Registreer ICarRepository als singleton (data wordt één keer geladen bij opstart)
// Singleton betekent dat er één instantie is voor de hele applicatie levensduur.
// Dit is efficiënt omdat we de CSV één keer inlezen en dan hergebruiken.
// Geef configuratie door aan CarRepository via factory pattern
builder.Services.AddSingleton<ICarRepository>(sp =>
{
    // #region agent log
    DebugLog("Program.cs:24", "CarRepository creation start", new { csvFileName, dataDirectory }, "B");
    // #endregion
    try
    {
        var repo = new CarRepository(csvFileName, dataDirectory);
        // #region agent log
        DebugLog("Program.cs:24", "CarRepository creation success", new { carCount = repo.GetAllCars().Count }, "B");
        // #endregion
        return repo;
    }
    catch (Exception ex)
    {
        // #region agent log
        DebugLog("Program.cs:24", "CarRepository creation failed", new { error = ex.Message, stackTrace = ex.StackTrace }, "B");
        // #endregion
        throw;
    }
});

// Registreer feedback services voor continue learning
builder.Services.AddSingleton<IFeedbackRepository, FeedbackRepository>();
builder.Services.AddSingleton<FeedbackTrackingService>();
// #region agent log
DebugLog("Program.cs:77", "MlRecommendationService registration start", null, "C");
// #endregion
builder.Services.AddSingleton<MlRecommendationService>(sp =>
{
    // #region agent log
    DebugLog("Program.cs:80", "MlRecommendationService factory start", null, "C");
    // #endregion
    try
    {
        var service = new MlRecommendationService();
        // #region agent log
        DebugLog("Program.cs:80", "MlRecommendationService factory success", null, "C");
        // #endregion
        return service;
    }
    catch (Exception ex)
    {
        // #region agent log
        DebugLog("Program.cs:80", "MlRecommendationService factory failed", new { error = ex.Message, stackTrace = ex.StackTrace, type = ex.GetType().Name }, "C");
        // #endregion
        throw;
    }
});

// Registreer session service voor user ID management (GEEN echte authentication)
builder.Services.AddSingleton<SessionUserService>();

// Registreer session middleware (voor session-based user IDs)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(24); // Session blijft 24 uur actief
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Registreer user rating services voor collaborative filtering
builder.Services.AddSingleton<IUserRatingRepository>(sp =>
{
    // #region agent log
    DebugLog("Program.cs:44", "UserRatingRepository registration start", null, "A");
    // #endregion
    // Haal database pad op uit configuratie
    // In Azure: gebruik HOME environment variable voor persistent storage
    var dbPath = builder.Configuration["DatabaseSettings:RatingsDatabasePath"];
    
    // Als geen pad is opgegeven, gebruik standaard locatie
    if (string.IsNullOrEmpty(dbPath))
    {
        // Probeer Azure HOME directory eerst (persistent storage)
        var homePath = Environment.GetEnvironmentVariable("HOME");
        if (!string.IsNullOrEmpty(homePath))
        {
            dbPath = Path.Combine(homePath, "data", "user_ratings.db");
        }
        else
        {
            // Fallback naar lokale data directory
            dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "user_ratings.db");
        }
    }
    
    // #region agent log
    DebugLog("Program.cs:44", "Database path determined", new { dbPath, homeEnv = Environment.GetEnvironmentVariable("HOME") }, "A");
    // #endregion
    
    UserRatingRepository ratingRepo;
    try
    {
        // #region agent log
        DebugLog("Program.cs:69", "UserRatingRepository constructor call start", new { dbPath }, "A");
        // #endregion
        ratingRepo = new UserRatingRepository(dbPath);
        
        // #region agent log
        DebugLog("Program.cs:69", "UserRatingRepository constructor success", new { databasePath = ratingRepo.DatabasePath }, "A");
        // #endregion
        
        // Log database locatie
        var logger = sp.GetService<ILogger<Program>>();
        logger?.LogInformation("User Ratings Database locatie: {DbPath}", ratingRepo.DatabasePath);
    }
    catch (Exception ex)
    {
        // #region agent log
        DebugLog("Program.cs:75", "UserRatingRepository constructor failed", new { error = ex.Message, stackTrace = ex.StackTrace }, "A");
        // #endregion
        // Als repository creation faalt, gebruik fallback met temp path
        var logger = sp.GetService<ILogger<Program>>();
        logger?.LogWarning(ex, "UserRatingRepository creation gefaald, gebruik fallback");
        ratingRepo = new UserRatingRepository(Path.Combine(Path.GetTempPath(), "user_ratings.db"));
    }
    
    // Initialiseer database asynchroon bij startup (met error handling om crashes te voorkomen)
    // Gebruik ConfigureAwait(false) om deadlocks te voorkomen
    // BELANGRIJK: Als database init faalt, laat app gewoon doorgaan zonder user ratings
    _ = Task.Run(async () =>
    {
        try
        {
            // #region agent log
            DebugLog("Program.cs:85", "Database init task started", null, "A");
            // #endregion
            // Wacht even zodat app kan starten
            await Task.Delay(3000).ConfigureAwait(false);
            
            // #region agent log
            DebugLog("Program.cs:90", "Starting database initialization", null, "A");
            // #endregion
            // Probeer database te initialiseren met retry logic
            int retries = 2; // Minder retries om sneller te falen
            for (int i = 0; i < retries; i++)
            {
                try
                {
                    // #region agent log
                    DebugLog("Program.cs:98", "Database init attempt", new { attempt = i + 1, maxRetries = retries }, "A");
                    // #endregion
                    await ratingRepo.InitializeDatabaseAsync().ConfigureAwait(false);
                    // #region agent log
                    DebugLog("Program.cs:98", "Database init success", new { attempt = i + 1 }, "A");
                    // #endregion
                    var logger = sp.GetService<ILogger<Program>>();
                    logger?.LogInformation("User Ratings Database succesvol geïnitialiseerd");
                    return; // Success - exit
                }
                catch (Exception retryEx) when (i < retries - 1)
                {
                    // #region agent log
                    DebugLog("Program.cs:102", "Database init retry", new { attempt = i + 1, error = retryEx.Message }, "A");
                    // #endregion
                    var logger = sp.GetService<ILogger<Program>>();
                    logger?.LogWarning(retryEx, "Database initialisatie poging {Attempt} gefaald, retry...", i + 1);
                    await Task.Delay(2000).ConfigureAwait(false); // Kortere delay
                }
                catch (Exception finalEx)
                {
                    // #region agent log
                    DebugLog("Program.cs:102", "Database init final failure", new { attempt = i + 1, error = finalEx.Message }, "A");
                    // #endregion
                    var logger = sp.GetService<ILogger<Program>>();
                    logger?.LogWarning(finalEx, "User Ratings Database initialisatie gefaald na alle retries. App blijft werken zonder user ratings.");
                    return; // Fail gracefully - app blijft werken
                }
            }
        }
        catch (Exception ex)
        {
            // #region agent log
            DebugLog("Program.cs:109", "Database init task failed", new { error = ex.Message, stackTrace = ex.StackTrace }, "A");
            // #endregion
            // Log error maar laat app niet crashen - app blijft werken zonder user ratings
            var logger = sp.GetService<ILogger<Program>>();
            logger?.LogWarning(ex, "User Ratings Database initialisatie gefaald. App blijft werken zonder user ratings.");
        }
    });
    
    return ratingRepo;
});
builder.Services.AddSingleton<CollaborativeFilteringService>(sp =>
{
    try
    {
        var ratingRepo = sp.GetService<IUserRatingRepository>();
        var carRepo = sp.GetRequiredService<ICarRepository>();
        // Als ratingRepo niet beschikbaar is, return null (wordt later opgevangen)
        if (ratingRepo == null)
        {
            // #region agent log
            DebugLog("Program.cs:240", "CollaborativeFilteringService: ratingRepo not available, skipping", null, "A");
            // #endregion
            return null!; // Return null, maar markeer als non-nullable voor DI
        }
        return new CollaborativeFilteringService(ratingRepo, carRepo);
    }
    catch (Exception ex)
    {
        // #region agent log
        DebugLog("Program.cs:240", "CollaborativeFilteringService creation failed", new { error = ex.Message }, "A");
        // #endregion
        return null!; // Return null, maar markeer als non-nullable voor DI
    }
});
// ModelRetrainingService wordt NIET geregistreerd om circular dependency te voorkomen
// RecommendationService maakt ModelRetrainingService optioneel, dus de app kan zonder werken
// #region agent log
DebugLog("Program.cs:264", "Skipping ModelRetrainingService registration - circular dependency with IRecommendationService", null, "D");
// #endregion
// ModelPerformanceMonitor wordt NIET geregistreerd omdat ModelRetrainingService niet beschikbaar is
// #region agent log
DebugLog("Program.cs:270", "Skipping ModelPerformanceMonitor registration - ModelRetrainingService not available", null, "D");
// #endregion

// Registreer IRecommendationService als scoped (één per HTTP request)
// Scoped betekent dat er één instantie is per HTTP request.
// Dit is geschikt voor services die per request gebruikt worden.
builder.Services.AddScoped<IRecommendationService>(sp =>
{
    // #region agent log
    DebugLog("Program.cs:143", "RecommendationService registration start", null, "C");
    // #endregion
    try
    {
        var carRepo = sp.GetRequiredService<ICarRepository>();
        var feedbackService = sp.GetRequiredService<FeedbackTrackingService>();
        // ModelRetrainingService is niet beschikbaar (circular dependency) - RecommendationService maakt het optioneel
        var retrainingService = (ModelRetrainingService?)null;
        var collaborativeService = sp.GetService<CollaborativeFilteringService>();
        var ratingRepo = sp.GetService<IUserRatingRepository>();
        // #region agent log
        DebugLog("Program.cs:143", "RecommendationService dependencies resolved", null, "C");
        // #endregion
        return new RecommendationService(carRepo, feedbackService, retrainingService, collaborativeService, ratingRepo);
    }
    catch (Exception ex)
    {
        // #region agent log
        DebugLog("Program.cs:143", "RecommendationService registration failed", new { error = ex.Message, stackTrace = ex.StackTrace }, "C");
        // #endregion
        throw;
    }
});

// ML Pipeline services - registreer voor ML evaluatie, hyperparameter tuning en forecasting
// HyperparameterTuningService en ForecastingService zijn stateless en kunnen als singleton
builder.Services.AddSingleton<HyperparameterTuningService>();
builder.Services.AddSingleton<ForecastingService>();

// MlEvaluationService is scoped omdat het IRecommendationService gebruikt (ook scoped)
builder.Services.AddScoped<IMlEvaluationService, MlEvaluationService>();

// Background service voor automatische retraining
// #region agent log
DebugLog("Program.cs:162", "RetrainingBackgroundService registration", null, "D");
// #endregion
builder.Services.AddHostedService<CarRecommender.Api.Services.RetrainingBackgroundService>();

// ============================================================================
// CORS CONFIGURATIE
// ============================================================================
// CORS is nodig zodat de frontend (lokaal en Azure) kan communiceren met de API
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                "http://localhost:7000", 
                "https://localhost:7001",
                "https://pp-carrecommender-web-dev.azurewebsites.net"
              )
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// ============================================================================
// SWAGGER/OPENAPI CONFIGURATIE
// ============================================================================
// Swagger zorgt voor automatische API documentatie en test interface.
// Via Swagger UI kunnen we alle endpoints testen zonder extra tools.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ============================================================================
// CONTROLLERS CONFIGURATIE
// ============================================================================
// Voeg controllers toe zodat ASP.NET Core ze kan vinden en routes kan maken.
// Configureer JSON serialization voor consistente error responses
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        // Standaard foutafhandeling voor model validation errors
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .SelectMany(x => x.Value!.Errors.Select(e => e.ErrorMessage))
                .ToList();
            
            return new BadRequestObjectResult(new { error = "Ongeldige request parameters.", details = errors });
        };
    });

// ============================================================================
// LOGGING CONFIGURATIE VOOR AZURE
// ============================================================================
// In Production loggen we alleen belangrijke fouten (Warning en Error)
// Dit voorkomt te veel log output in Azure App Service logs
// Development mode logt meer details voor debugging

// #region agent log
DebugLog("Program.cs:218", "Building application", null, "A");
// #endregion
try
{
    var app = builder.Build();
    // #region agent log
    DebugLog("Program.cs:218", "Application built successfully", null, "A");
    // #endregion

    // ============================================================================
    // HTTP REQUEST PIPELINE CONFIGURATIE
    // ============================================================================
    // Deze sectie configureert hoe HTTP requests worden verwerkt.

    // In development mode: toon Swagger UI voor API testing
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // HTTPS redirect (veiligheid) - alleen lokaal, Azure handelt dit zelf af
    if (app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }

    // Session middleware (voor session-based user IDs)
    app.UseSession();

    // CORS middleware - moet vóór UseRouting/MapControllers komen
    app.UseCors();

    // Serve static files (voor lokale auto-afbeeldingen uit images/ directory)
    // Dit maakt afbeeldingen beschikbaar via /images/{filename}.jpg
    app.UseStaticFiles();

    // Serve images from backend/images directory
    // Probeer verschillende locaties (lokaal en Azure)
    string[] possibleImagePaths = new[]
    {
        Path.Combine(builder.Environment.ContentRootPath, "..", "backend", "images"), // Lokaal development
        Path.Combine(builder.Environment.ContentRootPath, "images"), // Azure deployment
        Path.Combine(builder.Environment.ContentRootPath, "backend", "images"), // Alternatief lokaal
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "images"), // Azure runtime
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "backend", "images") // Alternatief Azure
    };

    string? imagesPath = null;
    foreach (var path in possibleImagePaths)
    {
        if (Directory.Exists(path))
        {
            imagesPath = path;
            break;
        }
    }

    if (!string.IsNullOrEmpty(imagesPath))
    {
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(imagesPath),
            RequestPath = "/images"
        });
        Console.WriteLine($"Images directory geconfigureerd: {imagesPath}");
    }
    else
    {
        Console.WriteLine($"Waarschuwing: Images directory niet gevonden. Gezocht in:");
        foreach (var path in possibleImagePaths)
        {
            Console.WriteLine($"  - {path}");
        }
    }

    // ============================================================================
    // GLOBALE FOUTAFHANDELING VOOR AZURE PRODUCTION
    // ============================================================================
    // Deze middleware vangt onverwachte exceptions op en geeft een nette 500 response
    // In Production loggen we de echte exception maar geven we geen details aan de client
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";

            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            var exceptionHandlerFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
            
            if (exceptionHandlerFeature?.Error != null)
            {
                var exception = exceptionHandlerFeature.Error;
                
                // Log de echte exception voor debugging (alleen in Azure logs, niet naar client)
                logger.LogError(exception, "Onverwachte fout opgetreden tijdens verwerking van request: {Path}", 
                    context.Request.Path);
                
                // Geef generieke foutmelding aan client (veiligheid)
                var response = new { error = "Er is een interne serverfout opgetreden. Probeer het later opnieuw." };
                await context.Response.WriteAsJsonAsync(response);
            }
        });
    });

// ============================================================================
// ROUTING CONFIGURATIE
// ============================================================================
    // Map controllers naar routes (bijv. /api/cars, /api/recommendations)
    // Deze routes werken zowel in Development als Production
    app.MapControllers();

    // #region agent log
    DebugLog("Program.cs:322", "Controllers mapped, app ready to run", null, "A");
    // #endregion

    // ============================================================================
    // AZURE DEPLOYMENT NOTES
    // ============================================================================
// Om deze API naar Azure App Service te publiceren:
// 
// 1. Publiceer via Visual Studio:
//    - Rechtsklik op CarRecommender.Api project → Publish
//    - Selecteer Azure App Service
//    - Maak nieuwe App Service of selecteer bestaande
//    - Klik Publish
//
// 2. Publiceer via Azure CLI:
//    dotnet publish -c Release
//    az webapp deploy --name <app-name> --resource-group <resource-group> --src-path bin/Release/net8.0/publish
//
// 3. Voor productie:
//    - Zet app.Environment.IsDevelopment() op false of gebruik appsettings.Production.json
//    - Configureer connection strings voor Azure SQL Database (als je die gebruikt)
//    - Stel Application Insights in voor monitoring
//    - Configureer CORS als je een frontend hebt op een ander domein
//
// 4. Data migratie naar Azure SQL:
//    - Maak een nieuwe SqlCarRepository die ICarRepository implementeert
//    - Registreer SqlCarRepository in plaats van CarRepository hierboven
//    - De rest van de code blijft hetzelfde werken!

    // #region agent log
    DebugLog("Program.cs:519", "Starting application", null, "A");
    // #endregion
    app.Run();
}
catch (Exception ex)
{
    // #region agent log
    DebugLog("Program.cs:523", "Application startup failed", new { error = ex.Message, stackTrace = ex.StackTrace, type = ex.GetType().Name }, "A");
    // #endregion
    throw;
}
