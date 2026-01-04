using Microsoft.AspNetCore.Mvc;
using CarRecommender;

namespace CarRecommender.Api.Controllers;

/// <summary>
/// API Controller voor ML evaluatie endpoints.
/// Biedt endpoints voor het ophalen van ML evaluatie resultaten, metrics en forecasting data.
/// 
/// Deze controller is onderdeel van de ML pipeline en biedt toegang tot:
/// - Evaluatie metrics (Precision@K, Recall@K, MAE, RMSE)
/// - Hyperparameter tuning resultaten
/// - Forecasting/trend analyse resultaten
/// </summary>
[ApiController]
[Route("api/ml")]
public class MlController : ControllerBase
{
    private readonly IMlEvaluationService _mlEvaluationService;
    private readonly MlRecommendationService _mlRecommendationService;
    private readonly ILogger<MlController> _logger;
    
    /// <summary>
    /// Constructor - krijgt services via dependency injection.
    /// </summary>
    public MlController(
        IMlEvaluationService mlEvaluationService,
        MlRecommendationService mlRecommendationService,
        ILogger<MlController> logger)
    {
        _mlEvaluationService = mlEvaluationService ?? throw new ArgumentNullException(nameof(mlEvaluationService));
        _mlRecommendationService = mlRecommendationService ?? throw new ArgumentNullException(nameof(mlRecommendationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    /// <summary>
    /// GET /api/ml/evaluation
    /// Haalt volledige ML evaluatie resultaten op: metrics, hyperparameter tuning en forecasting data.
    /// 
    /// Deze endpoint voert de volledige ML pipeline uit:
    /// 1. Train/test split van de dataset
    /// 2. Evaluatie van het recommendation model
    /// 3. Hyperparameter tuning
    /// 4. Forecasting/trend analyse
    /// 
    /// Response bevat:
    /// - Precision@K en Recall@K metrics (recommendation system metrics)
    /// - MAE en RMSE (prijsvoorspelling metrics)
    /// - Beste hyperparameter configuratie
    /// - Forecasting/trend analyse resultaten
    /// </summary>
    [HttpPost("evaluation")]
    [HttpGet("evaluation")] // Behoud GET voor backward compatibility
    [ProducesResponseType(typeof(MlEvaluationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetEvaluation()
    {
        try
        {
            _logger.LogInformation("ML evaluatie wordt uitgevoerd... (dit kan 30-60 seconden duren)");
            
            // ML evaluatie kan lang duren, gebruik async Task.Run om te voorkomen dat de request thread wordt geblokkeerd
            // Timeout is standaard 30 seconden in Azure, maar ML evaluatie kan langer duren
            // Gebruik async/await in plaats van GetAwaiter().GetResult() om deadlocks te voorkomen
            var result = await Task.Run(() => _mlEvaluationService.EvaluateModel());
            
            if (!result.IsValid)
            {
                _logger.LogWarning("ML evaluatie mislukt: {ErrorMessage}", result.ErrorMessage);
                return BadRequest(new { error = result.ErrorMessage ?? "ML evaluatie mislukt" });
            }
            
            _logger.LogInformation("ML evaluatie voltooid. Training set: {TrainingSize}, Test set: {TestSize}, Precision@K: {Precision}, Recall@K: {Recall}",
                result.TrainingSetSize, result.TestSetSize, result.PrecisionAtK, result.RecallAtK);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fout bij ML evaluatie");
            throw; // Exception wordt opgevangen door globale exception handler
        }
    }
    
    /// <summary>
    /// GET /api/ml/status
    /// Haalt de status op van het ML.NET model training.
    /// 
    /// Retourneert:
    /// - Of het model getraind is
    /// - Wanneer het model voor het laatst getraind is
    /// - Aantal training samples
    /// - Of het model opgeslagen is op disk
    /// </summary>
    [HttpGet("status")]
    [ProducesResponseType(typeof(MlModelStatus), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult GetStatus()
    {
        try
        {
            var statistics = _mlRecommendationService.GetModelStatistics();
            
            var status = new MlModelStatus
            {
                IsTrained = statistics.IsTrained,
                LastTrainingTime = statistics.LastTrainingTime,
                TrainingDataCount = statistics.TrainingDataCount,
                ModelExists = statistics.ModelExists,
                ModelPath = statistics.ModelPath
            };
            
            _logger.LogInformation("ML model status opgehaald. IsTrained: {IsTrained}, LastTraining: {LastTraining}", 
                status.IsTrained, status.LastTrainingTime);
            
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fout bij ophalen ML model status");
            throw; // Exception wordt opgevangen door globale exception handler
        }
    }

    /// <summary>
    /// POST /api/ml/cross-validation
    /// Voert cross-validation uit voor een specifiek algoritme.
    /// 
    /// Request body:
    /// {
    ///   "algorithmName": "mlnet" | "cosine" | "knn",
    ///   "kFolds": 5,
    ///   "topK": 10
    /// }
    /// </summary>
    [HttpPost("cross-validation")]
    [ProducesResponseType(typeof(CrossValidationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CrossValidation([FromBody] CrossValidationRequest request)
    {
        try
        {
            _logger.LogInformation("Cross-validation voor {Algorithm} wordt uitgevoerd...", request.AlgorithmName);
            
            var result = await Task.Run(() => 
                _mlEvaluationService.PerformCrossValidation(
                    request.AlgorithmName, 
                    request.KFolds, 
                    request.TopK));
            
            _logger.LogInformation("Cross-validation voltooid. Precision: {Precision:P2}, Recall: {Recall:P2}", 
                result.PrecisionAt10, result.RecallAt10);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fout bij cross-validation voor {Algorithm}", request.AlgorithmName);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/ml/compare
    /// Vergelijkt alle beschikbare algoritmes met cross-validation.
    /// 
    /// Request body:
    /// {
    ///   "kFolds": 5
    /// }
    /// </summary>
    [HttpPost("compare")]
    [ProducesResponseType(typeof(AlgorithmComparison), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CompareAlgorithms([FromBody] CompareRequest request)
    {
        try
        {
            _logger.LogInformation("Algoritme vergelijking wordt uitgevoerd met {KFolds} folds...", request.KFolds);
            
            var result = await Task.Run(() => 
                _mlEvaluationService.CompareAllAlgorithms(request.KFolds));
            
            _logger.LogInformation("Vergelijking voltooid. Best by Precision: {Best}", result.BestByPrecision);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fout bij algoritme vergelijking");
            return BadRequest(new { error = ex.Message });
        }
    }
}

/// <summary>
/// ML model status response model.
/// </summary>
public class MlModelStatus
{
    /// <summary>
    /// Of het model getraind is.
    /// </summary>
    public bool IsTrained { get; set; }
    
    /// <summary>
    /// Wanneer het model voor het laatst getraind is.
    /// </summary>
    public DateTime LastTrainingTime { get; set; }
    
    /// <summary>
    /// Aantal training samples gebruikt voor training.
    /// </summary>
    public int TrainingDataCount { get; set; }
    
    /// <summary>
    /// Of het model opgeslagen is op disk.
    /// </summary>
    public bool ModelExists { get; set; }
    
    /// <summary>
    /// Pad waar het model is opgeslagen (indien beschikbaar).
    /// </summary>
    public string? ModelPath { get; set; }
}

/// <summary>
/// Request model voor cross-validation endpoint.
/// </summary>
public class CrossValidationRequest
{
    public string AlgorithmName { get; set; } = "mlnet";
    public int KFolds { get; set; } = 5;
    public int TopK { get; set; } = 10;
}

/// <summary>
/// Request model voor compare endpoint.
/// </summary>
public class CompareRequest
{
    public int KFolds { get; set; } = 5;
}







