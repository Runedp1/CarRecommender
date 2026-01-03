namespace CarRecommender;

/// <summary>
/// User feedback model voor tracking van gebruikersinteracties.
/// Gebruikt voor continue learning van het ML model.
/// </summary>
public class UserFeedback
{
    /// <summary>
    /// Unieke ID van de feedback entry.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// ID van de auto waarop feedback wordt gegeven.
    /// </summary>
    public int CarId { get; set; }

    /// <summary>
    /// Type feedback actie.
    /// </summary>
    public FeedbackType FeedbackType { get; set; }

    /// <summary>
    /// Context van de recommendation (optioneel).
    /// Bijvoorbeeld: "text-based", "similar-cars", "manual-filters"
    /// </summary>
    public string? RecommendationContext { get; set; }

    /// <summary>
    /// De similarity score die de auto had toen deze werd aanbevolen.
    /// </summary>
    public double RecommendationScore { get; set; }

    /// <summary>
    /// Positie in de recommendation lijst (1 = eerste, 2 = tweede, etc.).
    /// </summary>
    public int Position { get; set; }

    /// <summary>
    /// Timestamp wanneer de feedback werd gegeven.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Session ID voor het groeperen van gerelateerde feedback.
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// User ID (optioneel, voor toekomstige personalisatie).
    /// </summary>
    public string? UserId { get; set; }
}

/// <summary>
/// Type feedback actie die een gebruiker kan uitvoeren.
/// </summary>
public enum FeedbackType
{
    /// <summary>
    /// Gebruiker heeft op de auto geklikt (bekeken).
    /// </summary>
    Click = 1,

    /// <summary>
    /// Gebruiker heeft de auto als favoriet gemarkeerd (toekomstig).
    /// </summary>
    Favorite = 2,

    /// <summary>
    /// Gebruiker heeft expliciet positieve feedback gegeven (toekomstig).
    /// </summary>
    Positive = 3,

    /// <summary>
    /// Gebruiker heeft expliciet negatieve feedback gegeven (toekomstig).
    /// </summary>
    Negative = 4,

    /// <summary>
    /// Gebruiker heeft de auto gekocht (toekomstig).
    /// </summary>
    Purchase = 5
}

/// <summary>
/// Aggregated feedback data voor ML training.
/// Bevat samengevoegde feedback per auto.
/// </summary>
public class AggregatedFeedback
{
    /// <summary>
    /// ID van de auto.
    /// </summary>
    public int CarId { get; set; }

    /// <summary>
    /// Totaal aantal clicks/views.
    /// </summary>
    public int TotalClicks { get; set; }

    /// <summary>
    /// Totaal aantal positieve feedback acties.
    /// </summary>
    public int TotalPositive { get; set; }

    /// <summary>
    /// Totaal aantal negatieve feedback acties.
    /// </summary>
    public int TotalNegative { get; set; }

    /// <summary>
    /// Gemiddelde positie in recommendation lijsten.
    /// </summary>
    public double AveragePosition { get; set; }

    /// <summary>
    /// Gemiddelde recommendation score.
    /// </summary>
    public double AverageRecommendationScore { get; set; }

    /// <summary>
    /// Click-through rate (CTR): aantal clicks / aantal keer aanbevolen.
    /// </summary>
    public double ClickThroughRate { get; set; }

    /// <summary>
    /// Genormaliseerde populairiteit score (0-1) op basis van feedback.
    /// </summary>
    public double PopularityScore { get; set; }

    /// <summary>
    /// Laatste update timestamp.
    /// </summary>
    public DateTime LastUpdated { get; set; }
}






