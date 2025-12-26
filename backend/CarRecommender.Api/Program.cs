using CarRecommender;
using Microsoft.AspNetCore.Mvc;

// Wrap entire startup in try-catch to prevent any unhandled exceptions from crashing IIS
try
{
    // #region agent log
    try {
        try {
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".cursor", "debug.log");
            var logDir = Path.GetDirectoryName(logPath);
            if (logDir != null && !Directory.Exists(logDir)) {
                try {
                    Directory.CreateDirectory(logDir);
                } catch {
                    logPath = Path.Combine(Path.GetTempPath(), "car_recommender_debug.log");
                }
            }
            var startupLog = new {
                location = "Program.cs:Entry",
                message = "Application startup initiated",
                data = new {
                    baseDirectory = AppDomain.CurrentDomain.BaseDirectory,
                    environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "NotSet",
                    dotnetVersion = Environment.Version.ToString()
                },
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                sessionId = "debug-session",
                runId = "startup",
                hypothesisId = "A"
            };
            System.IO.File.AppendAllText(logPath, System.Text.Json.JsonSerializer.Serialize(startupLog) + Environment.NewLine);
        } catch {}
    } catch {}
    // #endregion

    var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// CONFIGURATIE LEZEN
// ============================================================================
// Lees configuratie uit appsettings.json (lokaal) of appsettings.Production.json (Azure)
// Dit zorgt ervoor dat de applicatie werkt zonder harde paden
var csvFileName = builder.Configuration["DataSettings:CsvFileName"] ?? "Cleaned_Car_Data_For_App_Fully_Enriched.csv";
var dataDirectory = builder.Configuration["DataSettings:DataDirectory"] ?? "data";

// #region agent log
try {
    var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".cursor", "debug.log");
    var logDir = Path.GetDirectoryName(logPath);
    if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);
    var baseDir = AppDomain.CurrentDomain.BaseDirectory;
    var contentRoot = builder.Environment.ContentRootPath;
    var dataPath = Path.Combine(contentRoot, dataDirectory, csvFileName);
    var dataPathAlt = Path.Combine(baseDir, dataDirectory, csvFileName);
    var configLog = new {
        location = "Program.cs:ConfigRead",
        message = "Configuration loaded",
        data = new {
            csvFileName = csvFileName,
            dataDirectory = dataDirectory,
            contentRootPath = contentRoot,
            baseDirectory = baseDir,
            dataPath = dataPath,
            dataPathAlt = dataPathAlt,
            dataFileExists = File.Exists(dataPath) || File.Exists(dataPathAlt),
            environment = builder.Environment.EnvironmentName
        },
        timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        sessionId = "debug-session",
        runId = "startup",
        hypothesisId = "B"
    };
    await System.IO.File.AppendAllTextAsync(logPath, System.Text.Json.JsonSerializer.Serialize(configLog) + Environment.NewLine);
} catch {}
// #endregion

// ============================================================================
// DEPENDENCY INJECTION CONFIGURATIE
// ============================================================================
// Deze sectie registreert alle services die gebruikt worden door de applicatie.
// Via dependency injection kunnen controllers en andere services deze gebruiken.

