using CarRecommender;
using CarRecommender.Api.Services;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// CONFIGURATIE LEZEN
// ============================================================================
// Lees configuratie uit appsettings.json (lokaal) of appsettings.Production.json (Azure)
// Dit zorgt ervoor dat de applicatie werkt zonder harde paden
var csvFileName = builder.Configuration["DataSettings:CsvFileName"] ?? "df_master_v8_def.csv";
var configuredDataDirectory = builder.Configuration["DataSettings:DataDirectory"] ?? "../data";

// Bepaal absolute pad naar backend/data directory
// Strategie: zoek vanuit verschillende locaties om backend/data te vinden
string? dataDirectory = null;

// 1. Probeer vanuit assembly locatie (runtime - deployed)
// In Azure staat het CSV bestand in data/ folder naast de DLL
var assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
if (!string.IsNullOrEmpty(assemblyLocation))
{
    var assemblyDir = Path.GetDirectoryName(assemblyLocation);
    
    // Eerst: probeer data/ folder direct naast de DLL (Azure deployment)
    var dataPathNextToDll = Path.Combine(assemblyDir ?? "", "data");
    if (Directory.Exists(dataPathNextToDll) && File.Exists(Path.Combine(dataPathNextToDll, csvFileName)))
    {
        dataDirectory = dataPathNextToDll;
        Console.WriteLine($"[CONFIG] Found data folder next to DLL (Azure): {dataDirectory}");
    }
    else
    {
        // Fallback: zoek omhoog vanuit assembly directory (development)
        // Van bin/Debug/net8.0/CarRecommender.Api.dll naar backend/data
        var testPath = Path.Combine(assemblyDir ?? "", "..", "..", "..", "..", "data");
        testPath = Path.GetFullPath(testPath);
        if (Directory.Exists(testPath) && File.Exists(Path.Combine(testPath, csvFileName)))
        {
            dataDirectory = testPath;
            Console.WriteLine($"[CONFIG] Found backend/data from assembly location: {dataDirectory}");
        }
    }
}

// 2. Probeer vanuit current working directory (development)
if (dataDirectory == null)
{
    var currentWorkingDir = Directory.GetCurrentDirectory();
    // Zoek omhoog tot we backend/data vinden
    var searchDir = currentWorkingDir;
    for (int i = 0; i < 5 && searchDir != null; i++)
    {
        var testPath = Path.Combine(searchDir, "backend", "data");
        testPath = Path.GetFullPath(testPath);
        if (Directory.Exists(testPath) && File.Exists(Path.Combine(testPath, csvFileName)))
        {
            dataDirectory = testPath;
            Console.WriteLine($"[CONFIG] Found backend/data from current directory: {dataDirectory}");
            break;
        }
        searchDir = Directory.GetParent(searchDir)?.FullName;
    }
}

// 3. Gebruik geconfigureerd pad als fallback
if (dataDirectory == null)
{
    dataDirectory = Path.IsPathRooted(configuredDataDirectory) 
        ? configuredDataDirectory 
        : Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), configuredDataDirectory));
    Console.WriteLine($"[CONFIG] Using configured data directory: {dataDirectory}");
}

// Verifieer dat het bestand bestaat
var fullPath = Path.Combine(dataDirectory, csvFileName);
Console.WriteLine($"[CONFIG] CSV File: {csvFileName}");
Console.WriteLine($"[CONFIG] Data Directory: {dataDirectory}");
Console.WriteLine($"[CONFIG] Full Path: {fullPath}");
Console.WriteLine($"[CONFIG] File Exists: {File.Exists(fullPath)}");

if (File.Exists(fullPath))
{
    var fileInfo = new FileInfo(fullPath);
    Console.WriteLine($"[CONFIG] File Size: {fileInfo.Length:N0} bytes");
    Console.WriteLine($"[CONFIG] File Last Modified: {fileInfo.LastWriteTime}");
}
else
{
    Console.WriteLine($"[WARNING] CSV file not found at: {fullPath}");
    Console.WriteLine($"[WARNING] Application may not work correctly!");
}

