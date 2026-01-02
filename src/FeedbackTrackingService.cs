namespace CarRecommender;

/// <summary>
/// Service voor het tracken van user feedback en het coördineren van continue learning.
/// </summary>
public class FeedbackTrackingService
{
    private readonly IFeedbackRepository _feedbackRepository;

    public FeedbackTrackingService(IFeedbackRepository feedbackRepository)
    {
        _feedbackRepository = feedbackRepository ?? throw new ArgumentNullException(nameof(feedbackRepository));
    }

    /// <summary>
    /// Trackt dat een gebruiker op een auto heeft geklikt.
    /// </summary>
    public void TrackClick(int carId, double recommendationScore, int position, string? recommendationContext = null, string? sessionId = null)
    {
        var feedback = new UserFeedback
        {
            CarId = carId,
            FeedbackType = FeedbackType.Click,
            RecommendationScore = recommendationScore,
            Position = position,
            RecommendationContext = recommendationContext,
            SessionId = sessionId,
            Timestamp = DateTime.UtcNow
        };

        _feedbackRepository.AddFeedback(feedback);
    }

    /// <summary>
    /// Trackt dat een auto werd aanbevolen (zonder click).
    /// Dit wordt gebruikt om CTR te berekenen.
    /// </summary>
    public void TrackRecommendation(int carId, double recommendationScore, int position, string? recommendationContext = null, string? sessionId = null)
    {
        // Voor nu tracken we alleen clicks, maar dit kan uitgebreid worden
        // om alle recommendations te tracken voor betere CTR berekening
    }

    /// <summary>
    /// Haalt geaggregeerde feedback op voor een auto.
    /// </summary>
    public AggregatedFeedback? GetFeedbackForCar(int carId)
    {
        return _feedbackRepository.GetAggregatedFeedbackForCar(carId);
    }

    /// <summary>
    /// Haalt alle feedback op sinds een bepaalde datum.
    /// </summary>
    public List<UserFeedback> GetFeedbackSince(DateTime since)
    {
        return _feedbackRepository.GetFeedbackSince(since);
    }

    /// <summary>
    /// Haalt alle geaggregeerde feedback op.
    /// </summary>
    public Dictionary<int, AggregatedFeedback> GetAllAggregatedFeedback()
    {
        return _feedbackRepository.GetAggregatedFeedback();
    }

    /// <summary>
    /// Verwijdert oude feedback data.
    /// </summary>
    public void CleanupOldFeedback(int daysToKeep = 90)
    {
        _feedbackRepository.CleanupOldFeedback(daysToKeep);
    }

    /// <summary>
    /// Telt totaal aantal feedback entries.
    /// </summary>
    public int GetTotalFeedbackCount()
    {
        return _feedbackRepository.GetTotalFeedbackCount();
    }
}





