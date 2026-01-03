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
    private readonly ILogger<MlController> _logger;
    
    /// <summary>
    /// Constructor - krijgt services via dependency injection.
    /// </summary>
    public MlController(
        IMlEvaluationService mlEvaluationService,
        ILogger<MlController> logger)
    {
        _mlEvaluationService = mlEvaluationService ?? throw new ArgumentNullException(nameof(mlEvaluationService));
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
    [HttpGet("evaluation")]
    [ProducesResponseType(typeof(MlEvaluationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult GetEvaluation()
    {
        try
        {
            _logger.LogInformation("ML evaluatie wordt uitgevoerd...");
            
            var result = _mlEvaluationService.EvaluateModel();
            
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
}







