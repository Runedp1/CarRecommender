using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using CarRecommender;
using System.Text.Json;
using System.IO;

namespace CarRecommender.Api.Controllers;

/// <summary>
/// API Controller voor user ratings endpoints.
/// Handelt ratings af voor collaborative filtering.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class RatingsController : ControllerBase
{
    private readonly IUserRatingRepository _ratingRepository;
    private readonly CollaborativeFilteringService? _collaborativeService;
    private readonly SessionUserService? _sessionService;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<RatingsController> _logger;

    public RatingsController(
        IUserRatingRepository ratingRepository,
        IWebHostEnvironment environment,
        CollaborativeFilteringService? collaborativeService = null,
        SessionUserService? sessionService = null,
        ILogger<RatingsController>? logger = null)
    {
        _ratingRepository = ratingRepository ?? throw new ArgumentNullException(nameof(ratingRepository));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        _collaborativeService = collaborativeService;
        _sessionService = sessionService;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// POST /api/ratings
    /// Voegt een nieuwe user rating toe.
    /// 
    /// Request body:
    /// {
    ///   "carId": 1234,
    ///   "rating": 5,
    ///   "userId": "session-id-or-user-id",
    ///   "originalPrompt": "Ik wil een sportieve SUV",
    ///   "userPreferences": { ... },
    ///   "recommendationContext": "text-based"
    /// }
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddRating([FromBody] RatingRequest request)
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
                location = "RatingsController.cs:AddRating",
                message = "AddRating endpoint called",
                data = new { carId = request?.CarId, rating = request?.Rating },
                sessionId = "debug-session",
                runId = "runtime",
                hypothesisId = "E"
            });
            System.IO.File.AppendAllText(logPath, logEntry + Environment.NewLine);
        }
        catch { }
        // #endregion
        try
        {
            if (request == null || request.CarId <= 0)
            {
                return BadRequest(new { error = "CarId is verplicht en moet groter dan 0 zijn." });
            }

            if (request.Rating < 1 || request.Rating > 5)
            {
                return BadRequest(new { error = "Rating moet tussen 1 en 5 zijn." });
            }

            // Serialize user preferences als JSON
            string? preferencesJson = null;
            if (request.UserPreferences != null)
            {
                preferencesJson = JsonSerializer.Serialize(request.UserPreferences);
            }

            // Bepaal user ID: gebruik session service als beschikbaar, anders request.UserId of nieuwe GUID
            string userId;
            if (!string.IsNullOrEmpty(request.UserId))
            {
                // Gebruiker heeft expliciet user ID meegegeven
                userId = request.UserId;
            }
            else if (_sessionService != null)
            {
                // Gebruik session-based user ID (consistent per browser session)
                var sessionId = HttpContext.Session?.Id ?? HttpContext.Request.Headers["X-Session-Id"].FirstOrDefault();
                userId = _sessionService.GetOrCreateUserId(sessionId);
            }
            else
            {
                // Fallback: nieuwe GUID (elke rating krijgt nieuwe user ID)
                userId = Guid.NewGuid().ToString();
            }

            var rating = new UserRating
            {
                CarId = request.CarId,
                Rating = request.Rating,
                UserId = userId,
                OriginalPrompt = request.OriginalPrompt,
                UserPreferencesJson = preferencesJson,
                RecommendationContext = request.RecommendationContext,
                Timestamp = DateTime.UtcNow
            };

            // Gebruik AddOrUpdateRatingAsync om te voorkomen dat een gebruiker meerdere ratings kan geven voor dezelfde auto
            await _ratingRepository.AddOrUpdateRatingAsync(rating);

            _logger.LogInformation("Rating toegevoegd: CarId={CarId}, Rating={Rating}, UserId={UserId}",
                request.CarId, request.Rating, rating.UserId);

            return Ok(new { 
                message = "Rating succesvol toegevoegd.",
                ratingId = rating.Id 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fout bij toevoegen van rating");
            return StatusCode(500, new { error = "Interne serverfout bij toevoegen van rating." });
        }
    }

    /// <summary>
    /// GET /api/ratings/car/{carId}
    /// Haalt alle ratings op voor een specifieke auto.
    /// </summary>
    [HttpGet("car/{carId}")]
    [ProducesResponseType(typeof(AggregatedRating), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRatingsForCar(int carId)
    {
        try
        {
            var aggregated = await _ratingRepository.GetAggregatedRatingForCarAsync(carId);
            
            // Voorkom caching - altijd verse data
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";
            
            if (aggregated == null)
            {
                return NotFound(new { error = $"Geen ratings gevonden voor auto met ID {carId}." });
            }

            return Ok(aggregated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fout bij ophalen van ratings voor auto {CarId}", carId);
            return StatusCode(500, new { error = "Interne serverfout." });
        }
    }

    /// <summary>
    /// GET /api/ratings/user/{userId}
    /// Haalt alle ratings op van een specifieke gebruiker.
    /// </summary>
    [HttpGet("user/{userId}")]
    [ProducesResponseType(typeof(List<UserRating>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRatingsForUser(string userId)
    {
        try
        {
            var ratings = await _ratingRepository.GetRatingsForUserAsync(userId);
            return Ok(ratings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fout bij ophalen van ratings voor gebruiker {UserId}", userId);
            return StatusCode(500, new { error = "Interne serverfout." });
        }
    }

    /// <summary>
    /// GET /api/ratings/user/{userId}/car/{carId}
    /// Haalt de rating op van een specifieke gebruiker voor een specifieke auto.
    /// </summary>
    [HttpGet("user/{userId}/car/{carId}")]
    [ProducesResponseType(typeof(UserRating), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRatingForUserAndCar(string userId, int carId)
    {
        try
        {
            var rating = await _ratingRepository.GetRatingForUserAndCarAsync(userId, carId);
            
            if (rating == null)
            {
                return NotFound(new { error = $"Geen rating gevonden voor gebruiker {userId} en auto {carId}." });
            }

            return Ok(rating);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fout bij ophalen van rating voor gebruiker {UserId} en auto {CarId}", userId, carId);
            return StatusCode(500, new { error = "Interne serverfout." });
        }
    }

    /// <summary>
    /// GET /api/ratings/database/stats
    /// Haalt database statistieken op (voor testing/monitoring).
    /// </summary>
    [HttpGet("database/stats")]
    [ProducesResponseType(typeof(DatabaseStatistics), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDatabaseStats()
    {
        try
        {
            var stats = await _ratingRepository.GetDatabaseStatisticsAsync();
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fout bij ophalen van database statistieken");
            return StatusCode(500, new { error = "Interne serverfout." });
        }
    }

    /// <summary>
    /// DELETE /api/ratings/database/reset
    /// Reset de ratings database (alleen in Development mode voor veiligheid).
    /// 
    /// WAARSCHUWING: Dit verwijdert ALLE ratings en maakt de tabel opnieuw aan!
    /// Gebruik dit wanneer de dataset is verbeterd en CarIds zijn veranderd.
    /// </summary>
    [HttpDelete("database/reset")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ResetDatabase()
    {
        // Alleen in Development mode toestaan
        if (!_environment.IsDevelopment())
        {
            _logger.LogWarning("Database reset poging in Production mode - geblokkeerd");
            return StatusCode(403, new { 
                error = "Database reset is alleen toegestaan in Development mode.",
                hint = "Gebruik DELETE /api/ratings/database/clear in Production (verwijdert alleen data, niet tabel)"
            });
        }

        try
        {
            _logger.LogWarning("DATABASE RESET GESTART - Alle ratings worden verwijderd!");
            
            await _ratingRepository.ResetDatabaseAsync();
            
            _logger.LogWarning("Database succesvol gereset");
            
            return Ok(new { 
                message = "Database succesvol gereset. Alle ratings zijn verwijderd en tabel is opnieuw aangemaakt.",
                timestamp = DateTime.UtcNow,
                databasePath = ((UserRatingRepository)_ratingRepository).DatabasePath
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fout bij resetten van database");
            return StatusCode(500, new { error = "Interne serverfout bij resetten van database." });
        }
    }

    /// <summary>
    /// DELETE /api/ratings/database/clear
    /// Verwijdert alle ratings maar behoudt de tabel structuur.
    /// 
    /// Veiliger dan reset - tabel blijft bestaan.
    /// </summary>
    [HttpDelete("database/clear")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ClearAllRatings()
    {
        // Alleen in Development mode toestaan
        if (!_environment.IsDevelopment())
        {
            _logger.LogWarning("Clear ratings poging in Production mode - geblokkeerd");
            return StatusCode(403, new { 
                error = "Clear ratings is alleen toegestaan in Development mode."
            });
        }

        try
        {
            _logger.LogWarning("CLEAR RATINGS GESTART - Alle ratings worden verwijderd!");
            
            var statsBefore = await _ratingRepository.GetDatabaseStatisticsAsync();
            await _ratingRepository.ClearAllRatingsAsync();
            var statsAfter = await _ratingRepository.GetDatabaseStatisticsAsync();
            
            _logger.LogWarning("Alle ratings verwijderd. Voor: {Before}, Na: {After}", 
                statsBefore.TotalRatings, statsAfter.TotalRatings);
            
            return Ok(new { 
                message = "Alle ratings zijn succesvol verwijderd. Tabel structuur blijft behouden.",
                deletedCount = statsBefore.TotalRatings,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fout bij leegmaken van ratings");
            return StatusCode(500, new { error = "Interne serverfout." });
        }
    }
}

/// <summary>
/// Request model voor rating.
/// </summary>
public class RatingRequest
{
    public int CarId { get; set; }
    public int Rating { get; set; }
    public string? UserId { get; set; }
    public string? OriginalPrompt { get; set; }
    public UserPreferenceSnapshot? UserPreferences { get; set; }
    public string? RecommendationContext { get; set; }
}

