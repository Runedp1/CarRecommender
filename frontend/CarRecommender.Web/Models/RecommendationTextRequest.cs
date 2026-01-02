namespace CarRecommender.Web.Models;

/// <summary>
/// Request model voor POST naar /api/recommendations/text
/// Moet overeenkomen met TextRecommendationRequest in backend
/// </summary>
public class RecommendationTextRequest
{
    /// <summary>
    /// Tekst input van de gebruiker met voorkeuren.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Aantal recommendations om terug te geven (optioneel, standaard 5).
    /// Moet nullable zijn om overeen te komen met backend.
    /// </summary>
    public int? Top { get; set; } = 5;
}


