namespace CarRecommender;

/// <summary>
/// Interface voor ML evaluatie service.
/// Biedt methodes voor train/test split, evaluatie metrics en hyperparameter tuning.
/// </summary>
public interface IMlEvaluationService
{
    /// <summary>
    /// Voert volledige ML evaluatie uit: train/test split, traint model, evalueert metrics en voert hyperparameter tuning uit.
    /// </summary>
    MlEvaluationResult EvaluateModel();
    
    /// <summary>
    /// Berekent Precision@K metric voor recommendations.
    /// Precision@K = aantal relevante items in top K / K
    /// </summary>
    double CalculatePrecisionAtK(List<RecommendationResult> recommendations, List<Car> relevantCars, int k);
    
    /// <summary>
    /// Berekent Recall@K metric voor recommendations.
    /// Recall@K = aantal relevante items in top K / totaal aantal relevante items
    /// </summary>
    double CalculateRecallAtK(List<RecommendationResult> recommendations, List<Car> relevantCars, int k);
    
    /// <summary>
    /// Berekent Mean Absolute Error (MAE) voor prijsvoorspellingen.
    /// MAE = gemiddelde absolute fout tussen voorspelde en werkelijke prijzen
    /// </summary>
    double CalculateMeanAbsoluteError(List<Car> predicted, List<Car> actual);
    
    /// <summary>
    /// Berekent Root Mean Squared Error (RMSE) voor prijsvoorspellingen.
    /// RMSE = vierkantswortel van gemiddelde kwadratische fout
    /// </summary>
    double CalculateRootMeanSquaredError(List<Car> predicted, List<Car> actual);
}







