using Microsoft.AspNetCore.Mvc;
using CarRecommender;

namespace CarRecommender.Api.Controllers;

/// <summary>
/// API Controller voor feedback endpoints.
/// Handelt user feedback af voor continue learning.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class FeedbackController : ControllerBase
{
    private readonly FeedbackTrackingService _feedbackService;
    private readonly ModelPerformanceMonitor? _performanceMonitor;
    private readonly ILogger<FeedbackController> _logger;

    public FeedbackController(
        FeedbackTrackingService feedbackService,
        ModelPerformanceMonitor? performanceMonitor = null,
        ILogger<FeedbackController>? logger = null)
    {
        _feedbackService = feedbackService ?? throw new ArgumentNullException(nameof(feedbackService));
        _performanceMonitor = performanceMonitor;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// POST /api/feedback/click
    /// Trackt dat een gebruiker op een auto heeft geklikt.
    /// 
    /// Request body:
    /// {
    ///   "carId": 1234,
    ///   "recommendationScore": 0.85,
    ///   "position": 1,
    ///   "recommendationContext": "text-based",
    ///   "sessionId": "optional-session-id"
    /// }
    /// </summary>
    [HttpPost("click")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult TrackClick([FromBody] ClickFeedbackRequest request)
    {
        try
        {
            if (request == null || request.CarId <= 0)
            {
                return BadRequest(new { error = "CarId is verplicht en moet groter dan 0 zijn." });
            }

            _feedbackService.TrackClick(
                request.CarId,
                request.RecommendationScore,
                request.Position,
                request.RecommendationContext,
                request.SessionId);

            _logger.LogInformation("Feedback getrackt: CarId={CarId}, Score={Score}, Position={Position}",
                request.CarId, request.RecommendationScore, request.Position);

            return Ok(new { message = "Feedback succesvol getrackt." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fout bij tracken van feedback");
            return StatusCode(500, new { error = "Interne serverfout bij tracken van feedback." });
        }
    }

    /// <summary>
    /// GET /api/feedback/car/{carId}
    /// Haalt geaggregeerde feedback op voor een specifieke auto.
    /// </summary>
    [HttpGet("car/{carId}")]
    [ProducesResponseType(typeof(AggregatedFeedback), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetFeedbackForCar(int carId)
    {
        try
        {
            var feedback = _feedbackService.GetFeedbackForCar(carId);
            
            if (feedback == null)
            {
                return NotFound(new { error = $"Geen feedback gevonden voor auto met ID {carId}." });
            }

            return Ok(feedback);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fout bij ophalen van feedback voor auto {CarId}", carId);
            return StatusCode(500, new { error = "Interne serverfout." });
        }
    }

    /// <summary>
    /// GET /api/feedback/stats
    /// Haalt algemene feedback statistieken op.
    /// </summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(FeedbackStats), StatusCodes.Status200OK)]
    public IActionResult GetStats()
    {
        try
        {
            var totalFeedback = _feedbackService.GetTotalFeedbackCount();
            var allFeedback = _feedbackService.GetAllAggregatedFeedback();

            var stats = new FeedbackStats
            {
                TotalFeedbackCount = totalFeedback,
                CarsWithFeedback = allFeedback.Count,
                AverageClickThroughRate = allFeedback.Values
                    .Where(f => f.TotalClicks > 0)
                    .Select(f => f.ClickThroughRate)
                    .DefaultIfEmpty(0)
                    .Average(),
                AveragePopularityScore = allFeedback.Values
                    .Select(f => f.PopularityScore)
                    .DefaultIfEmpty(0)
                    .Average()
            };

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fout bij ophalen van feedback statistieken");
            return StatusCode(500, new { error = "Interne serverfout." });
        }
    }

    /// <summary>
    /// GET /api/feedback/performance
    /// Haalt ML model performance metrics op.
    /// </summary>
    [HttpGet("performance")]
    [ProducesResponseType(typeof(ModelPerformanceMetrics), StatusCodes.Status200OK)]
    public IActionResult GetPerformance()
    {
        try
        {
            if (_performanceMonitor == null)
            {
                return StatusCode(503, new { error = "Performance monitoring niet beschikbaar." });
            }

            var metrics = _performanceMonitor.GetPerformanceMetrics();
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fout bij ophalen van performance metrics");
            return StatusCode(500, new { error = "Interne serverfout." });
        }
    }
}

/// <summary>
/// Request model voor click feedback.
/// </summary>
public class ClickFeedbackRequest
{
    public int CarId { get; set; }
    public double RecommendationScore { get; set; }
    public int Position { get; set; }
    public string? RecommendationContext { get; set; }
    public string? SessionId { get; set; }
}

/// <summary>
/// Feedback statistieken.
/// </summary>
public class FeedbackStats
{
    public int TotalFeedbackCount { get; set; }
    public int CarsWithFeedback { get; set; }
    public double AverageClickThroughRate { get; set; }
    public double AveragePopularityScore { get; set; }
}






