namespace CarRecommender;

/// <summary>
/// Repository interface voor user feedback data.
/// Voor continue learning van het ML model.
/// </summary>
public interface IFeedbackRepository
{
    /// <summary>
    /// Voegt nieuwe feedback toe.
    /// </summary>
    void AddFeedback(UserFeedback feedback);

    /// <summary>
    /// Haalt alle feedback op voor een specifieke auto.
    /// </summary>
    List<UserFeedback> GetFeedbackForCar(int carId);

    /// <summary>
    /// Haalt alle feedback op binnen een tijdperiode.
    /// </summary>
    List<UserFeedback> GetFeedbackSince(DateTime since);

    /// <summary>
    /// Haalt geaggregeerde feedback op voor alle auto's.
    /// </summary>
    Dictionary<int, AggregatedFeedback> GetAggregatedFeedback();

    /// <summary>
    /// Haalt geaggregeerde feedback op voor een specifieke auto.
    /// </summary>
    AggregatedFeedback? GetAggregatedFeedbackForCar(int carId);

    /// <summary>
    /// Verwijdert oude feedback (ouder dan specified days).
    /// </summary>
    void CleanupOldFeedback(int daysToKeep = 90);

    /// <summary>
    /// Telt totaal aantal feedback entries.
    /// </summary>
    int GetTotalFeedbackCount();
}