// Registreer ICarRepository als singleton (data wordt één keer geladen bij opstart)
// Singleton betekent dat er één instantie is voor de hele applicatie levensduur.
// Dit is efficiënt omdat we de CSV één keer inlezen en dan hergebruiken.
// Geef configuratie door aan CarRepository via factory pattern
builder.Services.AddSingleton<ICarRepository>(sp => {
    // #region agent log
    try {
        var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".cursor", "debug.log");
        var logDir = Path.GetDirectoryName(logPath);
        if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);
        var repoLog = new {
            location = "Program.cs:CarRepositoryFactory",
            message = "Creating CarRepository",
            data = new {
                csvFileName = csvFileName,
                dataDirectory = dataDirectory,
                beforeCreation = true
            },
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            sessionId = "debug-session",
            runId = "startup",
            hypothesisId = "C"
        };
        System.IO.File.AppendAllText(logPath, System.Text.Json.JsonSerializer.Serialize(repoLog) + Environment.NewLine);
    } catch {}
    // #endregion
    try {
        var repo = new CarRepository(csvFileName, dataDirectory);
        // #region agent log
        try {
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".cursor", "debug.log");
            var logDir = Path.GetDirectoryName(logPath);
            if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);
            var successLog = new {
                location = "Program.cs:CarRepositoryFactory",
                message = "CarRepository created successfully",
                data = new {
                    afterCreation = true,
                    carCount = repo.GetAllCars().Count
                },
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                sessionId = "debug-session",
                runId = "startup",
                hypothesisId = "C"
            };
            System.IO.File.AppendAllText(logPath, System.Text.Json.JsonSerializer.Serialize(successLog) + Environment.NewLine);
        } catch {}
        // #endregion
        return repo;
    } catch (Exception ex) {
        // #region agent log
        try {
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".cursor", "debug.log");
            var logDir = Path.GetDirectoryName(logPath);
            if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);
            var errorLog = new {
                location = "Program.cs:CarRepositoryFactory",
                message = "CarRepository creation failed",
                data = new {
                    exceptionType = ex.GetType().Name,
                    exceptionMessage = ex.Message,
                    exceptionStackTrace = ex.StackTrace
                },
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                sessionId = "debug-session",
                runId = "startup",
                hypothesisId = "C"
            };
            System.IO.File.AppendAllText(logPath, System.Text.Json.JsonSerializer.Serialize(errorLog) + Environment.NewLine);
        } catch {}
        // #endregion
        throw;
    }
});

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
// KRITIEK: Deze service MOET altijd geregistreerd worden, zelfs als database init faalt
// Echter: Als SQLite native libraries niet beschikbaar zijn, gebruik no-op implementatie
bool sqliteAvailable = false;
Exception? sqliteException = null;
try
{
    // Test of SQLite beschikbaar is door een test connection te maken
    using var testConnection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
    testConnection.Open();
    testConnection.Close();
    sqliteAvailable = true;
    
    // #region agent log
    try {
        try {
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".cursor", "debug.log");
            var logDir = Path.GetDirectoryName(logPath);
            if (logDir != null && !Directory.Exists(logDir)) {
                try {
                    Directory.CreateDirectory(logDir);
                } catch {
                    logPath = Path.Combine(Path.GetTempPath(), "car_recommender_debug.log");
                }
            }
            var logEntry = new {
                location = "Program.cs:SqliteAvailabilityCheck",
                message = "SQLite is available",
                data = new { },
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                sessionId = "debug-session",
                runId = "startup",
                hypothesisId = "I"
            };
            System.IO.File.AppendAllText(logPath, System.Text.Json.JsonSerializer.Serialize(logEntry) + Environment.NewLine);
        } catch {}
    } catch {}
    // #endregion
}
catch (Exception sqliteEx)
{
    sqliteAvailable = false;
    sqliteException = sqliteEx;
    
    // #region agent log
    try {
        try {
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".cursor", "debug.log");
            var logDir = Path.GetDirectoryName(logPath);
            if (logDir != null && !Directory.Exists(logDir)) {
                try {
                    Directory.CreateDirectory(logDir);
                } catch {
                    logPath = Path.Combine(Path.GetTempPath(), "car_recommender_debug.log");
                }
            }
            var logEntry = new {
                location = "Program.cs:SqliteAvailabilityCheck",
                message = "SQLite is NOT available - will use no-op implementation",
                data = new {
                    exceptionType = sqliteEx.GetType().Name,
                    exceptionMessage = sqliteEx.Message,
                    exceptionStackTrace = sqliteEx.StackTrace
                },
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                sessionId = "debug-session",
                runId = "startup",
                hypothesisId = "I"
            };
            System.IO.File.AppendAllText(logPath, System.Text.Json.JsonSerializer.Serialize(logEntry) + Environment.NewLine);
        } catch {}
    } catch {}
    // #endregion
    
    // Log warning via builder logging (sp is nog niet beschikbaar)
    try
    {
        Console.WriteLine($"Waarschuwing: SQLite is niet beschikbaar op dit systeem. User ratings functionaliteit wordt uitgeschakeld. Error: {sqliteEx.Message}");
    }
    catch { }
}

