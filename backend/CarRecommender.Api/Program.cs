using CarRecommender;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddSingleton<ICarRepository>(sp => new CarRepository(csvFileName, dataDirectory));

// Registreer feedback services voor continue learning
builder.Services.AddSingleton<IFeedbackRepository, FeedbackRepository>();
builder.Services.AddSingleton<FeedbackTrackingService>();
builder.Services.AddSingleton<MlRecommendationService>();

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
    
    var ratingRepo = new UserRatingRepository(dbPath);
    
    // Log database locatie
    var logger = sp.GetService<ILogger<Program>>();
    logger?.LogInformation("User Ratings Database locatie: {DbPath}", ((UserRatingRepository)ratingRepo).DatabasePath);
    
    // Initialiseer database asynchroon bij startup (met error handling om crashes te voorkomen)
    // Gebruik ConfigureAwait(false) om deadlocks te voorkomen
    _ = Task.Run(async () =>
    {
        try
        {
            // Wacht even zodat app kan starten
            await Task.Delay(2000).ConfigureAwait(false);
            
            // Probeer database te initialiseren met retry logic
            int retries = 3;
            for (int i = 0; i < retries; i++)
            {
                try
                {
                    await ratingRepo.InitializeDatabaseAsync().ConfigureAwait(false);
                    logger?.LogInformation("User Ratings Database succesvol geïnitialiseerd");
                    break;
                }
                catch (Exception retryEx) when (i < retries - 1)
                {
                    logger?.LogWarning(retryEx, "Database initialisatie poging {Attempt} gefaald, retry...", i + 1);
                    await Task.Delay(1000 * (i + 1)).ConfigureAwait(false);
                }
            }
        }
        catch (Exception ex)
        {
            // Log error maar laat app niet crashen
            logger?.LogError(ex, "Fout bij initialiseren van User Ratings Database na alle retries. Collaborative filtering wordt uitgeschakeld.");
        }
    });
    
    return ratingRepo;
});
builder.Services.AddSingleton<CollaborativeFilteringService>(sp =>
{
    var ratingRepo = sp.GetRequiredService<IUserRatingRepository>();
    var carRepo = sp.GetRequiredService<ICarRepository>();
    return new CollaborativeFilteringService(ratingRepo, carRepo);
});
builder.Services.AddScoped<ModelRetrainingService>(sp =>
{
    var mlService = sp.GetRequiredService<MlRecommendationService>();
    var feedbackService = sp.GetRequiredService<FeedbackTrackingService>();
    var carRepo = sp.GetRequiredService<ICarRepository>();
    var recService = sp.GetRequiredService<IRecommendationService>();
    return new ModelRetrainingService(mlService, feedbackService, carRepo, recService);
});
builder.Services.AddSingleton<ModelPerformanceMonitor>(sp =>
{
    var feedbackService = sp.GetRequiredService<FeedbackTrackingService>();
    var mlService = sp.GetRequiredService<MlRecommendationService>();
    var retrainingService = sp.GetRequiredService<ModelRetrainingService>();
    return new ModelPerformanceMonitor(feedbackService, mlService, retrainingService);
});

// Registreer IRecommendationService als scoped (één per HTTP request)
// Scoped betekent dat er één instantie is per HTTP request.
// Dit is geschikt voor services die per request gebruikt worden.
builder.Services.AddScoped<IRecommendationService>(sp =>
{
    var carRepo = sp.GetRequiredService<ICarRepository>();
    var feedbackService = sp.GetRequiredService<FeedbackTrackingService>();
    var retrainingService = sp.GetRequiredService<ModelRetrainingService>();
    var collaborativeService = sp.GetService<CollaborativeFilteringService>();
    var ratingRepo = sp.GetService<IUserRatingRepository>();
    return new RecommendationService(carRepo, feedbackService, retrainingService, collaborativeService, ratingRepo);
});

// ML Pipeline services - registreer voor ML evaluatie, hyperparameter tuning en forecasting
// HyperparameterTuningService en ForecastingService zijn stateless en kunnen als singleton
builder.Services.AddSingleton<HyperparameterTuningService>();
builder.Services.AddSingleton<ForecastingService>();

// MlEvaluationService is scoped omdat het IRecommendationService gebruikt (ook scoped)
builder.Services.AddScoped<IMlEvaluationService, MlEvaluationService>();

// Background service voor automatische retraining
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

var app = builder.Build();

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
//    az webapp deploy --name <app-name> --resource-group <resource-group> --src-path bin/Release/net9.0/publish
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

app.Run();
