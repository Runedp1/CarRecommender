namespace CarRecommender.Web.Models;

/// <summary>
/// Request model voor POST naar /api/recommendations/text
/// </summary>
public class RecommendationTextRequest
{
    public string Text { get; set; } = string.Empty;
    public int Top { get; set; } = 5;
}


