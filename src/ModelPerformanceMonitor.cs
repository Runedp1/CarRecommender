namespace CarRecommender;

/// <summary>
/// Service voor het monitoren van ML model performance en continue learning metrics.
/// </summary>
public class ModelPerformanceMonitor
{
    private readonly FeedbackTrackingService _feedbackService;
    private readonly MlRecommendationService _mlService;
    private readonly ModelRetrainingService _retrainingService;

    public ModelPerformanceMonitor(
        FeedbackTrackingService feedbackService,
        MlRecommendationService mlService,
        ModelRetrainingService retrainingService)
    {
        _feedbackService = feedbackService ?? throw new ArgumentNullException(nameof(feedbackService));
        _mlService = mlService ?? throw new ArgumentNullException(nameof(mlService));
        _retrainingService = retrainingService ?? throw new ArgumentNullException(nameof(retrainingService));
    }

    /// <summary>
    /// Haalt performance metrics op voor het ML model.
    /// </summary>
    public ModelPerformanceMetrics GetPerformanceMetrics()
    {
        var modelStats = _mlService.GetModelStatistics();
        var totalFeedback = _feedbackService.GetTotalFeedbackCount();
        var aggregatedFeedback = _feedbackService.GetAllAggregatedFeedback();

        // Bereken gemiddelde CTR
        var avgCTR = aggregatedFeedback.Values
            .Where(f => f.TotalClicks > 0)
            .Select(f => f.ClickThroughRate)
            .DefaultIfEmpty(0)
            .Average();

        // Bereken gemiddelde populairiteit
        var avgPopularity = aggregatedFeedback.Values
            .Select(f => f.PopularityScore)
            .DefaultIfEmpty(0)
            .Average();

        // Tel auto's met feedback
        var carsWithFeedback = aggregatedFeedback.Count;

        return new ModelPerformanceMetrics
        {
            ModelTrained = modelStats.IsTrained,
            LastTrainingTime = modelStats.LastTrainingTime,
            TrainingDataCount = modelStats.TrainingDataCount,
            TotalFeedbackCount = totalFeedback,
            CarsWithFeedback = carsWithFeedback,
            AverageClickThroughRate = avgCTR,
            AveragePopularityScore = avgPopularity,
            FeedbackDistribution = CalculateFeedbackDistribution(aggregatedFeedback)
        };
    }

    /// <summary>
    /// Berekent distributie van feedback over verschillende score ranges.
    /// </summary>
    private Dictionary<string, int> CalculateFeedbackDistribution(Dictionary<int, AggregatedFeedback> feedback)
    {
        var distribution = new Dictionary<string, int>
        {
            { "HighPopularity", 0 },    // PopularityScore > 0.7
            { "MediumPopularity", 0 },  // 0.3 < PopularityScore <= 0.7
            { "LowPopularity", 0 }      // PopularityScore <= 0.3
        };

        foreach (var fb in feedback.Values)
        {
            if (fb.PopularityScore > 0.7)
                distribution["HighPopularity"]++;
            else if (fb.PopularityScore > 0.3)
                distribution["MediumPopularity"]++;
            else
                distribution["LowPopularity"]++;
        }

        return distribution;
    }

    /// <summary>
    /// Controleert of retraining nodig is en geeft details.
    /// </summary>
    public RetrainingStatus GetRetrainingStatus()
    {
        var result = _retrainingService.CheckAndRetrainIfNeeded();
        
        return new RetrainingStatus
        {
            ShouldRetrain = result.Retrained,
            Reason = result.Reason,
            LastRetrainingTime = _mlService.GetModelStatistics().LastTrainingTime
        };
    }
}

/// <summary>
/// Performance metrics voor het ML model.
/// </summary>
public class ModelPerformanceMetrics
{
    public bool ModelTrained { get; set; }
    public DateTime LastTrainingTime { get; set; }
    public int TrainingDataCount { get; set; }
    public int TotalFeedbackCount { get; set; }
    public int CarsWithFeedback { get; set; }
    public double AverageClickThroughRate { get; set; }
    public double AveragePopularityScore { get; set; }
    public Dictionary<string, int> FeedbackDistribution { get; set; } = new();
}

/// <summary>
/// Status van retraining.
/// </summary>
public class RetrainingStatus
{
    public bool ShouldRetrain { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime LastRetrainingTime { get; set; }
}