if (sqliteAvailable)
{
    builder.Services.AddSingleton<IUserRatingRepository>(sp =>
{
    // #region agent log
    try {
        try {
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".cursor", "debug.log");
            var logDir = Path.GetDirectoryName(logPath);
            if (logDir != null && !Directory.Exists(logDir)) {
                try {
                    Directory.CreateDirectory(logDir);
                } catch {
                    logPath = Path.Combine(Path.GetTempPath(), "car_recommender_debug.log");
                }
            }
            var logEntry = new {
                location = "Program.cs:UserRatingRepositoryFactory",
                message = "Creating UserRatingRepository",
                data = new {
                    beforeCreation = true
                },
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                sessionId = "debug-session",
                runId = "startup",
                hypothesisId = "F"
            };
            System.IO.File.AppendAllText(logPath, System.Text.Json.JsonSerializer.Serialize(logEntry) + Environment.NewLine);
        } catch {}
    } catch {}
    // #endregion
    
    UserRatingRepository ratingRepo = null!;
    Exception? lastException = null;
    
    // Probeer UserRatingRepository aan te maken - probeer meerdere keren met verschillende configuraties
    for (int attempt = 0; attempt < 3; attempt++)
    {
        try
        {
            // Haal database pad op uit configuratie (alleen bij eerste poging)
            string? dbPath = attempt == 0 ? builder.Configuration["DatabaseSettings:RatingsDatabasePath"] : null;
            
            // #region agent log
            try {
                try {
                    var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".cursor", "debug.log");
                    var logDir = Path.GetDirectoryName(logPath);
                    if (logDir != null && !Directory.Exists(logDir)) {
                        try {
                            Directory.CreateDirectory(logDir);
                        } catch {
                            logPath = Path.Combine(Path.GetTempPath(), "car_recommender_debug.log");
                        }
                    }
                    var logEntry = new {
                        location = "Program.cs:UserRatingRepositoryFactory",
                        message = $"Attempt {attempt + 1} to create UserRatingRepository",
                        data = new {
                            dbPath = dbPath ?? "(null)",
                            attempt = attempt + 1
                        },
                        timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                        sessionId = "debug-session",
                        runId = "startup",
                        hypothesisId = "F"
                    };
                    System.IO.File.AppendAllText(logPath, System.Text.Json.JsonSerializer.Serialize(logEntry) + Environment.NewLine);
                } catch {}
            } catch {}
            // #endregion
            
            // Probeer repository aan te maken
            // Bij poging 2: gebruik null (laat constructor zijn eigen pad bepalen)
            // Bij poging 3: gebruik in-memory fallback
            if (attempt == 2)
            {
                // Laatste poging: force in-memory database
                dbPath = ":memory:";
            }
            
            ratingRepo = new UserRatingRepository(dbPath);
            
            // #region agent log
            try {
                try {
                    var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".cursor", "debug.log");
                    var logDir = Path.GetDirectoryName(logPath);
                    if (logDir != null && !Directory.Exists(logDir)) {
                        try {
                            Directory.CreateDirectory(logDir);
                        } catch {
                            logPath = Path.Combine(Path.GetTempPath(), "car_recommender_debug.log");
                        }
                    }
                    var logEntry = new {
                        location = "Program.cs:UserRatingRepositoryFactory",
                        message = "UserRatingRepository created successfully",
                        data = new {
                            databasePath = ratingRepo.DatabasePath,
                            attempt = attempt + 1
                        },
                        timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                        sessionId = "debug-session",
                        runId = "startup",
                        hypothesisId = "F"
                    };
                    System.IO.File.AppendAllText(logPath, System.Text.Json.JsonSerializer.Serialize(logEntry) + Environment.NewLine);
                } catch {}
            } catch {}
            // #endregion
            
            // Succes! Breek uit de loop
            break;
        }
        catch (Exception repoEx)
        {
            lastException = repoEx;
            
            // #region agent log
            try {
                try {
                    var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".cursor", "debug.log");
                    var logDir = Path.GetDirectoryName(logPath);
                    if (logDir != null && !Directory.Exists(logDir)) {
                        try {
                            Directory.CreateDirectory(logDir);
                        } catch {
                            logPath = Path.Combine(Path.GetTempPath(), "car_recommender_debug.log");
                        }
                    }
                    var logEntry = new {
                        location = "Program.cs:UserRatingRepositoryFactory",
                        message = $"UserRatingRepository creation failed (attempt {attempt + 1})",
                        data = new {
                            exceptionType = repoEx.GetType().Name,
                            exceptionMessage = repoEx.Message,
                            attempt = attempt + 1
                        },
                        timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                        sessionId = "debug-session",
                        runId = "startup",
                        hypothesisId = "F"
                    };
                    System.IO.File.AppendAllText(logPath, System.Text.Json.JsonSerializer.Serialize(logEntry) + Environment.NewLine);
                } catch {}
            } catch {}
            // #endregion
            
            // Als dit de laatste poging was, gooi exception
            if (attempt == 2)
            {
                // Dit zou niet moeten gebeuren omdat constructor nu altijd moet slagen
                // Maar voor de zekerheid: log en gooi
                var logger = sp.GetService<ILogger<Program>>();
                logger?.LogCritical(repoEx, "KRITIEK: UserRatingRepository kon niet aangemaakt worden na 3 pogingen. Dit zou niet moeten gebeuren!");
                throw new InvalidOperationException("UserRatingRepository kon niet aangemaakt worden. Dit is een kritieke fout.", repoEx);
            }
        }
    }
    
    // Als we hier zijn, is ratingRepo succesvol aangemaakt
    if (ratingRepo == null)
    {
        // Dit zou niet moeten gebeuren, maar voor de zekerheid
        throw new InvalidOperationException("UserRatingRepository is null na alle pogingen", lastException);
    }
    
    // Log database locatie
    var finalLogger = sp.GetService<ILogger<Program>>();
    finalLogger?.LogInformation("User Ratings Database locatie: {DbPath}", ratingRepo.DatabasePath);
    
    // Initialiseer database asynchroon bij startup (met error handling om crashes te voorkomen)
    // Op Azure kan dit falen als er geen schrijfrechten zijn, maar dat mag de startup niet blokkeren
    _ = Task.Run(async () =>
    {
        try
        {
            // Wacht even zodat app kan starten
            await Task.Delay(2000);
            await ratingRepo.InitializeDatabaseAsync();
            finalLogger?.LogInformation("User Ratings Database succesvol geïnitialiseerd op pad: {DbPath}", ratingRepo.DatabasePath);
        }
        catch (Exception ex)
        {
            // Log error maar laat app niet crashen - collaborative filtering werkt gewoon niet
            finalLogger?.LogError(ex, "Fout bij initialiseren van User Ratings Database op pad: {DbPath}. Collaborative filtering wordt uitgeschakeld. Error: {Error}", 
                ratingRepo.DatabasePath, ex.Message);
        }
    });
    
    return ratingRepo;
});
}
else
{
    // SQLite niet beschikbaar - registreer een no-op implementatie
    // #region agent log
    try {
        try {
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".cursor", "debug.log");
            var logDir = Path.GetDirectoryName(logPath);
            if (logDir != null && !Directory.Exists(logDir)) {
                try {
                    Directory.CreateDirectory(logDir);
                } catch {
                    logPath = Path.Combine(Path.GetTempPath(), "car_recommender_debug.log");
                }
            }
            var logEntry = new {
                location = "Program.cs:NoOpUserRatingRepository",
                message = "Registering no-op UserRatingRepository (SQLite not available)",
                data = new { },
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                sessionId = "debug-session",
                runId = "startup",
                hypothesisId = "I"
            };
            System.IO.File.AppendAllText(logPath, System.Text.Json.JsonSerializer.Serialize(logEntry) + Environment.NewLine);
        } catch {}
    } catch {}
    // #endregion
    
    // Registreer een no-op implementatie die altijd lege resultaten teruggeeft
    builder.Services.AddSingleton<IUserRatingRepository>(sp => new NoOpUserRatingRepository());
}

