namespace CarRecommender;

/// <summary>
/// Interface voor business logica laag (Service pattern).
/// Deze laag bevat alle recommendation logica en gebruikt ICarRepository voor data toegang.
/// 
/// Voor Azure deployment:
/// - Deze service blijft hetzelfde werken, ongeacht of data uit CSV of SQL komt
/// - Dependency injection zorgt ervoor dat de juiste repository wordt gebruikt
/// </summary>
public interface IRecommendationService
{
    /// <summary>
    /// Vindt de meest vergelijkbare auto's voor een target auto.
    /// Sorteert op similarity en geeft top N terug.
    /// </summary>
    List<RecommendationResult> RecommendSimilarCars(Car target, int n);

    /// <summary>
    /// Genereert recommendations op basis van tekst input van gebruiker.
    /// Parse tekst, filter auto's, pas gewichten aan en genereer explanations.
    /// </summary>
    List<RecommendationResult> RecommendFromText(string inputText, int n = 5);
}




