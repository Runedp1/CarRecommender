using System.Collections.Concurrent;

namespace CarRecommender;

/// <summary>
/// In-memory implementatie van IFeedbackRepository.
/// 
/// Voor productie zou dit vervangen kunnen worden door een database implementatie.
/// </summary>
public class FeedbackRepository : IFeedbackRepository
{
    private readonly ConcurrentDictionary<int, UserFeedback> _feedback = new();
    private readonly ConcurrentDictionary<int, List<UserFeedback>> _feedbackByCar = new();
    private int _nextId = 1;
    private readonly object _lock = new object();

    public void AddFeedback(UserFeedback feedback)
    {
        if (feedback == null)
            throw new ArgumentNullException(nameof(feedback));

        // Genereer ID als nog niet ingesteld
        if (feedback.Id == 0)
        {
            lock (_lock)
            {
                feedback.Id = _nextId++;
            }
        }

        // Zet timestamp als nog niet ingesteld
        if (feedback.Timestamp == default)
        {
            feedback.Timestamp = DateTime.UtcNow;
        }

        // Voeg toe aan dictionaries
        _feedback.TryAdd(feedback.Id, feedback);

        _feedbackByCar.AddOrUpdate(
            feedback.CarId,
            new List<UserFeedback> { feedback },
            (key, existing) =>
            {
                existing.Add(feedback);
                return existing;
            });
    }

    public List<UserFeedback> GetFeedbackForCar(int carId)
    {
        if (_feedbackByCar.TryGetValue(carId, out var feedbackList))
        {
            return new List<UserFeedback>(feedbackList);
        }
        return new List<UserFeedback>();
    }

    public List<UserFeedback> GetFeedbackSince(DateTime since)
    {
        return _feedback.Values
            .Where(f => f.Timestamp >= since)
            .OrderByDescending(f => f.Timestamp)
            .ToList();
    }

    public Dictionary<int, AggregatedFeedback> GetAggregatedFeedback()
    {
        var aggregated = new Dictionary<int, AggregatedFeedback>();

        foreach (var carGroup in _feedbackByCar)
        {
            var carId = carGroup.Key;
            var feedbacks = carGroup.Value;

            if (feedbacks.Count == 0)
                continue;

            var clicks = feedbacks.Count(f => f.FeedbackType == FeedbackType.Click);
            var positive = feedbacks.Count(f => f.FeedbackType == FeedbackType.Positive || 
                                                f.FeedbackType == FeedbackType.Favorite || 
                                                f.FeedbackType == FeedbackType.Purchase);
            var negative = feedbacks.Count(f => f.FeedbackType == FeedbackType.Negative);

            var avgPosition = feedbacks.Where(f => f.Position > 0)
                .Select(f => (double)f.Position)
                .DefaultIfEmpty(0)
                .Average();

            var avgScore = feedbacks.Where(f => f.RecommendationScore > 0)
                .Select(f => f.RecommendationScore)
                .DefaultIfEmpty(0)
                .Average();

            // Bereken CTR: clicks / totaal aantal keer aanbevolen
            var totalRecommendations = feedbacks.Count;
            var ctr = totalRecommendations > 0 ? (double)clicks / totalRecommendations : 0.0;

            // Bereken populairiteit score (0-1)
            // Combineert CTR, positieve feedback, en gemiddelde positie
            var popularityScore = CalculatePopularityScore(ctr, positive, negative, avgPosition);

            aggregated[carId] = new AggregatedFeedback
            {
                CarId = carId,
                TotalClicks = clicks,
                TotalPositive = positive,
                TotalNegative = negative,
                AveragePosition = avgPosition,
                AverageRecommendationScore = avgScore,
                ClickThroughRate = ctr,
                PopularityScore = popularityScore,
                LastUpdated = DateTime.UtcNow
            };
        }

        return aggregated;
    }

    public AggregatedFeedback? GetAggregatedFeedbackForCar(int carId)
    {
        var allAggregated = GetAggregatedFeedback();
        return allAggregated.TryGetValue(carId, out var feedback) ? feedback : null;
    }

    public void CleanupOldFeedback(int daysToKeep = 90)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
        var toRemove = _feedback.Values
            .Where(f => f.Timestamp < cutoffDate)
            .Select(f => f.Id)
            .ToList();

        foreach (var id in toRemove)
        {
            if (_feedback.TryRemove(id, out var feedback))
            {
                if (_feedbackByCar.TryGetValue(feedback.CarId, out var carFeedback))
                {
                    carFeedback.RemoveAll(f => f.Id == id);
                    if (carFeedback.Count == 0)
                    {
                        _feedbackByCar.TryRemove(feedback.CarId, out _);
                    }
                }
            }
        }
    }

    public int GetTotalFeedbackCount()
    {
        return _feedback.Count;
    }

    /// <summary>
    /// Berekent populairiteit score op basis van feedback metrics.
    /// </summary>
    private double CalculatePopularityScore(double ctr, int positive, int negative, double avgPosition)
    {
        // CTR component (0-1): hoe vaak wordt er geklikt?
        var ctrComponent = Math.Min(1.0, ctr * 2.0); // Scale CTR (0.5 CTR = 1.0 score)

        // Positive feedback component (0-1)
        var positiveComponent = Math.Min(1.0, positive / 10.0); // 10 positive = max score

        // Negative feedback penalty (0-1)
        var negativePenalty = Math.Min(1.0, negative / 5.0); // 5 negative = max penalty

        // Position component: lagere positie = hogere score
        var positionComponent = avgPosition > 0 ? Math.Max(0.0, 1.0 - (avgPosition / 10.0)) : 0.5;

        // Combineer componenten
        var score = (ctrComponent * 0.4) + 
                   (positiveComponent * 0.3) + 
                   (positionComponent * 0.2) - 
                   (negativePenalty * 0.1);

        return Math.Max(0.0, Math.Min(1.0, score));
    }
}






