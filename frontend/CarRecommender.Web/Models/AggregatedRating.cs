namespace CarRecommender.Web.Models;

/// <summary>
/// Aggregated rating model - komt overeen met de API response.
/// </summary>
public class AggregatedRating
{
    public int CarId { get; set; }
    public double AverageRating { get; set; }
    public int TotalRatings { get; set; }
    public int FiveStarRatings { get; set; }
    public int FourStarRatings { get; set; }
    public int ThreeStarRatings { get; set; }
    public int TwoStarRatings { get; set; }
    public int OneStarRatings { get; set; }
    public double NormalizedRating { get; set; }
}

/// <summary>
/// Request model voor het toevoegen van een rating.
/// </summary>
public class RatingRequest
{
    public int CarId { get; set; }
    public int Rating { get; set; }
    public string? UserId { get; set; }
    public string? OriginalPrompt { get; set; }
    public string? RecommendationContext { get; set; }
}

/// <summary>
/// User rating model - komt overeen met de API response.
/// </summary>
public class UserRating
{
    public int Id { get; set; }
    public int CarId { get; set; }
    public int Rating { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? OriginalPrompt { get; set; }
    public string? RecommendationContext { get; set; }
    public DateTime Timestamp { get; set; }
}