builder.Services.AddSingleton<CollaborativeFilteringService>(sp =>
{
    // #region agent log
    try {
        try {
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".cursor", "debug.log");
            var logDir = Path.GetDirectoryName(logPath);
            if (logDir != null && !Directory.Exists(logDir)) {
                try {
                    Directory.CreateDirectory(logDir);
                } catch {
                    logPath = Path.Combine(Path.GetTempPath(), "car_recommender_debug.log");
                }
            }
            var logEntry = new {
                location = "Program.cs:CollaborativeFilteringServiceFactory",
                message = "Creating CollaborativeFilteringService",
                data = new { beforeCreation = true },
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                sessionId = "debug-session",
                runId = "startup",
                hypothesisId = "G"
            };
            System.IO.File.AppendAllText(logPath, System.Text.Json.JsonSerializer.Serialize(logEntry) + Environment.NewLine);
        } catch {}
    } catch {}
    // #endregion
    
    try
    {
        // Gebruik GetService in plaats van GetRequiredService - als repository niet beschikbaar is, 
        // maak service aan met null (service zal dan gewoon niet werken, maar app start wel)
        var ratingRepo = sp.GetService<IUserRatingRepository>();
        var carRepo = sp.GetRequiredService<ICarRepository>();
        
        // Als ratingRepo null is, maak een dummy repository aan (service moet niet null zijn)
        if (ratingRepo == null)
        {
            // #region agent log
            try {
                try {
                    var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".cursor", "debug.log");
                    var logDir = Path.GetDirectoryName(logPath);
                    if (logDir != null && !Directory.Exists(logDir)) {
                        try {
                            Directory.CreateDirectory(logDir);
                        } catch {
                            logPath = Path.Combine(Path.GetTempPath(), "car_recommender_debug.log");
                        }
                    }
                    var logEntry = new {
                        location = "Program.cs:CollaborativeFilteringServiceFactory",
                        message = "IUserRatingRepository not available, creating with fallback",
                        data = new { },
                        timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                        sessionId = "debug-session",
                        runId = "startup",
                        hypothesisId = "G"
                    };
                    System.IO.File.AppendAllText(logPath, System.Text.Json.JsonSerializer.Serialize(logEntry) + Environment.NewLine);
                } catch {}
            } catch {}
            // #endregion
            
            // Probeer opnieuw repository aan te maken met in-memory fallback
            try
            {
                ratingRepo = new UserRatingRepository(":memory:");
            }
            catch
            {
                // Als zelfs dat faalt, gooi exception - dit is echt een probleem
                throw new InvalidOperationException("Kan CollaborativeFilteringService niet aanmaken: IUserRatingRepository is niet beschikbaar en kan niet aangemaakt worden.");
            }
        }
        
        var service = new CollaborativeFilteringService(ratingRepo, carRepo);
        
        // #region agent log
        try {
            try {
                var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".cursor", "debug.log");
                var logDir = Path.GetDirectoryName(logPath);
                if (logDir != null && !Directory.Exists(logDir)) {
                    try {
                        Directory.CreateDirectory(logDir);
                    } catch {
                        logPath = Path.Combine(Path.GetTempPath(), "car_recommender_debug.log");
                    }
                }
                var logEntry = new {
                    location = "Program.cs:CollaborativeFilteringServiceFactory",
                    message = "CollaborativeFilteringService created successfully",
                    data = new { },
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    sessionId = "debug-session",
                    runId = "startup",
                    hypothesisId = "G"
                };
                System.IO.File.AppendAllText(logPath, System.Text.Json.JsonSerializer.Serialize(logEntry) + Environment.NewLine);
            } catch {}
        } catch {}
        // #endregion
        
        return service;
    }
    catch (Exception ex)
    {
        // #region agent log
        try {
            try {
                var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".cursor", "debug.log");
                var logDir = Path.GetDirectoryName(logPath);
                if (logDir != null && !Directory.Exists(logDir)) {
                    try {
                        Directory.CreateDirectory(logDir);
                    } catch {
                        logPath = Path.Combine(Path.GetTempPath(), "car_recommender_debug.log");
                    }
                }
                var logEntry = new {
                    location = "Program.cs:CollaborativeFilteringServiceFactory",
                    message = "CollaborativeFilteringService creation failed",
                    data = new {
                        exceptionType = ex.GetType().Name,
                        exceptionMessage = ex.Message
                    },
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    sessionId = "debug-session",
                    runId = "startup",
                    hypothesisId = "G"
                };
                System.IO.File.AppendAllText(logPath, System.Text.Json.JsonSerializer.Serialize(logEntry) + Environment.NewLine);
            } catch {}
        } catch {}
        // #endregion
        
        // Re-throw - dit is een kritieke fout
        throw;
    }
});
builder.Services.AddScoped<ModelRetrainingService>(sp =>
{
    var mlService = sp.GetRequiredService<MlRecommendationService>();
    var feedbackService = sp.GetRequiredService<FeedbackTrackingService>();
    var carRepo = sp.GetRequiredService<ICarRepository>();
    // Gebruik IServiceProvider in plaats van direct IRecommendationService om circulaire dependency te voorkomen
    return new ModelRetrainingService(mlService, feedbackService, carRepo, sp);
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

// #region agent log
try {
    var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".cursor", "debug.log");
    var logDir = Path.GetDirectoryName(logPath);
    if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);
    var buildLog = new {
        location = "Program.cs:BeforeBuild",
        message = "About to build application",
        data = new {
            servicesRegistered = true
        },
        timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        sessionId = "debug-session",
        runId = "startup",
        hypothesisId = "D"
    };
    await System.IO.File.AppendAllTextAsync(logPath, System.Text.Json.JsonSerializer.Serialize(buildLog) + Environment.NewLine);
} catch {}
// #endregion

