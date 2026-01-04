namespace CarRecommender;

/// <summary>
/// Interface voor ML evaluatie service.
/// Biedt methodes voor train/test split, evaluatie metrics, cross-validation en hyperparameter tuning.
/// </summary>
public interface IMlEvaluationService
{
    // ============================================================================
    // OUDE METHODES (behouden voor backwards compatibility)
    // ============================================================================
    
    /// <summary>
    /// Voert volledige ML evaluatie uit: train/test split, traint model, evalueert metrics.
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

    // ============================================================================
    // NIEUWE METHODES (voor cross-validation en algoritme vergelijking)
    // ============================================================================
    
    /// <summary>
    /// Voert k-fold cross-validation uit voor een specifiek algoritme.
    /// </summary>
    /// <param name="algorithmName">Naam van algoritme: "mlnet", "cosine", of "knn"</param>
    /// <param name="kFolds">Aantal folds (standaard 5)</param>
    /// <param name="topK">Aantal recommendations per query (standaard 10)</param>
    CrossValidationResult PerformCrossValidation(string algorithmName, int kFolds = 5, int topK = 10);
    
    /// <summary>
    /// Vergelijkt alle beschikbare algoritmes met cross-validation.
    /// </summary>
    /// <param name="kFolds">Aantal folds (standaard 5)</param>
    AlgorithmComparison CompareAllAlgorithms(int kFolds = 5);
}

/// <summary>
/// Resultaat van oude EvaluateModel methode (backwards compatibility)
/// </summary>
public class MlEvaluationResult
{
    public double Precision { get; set; }
    public double Recall { get; set; }
    public double MAE { get; set; }
    public double RMSE { get; set; }
    public string Message { get; set; } = string.Empty;
    
    // Extra properties voor MlController backward compatibility
    public bool IsValid { get; set; } = true;
    public string? ErrorMessage { get; set; }
    public int TrainingSetSize { get; set; }
    public int TestSetSize { get; set; }
    public double PrecisionAtK { get; set; }
    public double RecallAtK { get; set; }
}

/// <summary>
/// Resultaat van cross-validation voor één algoritme
/// </summary>
public class CrossValidationResult
{
    public string AlgorithmName { get; set; }
    public double PrecisionAt10 { get; set; }
    public double RecallAt10 { get; set; }
    public double F1Score { get; set; }
    public double AverageResponseTimeMs { get; set; }
    public int TotalTestCases { get; set; }
    public List<FoldMetrics> FoldResults { get; set; }
}

/// <summary>
/// Metrics voor één fold in cross-validation
/// </summary>
public class FoldMetrics
{
    public int FoldNumber { get; set; }
    public double Precision { get; set; }
    public double Recall { get; set; }
    public double F1Score { get; set; }
    public int TestSize { get; set; }
}

/// <summary>
/// Vergelijking van meerdere algoritmes
/// </summary>
public class AlgorithmComparison
{
    public Dictionary<string, CrossValidationResult> Results { get; set; }
    public string BestByPrecision { get; set; }
    public string BestByRecall { get; set; }
    public string BestBySpeed { get; set; }
}