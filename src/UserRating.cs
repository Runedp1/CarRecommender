namespace CarRecommender;

/// <summary>
/// User rating model voor expliciete ratings van gebruikers.
/// Gebruikt voor collaborative filtering.
/// </summary>
public class UserRating
{
    /// <summary>
    /// Unieke ID van de rating.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// ID van de auto die wordt beoordeeld.
    /// </summary>
    public int CarId { get; set; }

    /// <summary>
    /// Rating waarde (1-5 sterren).
    /// </summary>
    public int Rating { get; set; }

    /// <summary>
    /// User ID (session ID of echte user ID).
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Originele prompt/tekst input van de gebruiker.
    /// Bijvoorbeeld: "Ik wil een sportieve SUV met veel vermogen"
    /// </summary>
    public string? OriginalPrompt { get; set; }

    /// <summary>
    /// User preferences die werden gebruikt voor deze recommendation.
    /// Opgeslagen als JSON string voor later matching.
    /// </summary>
    public string? UserPreferencesJson { get; set; }

    /// <summary>
    /// Timestamp wanneer de rating werd gegeven.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Context van de recommendation (text-based, similar-cars, manual-filters).
    /// </summary>
    public string? RecommendationContext { get; set; }
}

/// <summary>
/// Aggregated rating data voor collaborative filtering.
/// </summary>
public class AggregatedRating
{
    /// <summary>
    /// ID van de auto.
    /// </summary>
    public int CarId { get; set; }

    /// <summary>
    /// Gemiddelde rating (1-5).
    /// </summary>
    public double AverageRating { get; set; }

    /// <summary>
    /// Totaal aantal ratings.
    /// </summary>
    public int TotalRatings { get; set; }

    /// <summary>
    /// Aantal 5-sterren ratings.
    /// </summary>
    public int FiveStarRatings { get; set; }

    /// <summary>
    /// Aantal 4-sterren ratings.
    /// </summary>
    public int FourStarRatings { get; set; }

    /// <summary>
    /// Aantal 3-sterren ratings.
    /// </summary>
    public int ThreeStarRatings { get; set; }

    /// <summary>
    /// Aantal 2-sterren ratings.
    /// </summary>
    public int TwoStarRatings { get; set; }

    /// <summary>
    /// Aantal 1-sterren ratings.
    /// </summary>
    public int OneStarRatings { get; set; }

    /// <summary>
    /// Genormaliseerde rating score (0-1) voor ML gebruik.
    /// </summary>
    public double NormalizedRating { get; set; }
}

/// <summary>
/// User preference snapshot voor matching.
/// </summary>
public class UserPreferenceSnapshot
{
    public double? MaxBudget { get; set; }
    public string? PreferredFuel { get; set; }
    public string? PreferredBrand { get; set; }
    public bool? AutomaticTransmission { get; set; }
    public double? MinPower { get; set; }
    public string? BodyTypePreference { get; set; }
    public double ComfortVsSportScore { get; set; }
    public Dictionary<string, double> PreferenceWeights { get; set; } = new();
}