// ============================================================================
// DEPENDENCY INJECTION CONFIGURATIE
// ============================================================================
// Deze sectie registreert alle services die gebruikt worden door de applicatie.
// Via dependency injection kunnen controllers en andere services deze gebruiken.

// Registreer ICarRepository als singleton (data wordt één keer geladen bij opstart)
// Singleton betekent dat er één instantie is voor de hele applicatie levensduur.
// Dit is efficiënt omdat we de CSV één keer inlezen en dan hergebruiken.
// ⚠️ BELANGRIJK: Als CSV bestand wordt vervangen, moet de applicatie worden herstart!
// Geef configuratie door aan CarRepository via factory pattern
Console.WriteLine($"[DI] Initialiseren CarRepository singleton...");
Console.WriteLine($"[DI] CSV File: {csvFileName}");
Console.WriteLine($"[DI] Data Directory: {dataDirectory}");
var carRepository = new CarRepository(csvFileName, dataDirectory);
Console.WriteLine($"[DI] CarRepository singleton geïnitialiseerd met {carRepository.GetAllCars().Count} auto's");
builder.Services.AddSingleton<ICarRepository>(sp => carRepository);
// Registreer IRecommendationService als scoped (één per HTTP request)
// Scoped betekent dat er één instantie is per HTTP request.
// Dit is geschikt voor services die per request gebruikt worden.
// Geef de gedeelde MlRecommendationService singleton door aan RecommendationService


/*builder.Services.AddScoped<IRecommendationService>(sp => 
    new RecommendationService(
        sp.GetRequiredService<ICarRepository>(), 
        sp.GetRequiredService<MlRecommendationService>()));*/

// Registreer ML evaluatie services (voor /api/ml/evaluation endpoint)
// Deze services zijn nodig voor de MlController
//builder.Services.AddScoped<HyperparameterTuningService>();
//builder.Services.AddScoped<ForecastingService>();
//builder.Services.AddScoped<IMlEvaluationService, MlEvaluationService>();
//builder.Services.AddScoped<ICarRepository, CarRepository>();
//builder.Services.AddScoped<MlRecommendationService>();
builder.Services.AddScoped<AdvancedScoringService>();
builder.Services.AddScoped<KnnRecommendationService>();
builder.Services.AddScoped<IMlEvaluationService, MlEvaluationService>();
builder.Services.AddScoped<CarFeatureVectorFactory>();


builder.Services.AddScoped<RuleBasedFilter>();
builder.Services.AddScoped<SimilarityService>();
builder.Services.AddScoped<RankingService>();
builder.Services.AddScoped<IRecommendationService, RecommendationService>();



