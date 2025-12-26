namespace CarRecommender;

/// <summary>
/// Veilige debug logger die niet crasht als logging faalt.
/// </summary>
public static class DebugLogger
{
    private static readonly object _lock = new object();
    private static string? _logPath = null;

    /// <summary>
    /// Haalt het log pad op, maakt directory aan indien nodig.
    /// Retourneert null als logging niet mogelijk is.
    /// </summary>
    private static string? GetLogPath()
    {
        if (_logPath != null)
            return _logPath;

        lock (_lock)
        {
            if (_logPath != null)
                return _logPath;

            try
            {
                // Probeer verschillende locaties
                var possiblePaths = new[]
                {
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".cursor", "debug.log"),
                    Path.Combine(Directory.GetCurrentDirectory(), ".cursor", "debug.log"),
                    Path.Combine(Path.GetTempPath(), "carrecommender-debug.log")
                };

                foreach (var path in possiblePaths)
                {
                    try
                    {
                        var dir = Path.GetDirectoryName(path);
                        if (dir != null && !Directory.Exists(dir))
                        {
                            Directory.CreateDirectory(dir);
                        }

                        // Test schrijven
                        File.AppendAllText(path, "");
                        _logPath = path;
                        return _logPath;
                    }
                    catch
                    {
                        // Probeer volgende pad
                        continue;
                    }
                }

                // Geen pad werkt - logging uitgeschakeld
                return null;
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Logt een entry veilig (faalt stil als logging niet mogelijk is).
    /// </summary>
    public static async Task LogAsync(string location, string message, object? data = null, string? hypothesisId = null)
    {
        var logPath = GetLogPath();
        if (logPath == null)
            return; // Logging niet mogelijk, fail silently

        try
        {
            var logEntry = new
            {
                location = location,
                message = message,
                data = data,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                sessionId = "debug-session",
                runId = "run1",
                hypothesisId = hypothesisId
            };

            var json = System.Text.Json.JsonSerializer.Serialize(logEntry) + Environment.NewLine;
            await File.AppendAllTextAsync(logPath, json);
        }
        catch
        {
            // Fail silently - logging is optioneel
        }
    }
}

