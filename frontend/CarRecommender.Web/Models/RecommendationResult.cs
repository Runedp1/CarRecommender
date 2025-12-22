namespace CarRecommender.Web.Models;

/// <summary>
/// Recommendation result model - komt overeen met de API response.
/// </summary>
public class RecommendationResult
{
    public Car Car { get; set; } = null!;
    public double SimilarityScore { get; set; }
    public string Explanation { get; set; } = string.Empty;
}


