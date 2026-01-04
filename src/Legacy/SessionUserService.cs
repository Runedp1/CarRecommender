namespace CarRecommender;

/// <summary>
/// Service voor het beheren van user IDs op basis van sessions.
/// 
/// GEEN ECHTE AUTHENTICATION - gebruikt browser sessions of GUIDs.
/// Dit is voldoende voor collaborative filtering zonder inlog systeem.
/// 
/// MOVED TO LEGACY: Not actively used - was intended for collaborative filtering which is disabled.
/// </summary>
public class SessionUserService
{
    private readonly Dictionary<string, string> _sessionToUserId = new();
    private readonly object _lock = new object();

    /// <summary>
    /// Haalt of maakt een user ID voor een session.
    /// Als er geen session ID is, wordt een nieuwe GUID gemaakt.
    /// </summary>
    public string GetOrCreateUserId(string? sessionId = null)
    {
        if (string.IsNullOrEmpty(sessionId))
        {
            // Geen session ID - maak nieuwe GUID
            return Guid.NewGuid().ToString();
        }

        lock (_lock)
        {
            if (!_sessionToUserId.TryGetValue(sessionId, out var userId))
            {
                // Nieuwe session - maak nieuwe user ID
                userId = Guid.NewGuid().ToString();
                _sessionToUserId[sessionId] = userId;
            }
            return userId;
        }
    }

    /// <summary>
    /// Haalt user ID op voor een session (retourneert null als niet bestaat).
    /// </summary>
    public string? GetUserId(string sessionId)
    {
        lock (_lock)
        {
            return _sessionToUserId.TryGetValue(sessionId, out var userId) ? userId : null;
        }
    }

    /// <summary>
    /// Verwijdert oude sessions (cleanup).
    /// </summary>
    public void CleanupOldSessions(TimeSpan maxAge)
    {
        // Voor nu: in-memory, dus cleanup gebeurt automatisch bij restart
        // In productie met database zou je hier oude sessions kunnen verwijderen
    }
}

