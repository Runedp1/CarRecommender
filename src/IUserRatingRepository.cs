namespace CarRecommender;

/// <summary>
/// Repository interface voor user ratings data.
/// Gebruikt voor collaborative filtering.
/// </summary>
public interface IUserRatingRepository
{
    /// <summary>
    /// Voegt een nieuwe rating toe.
    /// </summary>
    Task AddRatingAsync(UserRating rating);

    /// <summary>
    /// Haalt alle ratings op voor een specifieke auto.
    /// </summary>
    Task<List<UserRating>> GetRatingsForCarAsync(int carId);

    /// <summary>
    /// Haalt alle ratings op voor een specifieke gebruiker.
    /// </summary>
    Task<List<UserRating>> GetRatingsForUserAsync(string userId);

    /// <summary>
    /// Haalt geaggregeerde ratings op voor een auto.
    /// </summary>
    Task<AggregatedRating?> GetAggregatedRatingForCarAsync(int carId);

    /// <summary>
    /// Haalt alle geaggregeerde ratings op.
    /// </summary>
    Task<Dictionary<int, AggregatedRating>> GetAllAggregatedRatingsAsync();

    /// <summary>
    /// Zoekt ratings van gebruikers met gelijkaardige preferences.
    /// </summary>
    Task<List<UserRating>> FindSimilarUserRatingsAsync(UserPreferenceSnapshot preferences, int limit = 50);

    /// <summary>
    /// Haalt top-rated auto's op voor specifieke preferences.
    /// </summary>
    Task<List<(int CarId, double AverageRating, int Count)>> GetTopRatedCarsForPreferencesAsync(
        UserPreferenceSnapshot preferences, 
        int limit = 10);

    /// <summary>
    /// Initialiseert de database (maakt tabellen aan als ze niet bestaan).
    /// </summary>
    Task InitializeDatabaseAsync();

    /// <summary>
    /// Verwijdert alle ratings uit de database (voor testing/reset).
    /// WAARSCHUWING: Dit verwijdert alle data!
    /// </summary>
    Task ClearAllRatingsAsync();

    /// <summary>
    /// Verwijdert de hele database en maakt deze opnieuw aan.
    /// WAARSCHUWING: Dit verwijdert alle data en tabel structuur!
    /// </summary>
    Task ResetDatabaseAsync();

    /// <summary>
    /// Haalt database statistieken op (aantal ratings, etc.).
    /// </summary>
    Task<DatabaseStatistics> GetDatabaseStatisticsAsync();
}

/// <summary>
/// Database statistieken voor monitoring.
/// </summary>
public class DatabaseStatistics
{
    public int TotalRatings { get; set; }
    public int UniqueCars { get; set; }
    public int UniqueUsers { get; set; }
    public DateTime? OldestRating { get; set; }
    public DateTime? NewestRating { get; set; }
    public string DatabasePath { get; set; } = string.Empty;
}

