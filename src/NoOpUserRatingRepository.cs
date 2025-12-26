namespace CarRecommender;

/// <summary>
/// No-op implementatie van IUserRatingRepository voor wanneer SQLite niet beschikbaar is.
/// Alle methodes retourneren lege resultaten zonder errors te gooien.
/// </summary>
public class NoOpUserRatingRepository : IUserRatingRepository
{
    public string DatabasePath => ":memory: (no-op - SQLite not available)";

    public Task AddRatingAsync(UserRating rating) => Task.CompletedTask;

    public Task<List<UserRating>> GetRatingsForCarAsync(int carId) => Task.FromResult(new List<UserRating>());

    public Task<List<UserRating>> GetRatingsForUserAsync(string userId) => Task.FromResult(new List<UserRating>());

    public Task<AggregatedRating?> GetAggregatedRatingForCarAsync(int carId) => Task.FromResult<AggregatedRating?>(null);

    public Task<Dictionary<int, AggregatedRating>> GetAllAggregatedRatingsAsync() => Task.FromResult(new Dictionary<int, AggregatedRating>());

    public Task<List<UserRating>> FindSimilarUserRatingsAsync(UserPreferenceSnapshot preferences, int limit = 50) => Task.FromResult(new List<UserRating>());

    public Task<List<(int CarId, double AverageRating, int Count)>> GetTopRatedCarsForPreferencesAsync(
        UserPreferenceSnapshot preferences, 
        int limit = 10) => Task.FromResult(new List<(int CarId, double AverageRating, int Count)>());

    public Task InitializeDatabaseAsync() => Task.CompletedTask;

    public Task ClearAllRatingsAsync() => Task.CompletedTask;

    public Task ResetDatabaseAsync() => Task.CompletedTask;

    public Task<DatabaseStatistics> GetDatabaseStatisticsAsync() => Task.FromResult(new DatabaseStatistics
    {
        DatabasePath = ":memory: (no-op)",
        TotalRatings = 0,
        UniqueCars = 0,
        UniqueUsers = 0
    });
}

