using CarRecommender;
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
var assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
if (!string.IsNullOrEmpty(assemblyLocation))
{
    var assemblyDir = Path.GetDirectoryName(assemblyLocation);
    // Van bin/Debug/net8.0/CarRecommender.Api.dll naar backend/data
    // bin/Debug/net8.0 -> bin -> Debug -> .. -> backend/CarRecommender.Api -> .. -> backend -> data
    var testPath = Path.Combine(assemblyDir ?? "", "..", "..", "..", "..", "data");
    testPath = Path.GetFullPath(testPath);
    if (Directory.Exists(testPath) && File.Exists(Path.Combine(testPath, csvFileName)))
    {
        dataDirectory = testPath;
        Console.WriteLine($"[CONFIG] Found backend/data from assembly location: {dataDirectory}");
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
builder.Services.AddScoped<IRecommendationService, RecommendationService>();

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
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:7000", "https://localhost:7001", "http://localhost:5000", "https://localhost:5001")
              .AllowAnyHeader()
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