// Registreer MlRecommendationService als singleton zodat alle services dezelfde instantie gebruiken
// Dit zorgt ervoor dat het getrainde model gedeeld wordt tussen RecommendationService en de background service
// OPTIMALISATIE: Gebruik persistente locatie in Azure (D:\home\data) zodat model behouden blijft tussen deployments
// wwwroot wordt gewist bij elke deployment, maar D:\home blijft behouden
builder.Services.AddSingleton<MlRecommendationService>(sp =>
{
    var env = sp.GetRequiredService<IWebHostEnvironment>();
    
    // Standaard: gebruik ContentRootPath/data (voor lokale ontwikkeling)
    var dataDir = Path.Combine(env.ContentRootPath, "data");
    
    // Detecteer Azure App Service: ContentRootPath bevat meestal "site\wwwroot" of "site/wwwroot"
    // Of check expliciet voor D:\home\site\wwwroot patroon
    bool isAzure = env.ContentRootPath.Contains("site\\wwwroot", StringComparison.OrdinalIgnoreCase) || 
                   env.ContentRootPath.Contains("site/wwwroot", StringComparison.OrdinalIgnoreCase) ||
                   env.ContentRootPath.StartsWith(@"D:\home\", StringComparison.OrdinalIgnoreCase) ||
                   env.ContentRootPath.StartsWith(@"/home/", StringComparison.OrdinalIgnoreCase);
    
    if (isAzure)
    {
        // Azure App Service: gebruik persistente locatie D:\home\data
        // Probeer verschillende manieren om D:\home te vinden
        string? homePath = null;
        
        // Methode 1: Ga van D:\home\site\wwwroot naar D:\home (2 levels omhoog)
        if (env.ContentRootPath.Contains("site"))
        {
            var tempPath = Path.GetDirectoryName(Path.GetDirectoryName(env.ContentRootPath));
            if (!string.IsNullOrEmpty(tempPath) && Directory.Exists(tempPath) && 
                (tempPath.Contains("home") || tempPath.StartsWith(@"D:\", StringComparison.OrdinalIgnoreCase)))
            {
                homePath = tempPath;
            }
        }
        
        // Methode 2: Direct D:\home gebruiken als ContentRootPath begint met D:\home
        if (homePath == null && env.ContentRootPath.StartsWith(@"D:\home\", StringComparison.OrdinalIgnoreCase))
        {
            var parts = env.ContentRootPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (parts.Length >= 2 && parts[0].Length == 1 && parts[1].Equals("home", StringComparison.OrdinalIgnoreCase))
            {
                homePath = $"{parts[0]}:\\{parts[1]}";
                if (!Directory.Exists(homePath))
                {
                    homePath = null;
                }
            }
        }
        
        // Methode 3: Probeer expliciet D:\home
        if (homePath == null)
        {
            var explicitHomePath = @"D:\home";
            if (Directory.Exists(explicitHomePath))
            {
                homePath = explicitHomePath;
            }
        }
        
        if (!string.IsNullOrEmpty(homePath))
        {
            dataDir = Path.Combine(homePath, "data");
            Console.WriteLine($"[ML] ✅ Azure detectie: gebruik persistente data directory voor ML model: {dataDir}");
        }
        else
        {
            Console.WriteLine($"[ML] ⚠️ Azure detectie: kon D:\\home niet vinden, gebruik ContentRootPath: {dataDir}");
            Console.WriteLine($"[ML] ⚠️ ContentRootPath: {env.ContentRootPath}");
        }
    }
    else
    {
        Console.WriteLine($"[ML] Lokale omgeving: gebruik ContentRootPath voor ML model: {dataDir}");
    }
    
    // Zorg dat directory bestaat
    if (!Directory.Exists(dataDir))
    {
        try
        {
            Directory.CreateDirectory(dataDir);
            Console.WriteLine($"[ML] ML model data directory aangemaakt: {dataDir}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ML] ⚠️ Kon ML model data directory niet aanmaken: {ex.Message}");
            // Fallback naar ContentRootPath als backup niet werkt
            dataDir = Path.Combine(env.ContentRootPath, "data");
            Console.WriteLine($"[ML] Fallback naar ContentRootPath: {dataDir}");
        }
    }
    
    Console.WriteLine($"[ML] ML Model directory: {dataDir}");
    return new MlRecommendationService(dataDir);
});



// Registreer ML model training background service
// Traint ML.NET model in achtergrond na applicatie opstart (blokkeert niet)
builder.Services.AddHostedService<MlModelTrainingBackgroundService>();

// ============================================================================
// SWAGGER/OPENAPI CONFIGURATIE
// ============================================================================
// Swagger zorgt voor automatische API documentatie en test interface.
// Via Swagger UI kunnen we alle endpoints testen zonder extra tools.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ============================================================================
// CORS CONFIGURATIE
// ============================================================================
// CORS (Cross-Origin Resource Sharing) is nodig zodat de frontend (localhost:7000) 
// kan verbinden met de backend API (localhost:5283)
// In Azure: voeg frontend Azure URL toe aan WithOrigins
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        // Development origins
        var origins = new List<string>
        {
            "http://localhost:7000",
            "https://localhost:7001",
            "http://localhost:5000",
            "https://localhost:5001"
        };
        
        // Azure frontend origins (uit configuratie of hardcoded voor nu)
        // Voeg Azure frontend URL toe als die geconfigureerd is
        var azureFrontendUrl = builder.Configuration["CorsSettings:AzureFrontendUrl"];
        if (!string.IsNullOrEmpty(azureFrontendUrl))
        {
            origins.Add(azureFrontendUrl);
            origins.Add(azureFrontendUrl.Replace("https://", "http://")); // Voeg ook HTTP versie toe
        }
        
        // Voor Azure: sta ook alle azurewebsites.net subdomeinen toe (veiliger dan AllowAnyOrigin)
        if (builder.Environment.IsProduction() || builder.Configuration["CorsSettings:AllowAzureOrigins"] == "true")
        {
            // Sta alle Azure App Service frontend URLs toe (pattern: *.azurewebsites.net)
            // Dit is nodig omdat de frontend URL kan variëren per deployment
            policy.SetIsOriginAllowed(origin =>
            {
                if (string.IsNullOrEmpty(origin))
                    return false;
                
                var uri = new Uri(origin);
                // Sta localhost toe (development)
                if (uri.Host == "localhost" || uri.Host == "127.0.0.1")
                    return true;
                
                // Sta Azure App Service URLs toe (*.azurewebsites.net)
                if (uri.Host.EndsWith(".azurewebsites.net", StringComparison.OrdinalIgnoreCase))
                    return true;
                
                // Sta ook expliciet geconfigureerde origins toe
                return origins.Any(o => origin.StartsWith(o, StringComparison.OrdinalIgnoreCase));
            });
        }
        else
        {
            // Development: alleen specifieke origins
            policy.WithOrigins(origins.ToArray());
        }
        
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// ============================================================================
// CONTROLLERS CONFIGURATIE
// ============================================================================
// Voeg controllers toe zodat ASP.NET Core ze kan vinden en routes kan maken.
// Configureer JSON serialization voor consistente error responses
// Zorg dat camelCase wordt gebruikt (text i.p.v. Text) voor compatibiliteit met frontend
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    })
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

// Configureer request timeout voor ML evaluatie endpoint (kan lang duren)
// Azure App Service heeft standaard 230 seconden timeout, maar we configureren het expliciet
builder.Services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(options =>
{
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(5);
    options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(5);
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

    // Redirect root naar Swagger
    app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
}

// ============================================================================
// GLOBALE FOUTAFHANDELING VOOR AZURE PRODUCTION
// ============================================================================
// Deze middleware vangt onverwachte exceptions op en geeft een nette 500 response
// MOET vroeg in de pipeline staan om alle exceptions te vangen
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

// HTTPS redirect (veiligheid) - alleen lokaal, Azure handelt dit zelf af
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Serve static files (voor lokale auto-afbeeldingen uit images/ directory)
// Dit maakt afbeeldingen beschikbaar via /images/{brand}/{model}/{id}.jpg
// Zoek naar images directory: eerst in output directory, dan in backend/images
string? imagesPath = null;
var imagesCurrentDir = Directory.GetCurrentDirectory();

// 1. Probeer images in output directory (deployed)
var outputImagesPath = Path.Combine(imagesCurrentDir, "images");
if (Directory.Exists(outputImagesPath))
{
    imagesPath = outputImagesPath;
    Console.WriteLine($"[CONFIG] Static files from output directory: {imagesPath}");
}

// 2. Probeer backend/images (development)
if (imagesPath == null)
{
    var backendImagesPath = Path.Combine(imagesCurrentDir, "..", "images");
    backendImagesPath = Path.GetFullPath(backendImagesPath);
    if (Directory.Exists(backendImagesPath))
    {
        imagesPath = backendImagesPath;
        Console.WriteLine($"[CONFIG] Static files from backend/images: {imagesPath}");
    }
}

// Configureer static files
if (!string.IsNullOrEmpty(imagesPath))
{
    var staticFilesOptions = new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(imagesPath),
        RequestPath = "/images"
    };
    app.UseStaticFiles(staticFilesOptions);
}
else
{
    // Fallback: gebruik default static files (wwwroot)
    app.UseStaticFiles();
    Console.WriteLine($"[WARNING] Images directory not found, using default static files");
}

// CORS middleware - MOET vóór UseRouting() komen
app.UseCors("AllowFrontend");

// Routing moet vóór authorization komen
app.UseRouting();

// ============================================================================
// AUTHORIZATION & ENDPOINTS
// ============================================================================
// Authorization middleware (na routing, vóór endpoints)
app.UseAuthorization();

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
