namespace CarRecommender;

/// <summary>
/// Service voor automatische retraining van het ML model op basis van nieuwe feedback.
/// 
/// Deze service controleert periodiek of er voldoende nieuwe feedback is om het model opnieuw te trainen.
/// </summary>
public class ModelRetrainingService
{
    private readonly MlRecommendationService _mlService;
    private readonly FeedbackTrackingService _feedbackService;
    private readonly ICarRepository _carRepository;
    private readonly IRecommendationService _recommendationService;
    
    // Configuratie
    private readonly int _minNewFeedbackForRetraining;
    private readonly int _retrainingSampleSize;
    private readonly TimeSpan _minTimeBetweenRetraining;

    private DateTime _lastRetrainingTime = DateTime.MinValue;
    private int _lastFeedbackCount = 0;

    public ModelRetrainingService(
        MlRecommendationService mlService,
        FeedbackTrackingService feedbackService,
        ICarRepository carRepository,
        IRecommendationService recommendationService,
        int minNewFeedbackForRetraining = 50,
        int retrainingSampleSize = 100,
        TimeSpan? minTimeBetweenRetraining = null)
    {
        _mlService = mlService ?? throw new ArgumentNullException(nameof(mlService));
        _feedbackService = feedbackService ?? throw new ArgumentNullException(nameof(feedbackService));
        _carRepository = carRepository ?? throw new ArgumentNullException(nameof(carRepository));
        _recommendationService = recommendationService ?? throw new ArgumentNullException(nameof(recommendationService));
        
        _minNewFeedbackForRetraining = minNewFeedbackForRetraining;
        _retrainingSampleSize = retrainingSampleSize;
        _minTimeBetweenRetraining = minTimeBetweenRetraining ?? TimeSpan.FromHours(1);
    }

    /// <summary>
    /// Controleert of retraining nodig is en voert deze uit indien nodig.
    /// </summary>
    public RetrainingResult CheckAndRetrainIfNeeded()
    {
        var currentFeedbackCount = _feedbackService.GetTotalFeedbackCount();
        var newFeedbackCount = currentFeedbackCount - _lastFeedbackCount;
        var timeSinceLastRetraining = DateTime.UtcNow - _lastRetrainingTime;

        // Check of retraining nodig is
        bool shouldRetrain = 
            newFeedbackCount >= _minNewFeedbackForRetraining &&
            timeSinceLastRetraining >= _minTimeBetweenRetraining;

        if (!shouldRetrain)
        {
            return new RetrainingResult
            {
                Retrained = false,
                Reason = $"Niet genoeg nieuwe feedback ({newFeedbackCount}/{_minNewFeedbackForRetraining}) of te recent getraind ({timeSinceLastRetraining.TotalMinutes:F1} min geleden)"
            };
        }

        try
        {
            // Haal alle auto's op
            var allCars = _carRepository.GetAllCars();
            
            // Haal user feedback op
            var userFeedback = _feedbackService.GetAllAggregatedFeedback();

            // Genereer nieuwe training data
            var trainingResults = GenerateTrainingData(allCars, userFeedback);

            if (trainingResults.Count < 10)
            {
                return new RetrainingResult
                {
                    Retrained = false,
                    Reason = "Onvoldoende training data gegenereerd"
                };
            }

            // Retrain het model
            _mlService.RetrainModel(allCars, trainingResults, userFeedback);

            // Update tracking
            _lastRetrainingTime = DateTime.UtcNow;
            _lastFeedbackCount = currentFeedbackCount;

            return new RetrainingResult
            {
                Retrained = true,
                Reason = "Model succesvol opnieuw getraind",
                TrainingDataCount = trainingResults.Count,
                FeedbackCount = currentFeedbackCount
            };
        }
        catch (Exception ex)
        {
            return new RetrainingResult
            {
                Retrained = false,
                Reason = $"Fout tijdens retraining: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Genereert training data door recommendations te maken voor een sample van auto's.
    /// </summary>
    private List<RecommendationResult> GenerateTrainingData(List<Car> allCars, Dictionary<int, AggregatedFeedback> userFeedback)
    {
        var trainingResults = new List<RecommendationResult>();
        var sampleSize = Math.Min(_retrainingSampleSize, allCars.Count);
        var random = new Random();

        // Gebruik random sample, maar geef voorkeur aan auto's met feedback
        var carsWithFeedback = allCars
            .Where(c => userFeedback.ContainsKey(c.Id))
            .OrderByDescending(c => userFeedback[c.Id].PopularityScore)
            .Take(sampleSize / 2)
            .ToList();

        var remainingCars = allCars
            .Where(c => !carsWithFeedback.Contains(c))
            .OrderBy(x => random.Next())
            .Take(sampleSize - carsWithFeedback.Count)
            .ToList();

        var sampleCars = carsWithFeedback.Concat(remainingCars).ToList();

        foreach (var targetCar in sampleCars)
        {
            if (targetCar.Power <= 0 || targetCar.Budget <= 0 || targetCar.Year < 1900)
                continue;

            try
            {
                var recommendations = _recommendationService.RecommendSimilarCars(targetCar, 10);
                trainingResults.AddRange(recommendations);
            }
            catch
            {
                // Skip als recommendation faalt
                continue;
            }
        }

        return trainingResults;
    }

    /// <summary>
    /// Forceert retraining (gebruik met voorzichtigheid).
    /// </summary>
    public RetrainingResult ForceRetrain()
    {
        _lastRetrainingTime = DateTime.MinValue;
        _lastFeedbackCount = 0;
        return CheckAndRetrainIfNeeded();
    }
}

/// <summary>
/// Resultaat van een retraining operatie.
/// </summary>
public class RetrainingResult
{
    public bool Retrained { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int TrainingDataCount { get; set; }
    public int FeedbackCount { get; set; }
}


