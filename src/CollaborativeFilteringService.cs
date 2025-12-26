using System.Text.Json;

namespace CarRecommender;

/// <summary>
/// Service voor collaborative filtering op basis van user ratings.
/// Vindt gebruikers met gelijkaardige preferences en gebruikt hun ratings voor recommendations.
/// </summary>
public class CollaborativeFilteringService
{
    private readonly IUserRatingRepository _ratingRepository;
    private readonly ICarRepository _carRepository;

    public CollaborativeFilteringService(
        IUserRatingRepository ratingRepository,
        ICarRepository carRepository)
    {
        _ratingRepository = ratingRepository ?? throw new ArgumentNullException(nameof(ratingRepository));
        _carRepository = carRepository ?? throw new ArgumentNullException(nameof(carRepository));
    }

    /// <summary>
    /// Berekent collaborative filtering score voor een auto op basis van gelijkaardige gebruikers.
    /// </summary>
    public async Task<CollaborativeScore> CalculateCollaborativeScoreAsync(
        int carId, 
        UserPreferenceSnapshot currentUserPreferences)
    {
        // #region agent log
        var startTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        try {
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".cursor", "debug.log");
            var logEntry = new {
                location = "CollaborativeFilteringService.cs:CalculateCollaborativeScoreAsync",
                message = "Method entry",
                data = new { carId = carId },
                timestamp = startTime,
                sessionId = "debug-session",
                runId = "run1",
                hypothesisId = "A"
            };
            await System.IO.File.AppendAllTextAsync(logPath, System.Text.Json.JsonSerializer.Serialize(logEntry) + Environment.NewLine);
        } catch {}
        // #endregion
        // Vind ratings van gebruikers met gelijkaardige preferences
        var similarRatings = await _ratingRepository.FindSimilarUserRatingsAsync(
            currentUserPreferences, 
            limit: 100);
        // #region agent log
        var queryEndTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        try {
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".cursor", "debug.log");
            var logEntry = new {
                location = "CollaborativeFilteringService.cs:CalculateCollaborativeScoreAsync",
                message = "Database query completed",
                data = new { carId = carId, ratingCount = similarRatings?.Count ?? 0, queryDurationMs = queryEndTime - startTime },
                timestamp = queryEndTime,
                sessionId = "debug-session",
                runId = "run1",
                hypothesisId = "A"
            };
            await System.IO.File.AppendAllTextAsync(logPath, System.Text.Json.JsonSerializer.Serialize(logEntry) + Environment.NewLine);
        } catch {}
        // #endregion

        // Filter op deze specifieke auto
        var carRatings = similarRatings
            .Where(r => r.CarId == carId)
            .ToList();

        if (carRatings.Count == 0)
        {
            return new CollaborativeScore
            {
                Score = 0.0,
                UserCount = 0,
                AverageRating = 0.0,
                HasCollaborativeData = false
            };
        }

        // Bereken gemiddelde rating van gelijkaardige gebruikers
        var averageRating = carRatings.Average(r => r.Rating);
        var normalizedRating = (averageRating - 1) / 4.0; // Normaliseer 1-5 naar 0-1

        // Hoe meer gebruikers, hoe betrouwbaarder
        var confidence = Math.Min(1.0, carRatings.Count / 10.0); // Max confidence bij 10+ ratings

        // Combineer rating met confidence
        var score = normalizedRating * confidence;

        return new CollaborativeScore
        {
            Score = score,
            UserCount = carRatings.Count,
            AverageRating = averageRating,
            HasCollaborativeData = true,
            TopRatings = carRatings
                .Where(r => r.Rating >= 4)
                .Take(5)
                .ToList()
        };
    }

    /// <summary>
    /// Genereert uitleg tekst op basis van collaborative data.
    /// Bijvoorbeeld: "Mensen die ook een sportieve SUV willen vonden deze het best"
    /// </summary>
    public string GenerateCollaborativeExplanation(
        CollaborativeScore collaborativeScore,
        UserPreferenceSnapshot preferences)
    {
        if (!collaborativeScore.HasCollaborativeData || collaborativeScore.UserCount == 0)
        {
            return string.Empty;
        }

        // Genereer preference beschrijving
        var preferenceDescription = GeneratePreferenceDescription(preferences);

        if (string.IsNullOrEmpty(preferenceDescription))
        {
            preferenceDescription = "vergelijkbare voorkeuren";
        }

        // Bepaal rating tekst
        string ratingText;
        if (collaborativeScore.AverageRating >= 4.5)
        {
            ratingText = "vonden deze uitstekend";
        }
        else if (collaborativeScore.AverageRating >= 4.0)
        {
            ratingText = "vonden deze het best";
        }
        else if (collaborativeScore.AverageRating >= 3.5)
        {
            ratingText = "vonden deze goed";
        }
        else
        {
            ratingText = "beoordeelden deze positief";
        }

        // Genereer uitleg
        if (collaborativeScore.UserCount == 1)
        {
            return $"Iemand met {preferenceDescription} {ratingText} (⭐{collaborativeScore.AverageRating:F1})";
        }
        else if (collaborativeScore.UserCount < 5)
        {
            return $"{collaborativeScore.UserCount} mensen met {preferenceDescription} {ratingText} (⭐{collaborativeScore.AverageRating:F1})";
        }
        else
        {
            return $"Veel mensen met {preferenceDescription} {ratingText} (⭐{collaborativeScore.AverageRating:F1}, {collaborativeScore.UserCount} beoordelingen)";
        }
    }

    /// <summary>
    /// Genereert een beschrijving van user preferences voor uitleg.
    /// </summary>
    private string GeneratePreferenceDescription(UserPreferenceSnapshot preferences)
    {
        var parts = new List<string>();

        // Body type
        if (!string.IsNullOrEmpty(preferences.BodyTypePreference))
        {
            var bodyType = preferences.BodyTypePreference.ToLower();
            if (bodyType == "suv")
                parts.Add("een SUV");
            else if (bodyType == "sedan")
                parts.Add("een sedan");
            else if (bodyType == "hatchback")
                parts.Add("een hatchback");
            else
                parts.Add($"een {bodyType}");
        }

        // Sportief vs comfort
        if (preferences.ComfortVsSportScore < 0.4)
        {
            parts.Add("sportieve");
        }
        else if (preferences.ComfortVsSportScore > 0.6)
        {
            parts.Add("comfortabele");
        }

        // Brandstof
        if (!string.IsNullOrEmpty(preferences.PreferredFuel))
        {
            var fuel = preferences.PreferredFuel.ToLower();
            if (fuel == "diesel")
                parts.Add("diesel");
            else if (fuel == "hybrid")
                parts.Add("hybride");
            else if (fuel == "electric")
                parts.Add("elektrische");
        }

        // Merk
        if (!string.IsNullOrEmpty(preferences.PreferredBrand))
        {
            parts.Add($"{preferences.PreferredBrand.ToUpper()}");
        }

        if (parts.Count == 0)
        {
            return string.Empty;
        }

        return string.Join(" ", parts);
    }

    /// <summary>
    /// Haalt top-rated auto's op voor specifieke preferences.
    /// </summary>
    public async Task<List<(Car Car, double Rating, int Count)>> GetTopRatedCarsForPreferencesAsync(
        UserPreferenceSnapshot preferences,
        int limit = 10)
    {
        var topRated = await _ratingRepository.GetTopRatedCarsForPreferencesAsync(preferences, limit);
        var allCars = _carRepository.GetAllCars();
        var carDict = allCars.ToDictionary(c => c.Id);

        return topRated
            .Where(tr => carDict.ContainsKey(tr.CarId))
            .Select(tr => (carDict[tr.CarId], tr.AverageRating, tr.Count))
            .ToList();
    }
}

/// <summary>
/// Collaborative filtering score voor een auto.
/// </summary>
public class CollaborativeScore
{
    /// <summary>
    /// Genormaliseerde score (0-1) op basis van gelijkaardige gebruikers.
    /// </summary>
    public double Score { get; set; }

    /// <summary>
    /// Aantal gelijkaardige gebruikers die deze auto hebben beoordeeld.
    /// </summary>
    public int UserCount { get; set; }

    /// <summary>
    /// Gemiddelde rating (1-5) van gelijkaardige gebruikers.
    /// </summary>
    public double AverageRating { get; set; }

    /// <summary>
    /// Of er collaborative data beschikbaar is.
    /// </summary>
    public bool HasCollaborativeData { get; set; }

    /// <summary>
    /// Top ratings (4+ sterren) van gelijkaardige gebruikers.
    /// </summary>
    public List<UserRating> TopRatings { get; set; } = new();
}

