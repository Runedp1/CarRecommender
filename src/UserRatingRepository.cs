using Microsoft.Data.Sqlite;
using System.Text.Json;

namespace CarRecommender;

/// <summary>
/// SQLite implementatie van IUserRatingRepository.
/// Slaat user ratings op in een lokale database voor collaborative filtering.
/// </summary>
public class UserRatingRepository : IUserRatingRepository
{
    private string _connectionString;
    private string _dbPath;

    public UserRatingRepository(string? dbPath = null)
    {
        // #region agent log
        try
        {
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".cursor", "debug.log");
            var logDir = Path.GetDirectoryName(logPath);
            if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);
            var logEntry = System.Text.Json.JsonSerializer.Serialize(new
            {
                id = $"log_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Guid.NewGuid():N}",
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                location = "UserRatingRepository.cs:15",
                message = "UserRatingRepository constructor start",
                data = new { dbPath },
                sessionId = "debug-session",
                runId = "startup",
                hypothesisId = "A"
            });
            File.AppendAllText(logPath, logEntry + Environment.NewLine);
        }
        catch { }
        // #endregion
        try
        {
            // Standaard: gebruik database in data directory
            if (string.IsNullOrEmpty(dbPath))
            {
                // Probeer verschillende locaties
                var possiblePaths = new[]
                {
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "user_ratings.db"),
                    Path.Combine(Directory.GetCurrentDirectory(), "data", "user_ratings.db"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "backend", "data", "user_ratings.db"),
                    Path.Combine(Directory.GetCurrentDirectory(), "backend", "data", "user_ratings.db"),
                    "data/user_ratings.db"
                };

                foreach (var path in possiblePaths)
                {
                    try
                    {
                        var fullPath = Path.GetFullPath(path);
                        var dir = Path.GetDirectoryName(fullPath);
                        if (dir != null)
                        {
                            if (!Directory.Exists(dir))
                            {
                                Directory.CreateDirectory(dir);
                            }
                            _dbPath = fullPath;
                            break;
                        }
                    }
                    catch
                    {
                        // Skip deze path en probeer volgende
                        continue;
                    }
                }

                // Fallback
                if (string.IsNullOrEmpty(_dbPath))
                {
                    try
                    {
                        _dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "user_ratings.db");
                        var directory = Path.GetDirectoryName(_dbPath);
                        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }
                    }
                    catch
                    {
                        // Als alles faalt, gebruik een tijdelijke locatie
                        _dbPath = Path.Combine(Path.GetTempPath(), "user_ratings.db");
                    }
                }
            }
            else
            {
                _dbPath = dbPath;
                try
                {
                    var directory = Path.GetDirectoryName(_dbPath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                }
                catch
                {
                    // Als directory creation faalt, gebruik temp path
                    _dbPath = Path.Combine(Path.GetTempPath(), "user_ratings.db");
                }
            }

            _connectionString = $"Data Source={_dbPath}";
            
            // #region agent log
            try
            {
                var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".cursor", "debug.log");
                var logEntry = System.Text.Json.JsonSerializer.Serialize(new
                {
                    id = $"log_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Guid.NewGuid():N}",
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    location = "UserRatingRepository.cs:92",
                    message = "UserRatingRepository constructor success",
                    data = new { dbPath = _dbPath, connectionString = _connectionString },
                    sessionId = "debug-session",
                    runId = "startup",
                    hypothesisId = "A"
                });
                File.AppendAllText(logPath, logEntry + Environment.NewLine);
            }
            catch { }
            // #endregion
            
            // Log database locatie voor debugging (alleen als Console beschikbaar is)
            try
            {
                Console.WriteLine($"User Ratings Database: {_dbPath}");
            }
            catch
            {
                // Console niet beschikbaar, negeer
            }
        }
        catch (Exception ex)
        {
            // #region agent log
            try
            {
                var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".cursor", "debug.log");
                var logEntry = System.Text.Json.JsonSerializer.Serialize(new
                {
                    id = $"log_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Guid.NewGuid():N}",
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    location = "UserRatingRepository.cs:104",
                    message = "UserRatingRepository constructor exception",
                    data = new { error = ex.Message, stackTrace = ex.StackTrace },
                    sessionId = "debug-session",
                    runId = "startup",
                    hypothesisId = "A"
                });
                File.AppendAllText(logPath, logEntry + Environment.NewLine);
            }
            catch { }
            // #endregion
            // Als alles faalt, gebruik temp path als laatste redmiddel
            _dbPath = Path.Combine(Path.GetTempPath(), $"user_ratings_{Guid.NewGuid()}.db");
            _connectionString = $"Data Source={_dbPath}";
            
            try
            {
                Console.WriteLine($"User Ratings Database fallback naar temp: {_dbPath} (Error: {ex.Message})");
            }
            catch
            {
                // Negeer
            }
        }
    }
    
    /// <summary>
    /// Haalt het pad naar de database op (voor debugging/monitoring).
    /// </summary>
    public string DatabasePath => _dbPath;

    public async Task InitializeDatabaseAsync()
    {
        // #region agent log
        try
        {
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".cursor", "debug.log");
            var logEntry = System.Text.Json.JsonSerializer.Serialize(new
            {
                id = $"log_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Guid.NewGuid():N}",
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                location = "UserRatingRepository.cs:187",
                message = "InitializeDatabaseAsync start",
                data = new { dbPath = _dbPath },
                sessionId = "debug-session",
                runId = "startup",
                hypothesisId = "A"
            });
            File.AppendAllText(logPath, logEntry + Environment.NewLine);
        }
        catch { }
        // #endregion
        
        // Zorg dat directory bestaat - probeer meerdere keren met fallbacks
        string? finalDbPath = _dbPath;
        try
        {
            var directory = Path.GetDirectoryName(_dbPath);
            if (!string.IsNullOrEmpty(directory))
            {
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                // Test of we kunnen schrijven
                var testFile = Path.Combine(directory, ".test_write");
                try
                {
                    File.WriteAllText(testFile, "test");
                    File.Delete(testFile);
                }
                catch
                {
                    // Kan niet schrijven - gebruik temp
                    finalDbPath = Path.Combine(Path.GetTempPath(), $"user_ratings_{Guid.NewGuid()}.db");
                }
            }
        }
        catch (Exception dirEx)
        {
            // Als directory creation faalt, gebruik temp path
            finalDbPath = Path.Combine(Path.GetTempPath(), $"user_ratings_{Guid.NewGuid()}.db");
            try
            {
                Console.WriteLine($"Database directory creation gefaald, gebruik temp path: {finalDbPath} (Error: {dirEx.Message})");
            }
            catch { }
        }
        
        // Update connection string als pad is veranderd
        if (finalDbPath != _dbPath)
        {
            _dbPath = finalDbPath;
            _connectionString = $"Data Source={_dbPath}";
        }
        
        try
        {

            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            // Maak UserRatings tabel
            var createRatingsTable = @"
                CREATE TABLE IF NOT EXISTS UserRatings (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    CarId INTEGER NOT NULL,
                    Rating INTEGER NOT NULL CHECK (Rating >= 1 AND Rating <= 5),
                    UserId TEXT NOT NULL,
                    OriginalPrompt TEXT,
                    UserPreferencesJson TEXT,
                    RecommendationContext TEXT,
                    Timestamp DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
                )";

            using var command = new SqliteCommand(createRatingsTable, connection);
            await command.ExecuteNonQueryAsync();

            // Maak indexes (kan falen als ze al bestaan, dus apart)
            try
            {
                var createIndexes = @"
                    CREATE INDEX IF NOT EXISTS idx_carid ON UserRatings(CarId);
                    CREATE INDEX IF NOT EXISTS idx_userid ON UserRatings(UserId);
                    CREATE INDEX IF NOT EXISTS idx_timestamp ON UserRatings(Timestamp);";
                
                using var indexCommand = new SqliteCommand(createIndexes, connection);
                await indexCommand.ExecuteNonQueryAsync();
            }
            catch
            {
                // Indexes kunnen al bestaan, negeer error
            }
        }
        catch (Exception ex)
        {
            // #region agent log
            try
            {
                var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".cursor", "debug.log");
                var logEntry = System.Text.Json.JsonSerializer.Serialize(new
                {
                    id = $"log_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Guid.NewGuid():N}",
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    location = "UserRatingRepository.cs:186",
                    message = "InitializeDatabaseAsync failed",
                    data = new { error = ex.Message, stackTrace = ex.StackTrace },
                    sessionId = "debug-session",
                    runId = "startup",
                    hypothesisId = "A"
                });
                File.AppendAllText(logPath, logEntry + Environment.NewLine);
            }
            catch { }
            // #endregion
            // Log maar gooi niet - laat app niet crashen
            Console.WriteLine($"Waarschuwing: Database initialisatie gefaald: {ex.Message}");
            throw; // Re-throw zodat caller weet dat het gefaald is
        }
    }

    public async Task AddRatingAsync(UserRating rating)
    {
        // #region agent log
        try
        {
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".cursor", "debug.log");
            var logDir = Path.GetDirectoryName(logPath);
            if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);
            var logEntry = System.Text.Json.JsonSerializer.Serialize(new
            {
                id = $"log_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Guid.NewGuid():N}",
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                location = "UserRatingRepository.cs:AddRatingAsync",
                message = "AddRatingAsync start",
                data = new { carId = rating.CarId, rating = rating.Rating, dbPath = _dbPath },
                sessionId = "debug-session",
                runId = "runtime",
                hypothesisId = "E"
            });
            File.AppendAllText(logPath, logEntry + Environment.NewLine);
        }
        catch { }
        // #endregion
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            // #region agent log
            try
            {
                var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".cursor", "debug.log");
                var logEntry = System.Text.Json.JsonSerializer.Serialize(new
                {
                    id = $"log_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Guid.NewGuid():N}",
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    location = "UserRatingRepository.cs:AddRatingAsync",
                    message = "Opening database connection",
                    data = new { connectionString = _connectionString },
                    sessionId = "debug-session",
                    runId = "runtime",
                    hypothesisId = "E"
                });
                File.AppendAllText(logPath, logEntry + Environment.NewLine);
            }
            catch { }
            // #endregion
            await connection.OpenAsync();

        var insertCommand = @"
            INSERT INTO UserRatings (CarId, Rating, UserId, OriginalPrompt, UserPreferencesJson, RecommendationContext, Timestamp)
            VALUES (@CarId, @Rating, @UserId, @OriginalPrompt, @UserPreferencesJson, @RecommendationContext, @Timestamp)";

        using var command = new SqliteCommand(insertCommand, connection);
        command.Parameters.AddWithValue("@CarId", rating.CarId);
        command.Parameters.AddWithValue("@Rating", rating.Rating);
        command.Parameters.AddWithValue("@UserId", rating.UserId);
        command.Parameters.AddWithValue("@OriginalPrompt", (object?)rating.OriginalPrompt ?? DBNull.Value);
        command.Parameters.AddWithValue("@UserPreferencesJson", (object?)rating.UserPreferencesJson ?? DBNull.Value);
        command.Parameters.AddWithValue("@RecommendationContext", (object?)rating.RecommendationContext ?? DBNull.Value);
        command.Parameters.AddWithValue("@Timestamp", rating.Timestamp);

            // #region agent log
            try
            {
                var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".cursor", "debug.log");
                var logEntry = System.Text.Json.JsonSerializer.Serialize(new
                {
                    id = $"log_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Guid.NewGuid():N}",
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    location = "UserRatingRepository.cs:AddRatingAsync",
                    message = "Executing insert command",
                    data = new { },
                    sessionId = "debug-session",
                    runId = "runtime",
                    hypothesisId = "E"
                });
                File.AppendAllText(logPath, logEntry + Environment.NewLine);
            }
            catch { }
            // #endregion
            await command.ExecuteNonQueryAsync();
            // #region agent log
            try
            {
                var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".cursor", "debug.log");
                var logEntry = System.Text.Json.JsonSerializer.Serialize(new
                {
                    id = $"log_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Guid.NewGuid():N}",
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    location = "UserRatingRepository.cs:AddRatingAsync",
                    message = "AddRatingAsync success",
                    data = new { },
                    sessionId = "debug-session",
                    runId = "runtime",
                    hypothesisId = "E"
                });
                File.AppendAllText(logPath, logEntry + Environment.NewLine);
            }
            catch { }
            // #endregion
        }
        catch (Exception ex)
        {
            // #region agent log
            try
            {
                var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".cursor", "debug.log");
                var logEntry = System.Text.Json.JsonSerializer.Serialize(new
                {
                    id = $"log_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Guid.NewGuid():N}",
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    location = "UserRatingRepository.cs:AddRatingAsync",
                    message = "AddRatingAsync failed",
                    data = new { error = ex.Message, stackTrace = ex.StackTrace, type = ex.GetType().Name, dbPath = _dbPath },
                    sessionId = "debug-session",
                    runId = "runtime",
                    hypothesisId = "E"
                });
                File.AppendAllText(logPath, logEntry + Environment.NewLine);
            }
            catch { }
            // #endregion
            throw;
        }
    }

    public async Task<List<UserRating>> GetRatingsForCarAsync(int carId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var selectCommand = @"
            SELECT Id, CarId, Rating, UserId, OriginalPrompt, UserPreferencesJson, RecommendationContext, Timestamp
            FROM UserRatings
            WHERE CarId = @CarId
            ORDER BY Timestamp DESC";

        using var command = new SqliteCommand(selectCommand, connection);
        command.Parameters.AddWithValue("@CarId", carId);

        var ratings = new List<UserRating>();
        using var reader = await command.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            ratings.Add(new UserRating
            {
                Id = reader.GetInt32(0),
                CarId = reader.GetInt32(1),
                Rating = reader.GetInt32(2),
                UserId = reader.GetString(3),
                OriginalPrompt = reader.IsDBNull(4) ? null : reader.GetString(4),
                UserPreferencesJson = reader.IsDBNull(5) ? null : reader.GetString(5),
                RecommendationContext = reader.IsDBNull(6) ? null : reader.GetString(6),
                Timestamp = reader.GetDateTime(7)
            });
        }

        return ratings;
    }

    public async Task<List<UserRating>> GetRatingsForUserAsync(string userId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var selectCommand = @"
            SELECT Id, CarId, Rating, UserId, OriginalPrompt, UserPreferencesJson, RecommendationContext, Timestamp
            FROM UserRatings
            WHERE UserId = @UserId
            ORDER BY Timestamp DESC";

        using var command = new SqliteCommand(selectCommand, connection);
        command.Parameters.AddWithValue("@UserId", userId);

        var ratings = new List<UserRating>();
        using var reader = await command.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            ratings.Add(new UserRating
            {
                Id = reader.GetInt32(0),
                CarId = reader.GetInt32(1),
                Rating = reader.GetInt32(2),
                UserId = reader.GetString(3),
                OriginalPrompt = reader.IsDBNull(4) ? null : reader.GetString(4),
                UserPreferencesJson = reader.IsDBNull(5) ? null : reader.GetString(5),
                RecommendationContext = reader.IsDBNull(6) ? null : reader.GetString(6),
                Timestamp = reader.GetDateTime(7)
            });
        }

        return ratings;
    }

    public async Task<AggregatedRating?> GetAggregatedRatingForCarAsync(int carId)
    {
        // #region agent log
        try
        {
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".cursor", "debug.log");
            var logDir = Path.GetDirectoryName(logPath);
            if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);
            var logEntry = System.Text.Json.JsonSerializer.Serialize(new
            {
                id = $"log_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Guid.NewGuid():N}",
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                location = "UserRatingRepository.cs:GetAggregatedRatingForCarAsync",
                message = "GetAggregatedRatingForCarAsync start",
                data = new { carId, dbPath = _dbPath },
                sessionId = "debug-session",
                runId = "runtime",
                hypothesisId = "E"
            });
            File.AppendAllText(logPath, logEntry + Environment.NewLine);
        }
        catch { }
        // #endregion
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var aggregateCommand = @"
                SELECT 
                    CarId,
                    AVG(Rating) as AverageRating,
                    COUNT(*) as TotalRatings,
                    SUM(CASE WHEN Rating = 5 THEN 1 ELSE 0 END) as FiveStarRatings,
                    SUM(CASE WHEN Rating = 4 THEN 1 ELSE 0 END) as FourStarRatings,
                    SUM(CASE WHEN Rating = 3 THEN 1 ELSE 0 END) as ThreeStarRatings,
                    SUM(CASE WHEN Rating = 2 THEN 1 ELSE 0 END) as TwoStarRatings,
                    SUM(CASE WHEN Rating = 1 THEN 1 ELSE 0 END) as OneStarRatings
                FROM UserRatings
                WHERE CarId = @CarId
                GROUP BY CarId";

            using var command = new SqliteCommand(aggregateCommand, connection);
            command.Parameters.AddWithValue("@CarId", carId);

            using var reader = await command.ExecuteReaderAsync();
            
            if (await reader.ReadAsync())
            {
                var avgRating = reader.GetDouble(1);
                // #region agent log
                try
                {
                    var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".cursor", "debug.log");
                    var logEntry = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        id = $"log_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Guid.NewGuid():N}",
                        timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                        location = "UserRatingRepository.cs:GetAggregatedRatingForCarAsync",
                        message = "GetAggregatedRatingForCarAsync found ratings",
                        data = new { carId, avgRating, totalRatings = reader.GetInt32(2) },
                        sessionId = "debug-session",
                        runId = "runtime",
                        hypothesisId = "E"
                    });
                    File.AppendAllText(logPath, logEntry + Environment.NewLine);
                }
                catch { }
                // #endregion
                return new AggregatedRating
                {
                    CarId = carId,
                    AverageRating = avgRating,
                    TotalRatings = reader.GetInt32(2),
                    FiveStarRatings = reader.GetInt32(3),
                    FourStarRatings = reader.GetInt32(4),
                    ThreeStarRatings = reader.GetInt32(5),
                    TwoStarRatings = reader.GetInt32(6),
                    OneStarRatings = reader.GetInt32(7),
                    NormalizedRating = (avgRating - 1) / 4.0 // Normaliseer 1-5 naar 0-1
                };
            }

            // #region agent log
            try
            {
                var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".cursor", "debug.log");
                var logEntry = System.Text.Json.JsonSerializer.Serialize(new
                {
                    id = $"log_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Guid.NewGuid():N}",
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    location = "UserRatingRepository.cs:GetAggregatedRatingForCarAsync",
                    message = "GetAggregatedRatingForCarAsync no ratings found",
                    data = new { carId },
                    sessionId = "debug-session",
                    runId = "runtime",
                    hypothesisId = "E"
                });
                File.AppendAllText(logPath, logEntry + Environment.NewLine);
            }
            catch { }
            // #endregion
            return null;
        }
        catch (Exception ex)
        {
            // #region agent log
            try
            {
                var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".cursor", "debug.log");
                var logEntry = System.Text.Json.JsonSerializer.Serialize(new
                {
                    id = $"log_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Guid.NewGuid():N}",
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    location = "UserRatingRepository.cs:GetAggregatedRatingForCarAsync",
                    message = "GetAggregatedRatingForCarAsync failed",
                    data = new { error = ex.Message, stackTrace = ex.StackTrace, type = ex.GetType().Name, carId, dbPath = _dbPath },
                    sessionId = "debug-session",
                    runId = "runtime",
                    hypothesisId = "E"
                });
                File.AppendAllText(logPath, logEntry + Environment.NewLine);
            }
            catch { }
            // #endregion
            throw;
        }
    }

    public async Task<Dictionary<int, AggregatedRating>> GetAllAggregatedRatingsAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var aggregateCommand = @"
            SELECT 
                CarId,
                AVG(Rating) as AverageRating,
                COUNT(*) as TotalRatings,
                SUM(CASE WHEN Rating = 5 THEN 1 ELSE 0 END) as FiveStarRatings,
                SUM(CASE WHEN Rating = 4 THEN 1 ELSE 0 END) as FourStarRatings,
                SUM(CASE WHEN Rating = 3 THEN 1 ELSE 0 END) as ThreeStarRatings,
                SUM(CASE WHEN Rating = 2 THEN 1 ELSE 0 END) as TwoStarRatings,
                SUM(CASE WHEN Rating = 1 THEN 1 ELSE 0 END) as OneStarRatings
            FROM UserRatings
            GROUP BY CarId";

        using var command = new SqliteCommand(aggregateCommand, connection);
        var aggregated = new Dictionary<int, AggregatedRating>();

        using var reader = await command.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            var carId = reader.GetInt32(0);
            var avgRating = reader.GetDouble(1);
            
            aggregated[carId] = new AggregatedRating
            {
                CarId = carId,
                AverageRating = avgRating,
                TotalRatings = reader.GetInt32(2),
                FiveStarRatings = reader.GetInt32(3),
                FourStarRatings = reader.GetInt32(4),
                ThreeStarRatings = reader.GetInt32(5),
                TwoStarRatings = reader.GetInt32(6),
                OneStarRatings = reader.GetInt32(7),
                NormalizedRating = (avgRating - 1) / 4.0
            };
        }

        return aggregated;
    }

    public async Task<List<UserRating>> FindSimilarUserRatingsAsync(UserPreferenceSnapshot preferences, int limit = 50)
    {
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

        // Haal alle ratings op met preferences
        var selectCommand = @"
            SELECT Id, CarId, Rating, UserId, OriginalPrompt, UserPreferencesJson, RecommendationContext, Timestamp
            FROM UserRatings
            WHERE UserPreferencesJson IS NOT NULL
            ORDER BY Timestamp DESC
            LIMIT @Limit";

        using var command = new SqliteCommand(selectCommand, connection);
        command.Parameters.AddWithValue("@Limit", limit * 10); // Haal meer op voor filtering

        var allRatings = new List<UserRating>();
        using var reader = await command.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            allRatings.Add(new UserRating
            {
                Id = reader.GetInt32(0),
                CarId = reader.GetInt32(1),
                Rating = reader.GetInt32(2),
                UserId = reader.GetString(3),
                OriginalPrompt = reader.IsDBNull(4) ? null : reader.GetString(4),
                UserPreferencesJson = reader.IsDBNull(5) ? null : reader.GetString(5),
                RecommendationContext = reader.IsDBNull(6) ? null : reader.GetString(6),
                Timestamp = reader.GetDateTime(7)
            });
        }

        // Filter op gelijkaardige preferences
        var similarRatings = new List<(UserRating rating, double similarity)>();
        
        foreach (var rating in allRatings)
        {
            if (string.IsNullOrEmpty(rating.UserPreferencesJson))
                continue;

            try
            {
                var storedPrefs = JsonSerializer.Deserialize<UserPreferenceSnapshot>(rating.UserPreferencesJson);
                if (storedPrefs != null)
                {
                    var similarity = CalculatePreferenceSimilarity(preferences, storedPrefs);
                    if (similarity > 0.5) // Alleen als > 50% gelijkaardig
                    {
                        similarRatings.Add((rating, similarity));
                    }
                }
            }
            catch
            {
                // Skip invalid JSON
                continue;
            }
        }

        // Sorteer op similarity en rating, pak top N
        var result = similarRatings
            .OrderByDescending(x => x.similarity)
            .ThenByDescending(x => x.rating.Rating)
            .Take(limit)
            .Select(x => x.rating)
            .ToList();

            return result;
        }
        catch (Exception)
        {
            // Als database niet beschikbaar is, retourneer lege lijst (collaborative filtering is optioneel)
            return new List<UserRating>();
        }
    }

    public async Task<List<(int CarId, double AverageRating, int Count)>> GetTopRatedCarsForPreferencesAsync(
        UserPreferenceSnapshot preferences, 
        int limit = 10)
    {
        // Haal gelijkaardige ratings op
        var similarRatings = await FindSimilarUserRatingsAsync(preferences, limit * 5);
        
        // Groepeer per auto en bereken gemiddelde
        var carRatings = similarRatings
            .Where(r => r.Rating >= 4) // Alleen 4+ sterren
            .GroupBy(r => r.CarId)
            .Select(g => (
                CarId: g.Key,
                AverageRating: g.Average(r => r.Rating),
                Count: g.Count()
            ))
            .OrderByDescending(x => x.AverageRating)
            .ThenByDescending(x => x.Count)
            .Take(limit)
            .ToList();

        return carRatings;
    }

    /// <summary>
    /// Berekent similarity tussen twee user preference snapshots.
    /// </summary>
    private double CalculatePreferenceSimilarity(UserPreferenceSnapshot pref1, UserPreferenceSnapshot pref2)
    {
        double similarity = 0.0;
        double totalWeight = 0.0;

        // Budget similarity
        if (pref1.MaxBudget.HasValue && pref2.MaxBudget.HasValue)
        {
            var budgetDiff = Math.Abs(pref1.MaxBudget.Value - pref2.MaxBudget.Value);
            var maxBudget = Math.Max(pref1.MaxBudget.Value, pref2.MaxBudget.Value);
            var budgetSimilarity = maxBudget > 0 ? 1.0 - Math.Min(1.0, budgetDiff / maxBudget) : 0.5;
            similarity += budgetSimilarity * 0.3;
            totalWeight += 0.3;
        }

        // Fuel similarity
        if (!string.IsNullOrEmpty(pref1.PreferredFuel) && !string.IsNullOrEmpty(pref2.PreferredFuel))
        {
            var fuelSimilarity = pref1.PreferredFuel.Equals(pref2.PreferredFuel, StringComparison.OrdinalIgnoreCase) ? 1.0 : 0.0;
            similarity += fuelSimilarity * 0.2;
            totalWeight += 0.2;
        }

        // Brand similarity
        if (!string.IsNullOrEmpty(pref1.PreferredBrand) && !string.IsNullOrEmpty(pref2.PreferredBrand))
        {
            var brandSimilarity = pref1.PreferredBrand.Equals(pref2.PreferredBrand, StringComparison.OrdinalIgnoreCase) ? 1.0 : 0.0;
            similarity += brandSimilarity * 0.15;
            totalWeight += 0.15;
        }

        // Transmission similarity
        if (pref1.AutomaticTransmission.HasValue && pref2.AutomaticTransmission.HasValue)
        {
            var transSimilarity = pref1.AutomaticTransmission.Value == pref2.AutomaticTransmission.Value ? 1.0 : 0.0;
            similarity += transSimilarity * 0.1;
            totalWeight += 0.1;
        }

        // Body type similarity
        if (!string.IsNullOrEmpty(pref1.BodyTypePreference) && !string.IsNullOrEmpty(pref2.BodyTypePreference))
        {
            var bodySimilarity = pref1.BodyTypePreference.Equals(pref2.BodyTypePreference, StringComparison.OrdinalIgnoreCase) ? 1.0 : 0.0;
            similarity += bodySimilarity * 0.15;
            totalWeight += 0.15;
        }

        // Comfort vs Sport similarity
        var comfortSimilarity = 1.0 - Math.Abs(pref1.ComfortVsSportScore - pref2.ComfortVsSportScore);
        similarity += comfortSimilarity * 0.1;
        totalWeight += 0.1;

        // Normaliseer
        return totalWeight > 0 ? similarity / totalWeight : 0.0;
    }

    public async Task ClearAllRatingsAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var deleteCommand = "DELETE FROM UserRatings";
        using var command = new SqliteCommand(deleteCommand, connection);
        var deletedCount = await command.ExecuteNonQueryAsync();
        
        Console.WriteLine($"Alle ratings verwijderd: {deletedCount} entries");
    }

    public async Task ResetDatabaseAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        // Verwijder tabel
        var dropCommand = "DROP TABLE IF EXISTS UserRatings";
        using var dropCmd = new SqliteCommand(dropCommand, connection);
        await dropCmd.ExecuteNonQueryAsync();

        // Maak tabel opnieuw aan
        await InitializeDatabaseAsync();
        
        Console.WriteLine($"Database gereset: {_dbPath}");
    }

    public async Task<DatabaseStatistics> GetDatabaseStatisticsAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        // Check of tabel bestaat
        var tableExistsCommand = @"
            SELECT name FROM sqlite_master 
            WHERE type='table' AND name='UserRatings'";
        
        using var checkCmd = new SqliteCommand(tableExistsCommand, connection);
        var tableExists = await checkCmd.ExecuteScalarAsync() != null;

        if (!tableExists)
        {
            return new DatabaseStatistics
            {
                DatabasePath = _dbPath,
                TotalRatings = 0,
                UniqueCars = 0,
                UniqueUsers = 0
            };
        }

        var statsCommand = @"
            SELECT 
                COUNT(*) as TotalRatings,
                COUNT(DISTINCT CarId) as UniqueCars,
                COUNT(DISTINCT UserId) as UniqueUsers,
                MIN(Timestamp) as OldestRating,
                MAX(Timestamp) as NewestRating
            FROM UserRatings";

        using var command = new SqliteCommand(statsCommand, connection);
        using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return new DatabaseStatistics
            {
                TotalRatings = reader.GetInt32(0),
                UniqueCars = reader.GetInt32(1),
                UniqueUsers = reader.GetInt32(2),
                OldestRating = reader.IsDBNull(3) ? null : reader.GetDateTime(3),
                NewestRating = reader.IsDBNull(4) ? null : reader.GetDateTime(4),
                DatabasePath = _dbPath
            };
        }

        return new DatabaseStatistics
        {
            DatabasePath = _dbPath
        };
    }
}