WebApplication app;
try {
    app = builder.Build();
    // #region agent log
    try {
        var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".cursor", "debug.log");
        var logDir = Path.GetDirectoryName(logPath);
        if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);
        var buildSuccessLog = new {
            location = "Program.cs:AfterBuild",
            message = "Application built successfully",
            data = new {
                environment = app.Environment.EnvironmentName,
                isDevelopment = app.Environment.IsDevelopment()
            },
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            sessionId = "debug-session",
            runId = "startup",
            hypothesisId = "D"
        };
        await System.IO.File.AppendAllTextAsync(logPath, System.Text.Json.JsonSerializer.Serialize(buildSuccessLog) + Environment.NewLine);
    } catch {}
    // #endregion
} catch (Exception ex) {
    // #region agent log
    try {
        var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".cursor", "debug.log");
        var logDir = Path.GetDirectoryName(logPath);
        if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);
        var buildErrorLog = new {
            location = "Program.cs:BuildFailed",
            message = "Application build failed",
            data = new {
                exceptionType = ex.GetType().Name,
                exceptionMessage = ex.Message,
                exceptionStackTrace = ex.StackTrace
            },
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            sessionId = "debug-session",
            runId = "startup",
            hypothesisId = "D"
        };
        await System.IO.File.AppendAllTextAsync(logPath, System.Text.Json.JsonSerializer.Serialize(buildErrorLog) + Environment.NewLine);
    } catch {}
    // #endregion
    throw;
}

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
try {
    var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".cursor", "debug.log");
    var logDir = Path.GetDirectoryName(logPath);
    if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);
    var readyLog = new {
        location = "Program.cs:BeforeRun",
        message = "Application ready to run",
        data = new {
            controllersMapped = true
        },
        timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        sessionId = "debug-session",
        runId = "startup",
        hypothesisId = "E"
    };
    System.IO.File.AppendAllText(logPath, System.Text.Json.JsonSerializer.Serialize(readyLog) + Environment.NewLine);
} catch {}
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

    try {
        app.Run();
    } catch (Exception ex) {
        // #region agent log
        try {
            try {
                var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".cursor", "debug.log");
                var logDir = Path.GetDirectoryName(logPath);
                if (logDir != null && !Directory.Exists(logDir)) {
                    try {
                        Directory.CreateDirectory(logDir);
                    } catch {
                        logPath = Path.Combine(Path.GetTempPath(), "car_recommender_debug.log");
                    }
                }
                var runErrorLog = new {
                    location = "Program.cs:RunFailed",
                    message = "Application run failed",
                    data = new {
                        exceptionType = ex.GetType().Name,
                        exceptionMessage = ex.Message,
                        exceptionStackTrace = ex.StackTrace
                    },
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    sessionId = "debug-session",
                    runId = "startup",
                    hypothesisId = "E"
                };
                System.IO.File.AppendAllText(logPath, System.Text.Json.JsonSerializer.Serialize(runErrorLog) + Environment.NewLine);
            } catch {}
        } catch {}
        // #endregion
        throw;
    }
}
catch (Exception globalEx)
{
    // Global exception handler voor startup - log en re-throw zodat IIS de error ziet
    try
    {
        try
        {
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".cursor", "debug.log");
            var logDir = Path.GetDirectoryName(logPath);
            if (logDir != null && !Directory.Exists(logDir))
            {
                try
                {
                    Directory.CreateDirectory(logDir);
                }
                catch
                {
                    logPath = Path.Combine(Path.GetTempPath(), "car_recommender_debug.log");
                }
            }
            var globalErrorLog = new
            {
                location = "Program.cs:GlobalExceptionHandler",
                message = "CRITICAL: Unhandled exception during application startup",
                data = new
                {
                    exceptionType = globalEx.GetType().Name,
                    exceptionMessage = globalEx.Message,
                    exceptionStackTrace = globalEx.StackTrace,
                    innerException = globalEx.InnerException != null ? new
                    {
                        type = globalEx.InnerException.GetType().Name,
                        message = globalEx.InnerException.Message
                    } : null
                },
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                sessionId = "debug-session",
                runId = "startup",
                hypothesisId = "H"
            };
            System.IO.File.AppendAllText(logPath, System.Text.Json.JsonSerializer.Serialize(globalErrorLog) + Environment.NewLine);
        }
        catch { }
    }
    catch { }
    
    // Re-throw zodat IIS de error ziet en kan loggen
    throw;
}
