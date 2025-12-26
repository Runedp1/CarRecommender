using CarRecommender;

namespace CarRecommender.Api.Services;

/// <summary>
/// Background service die periodiek controleert of retraining nodig is.
/// Draait op de achtergrond en triggert automatische retraining.
/// </summary>
public class RetrainingBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RetrainingBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(30); // Check elke 30 minuten

    public RetrainingBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<RetrainingBackgroundService> logger)
    {
        // #region agent log
        try
        {
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".cursor", "debug.log");
            var logDir = Path.GetDirectoryName(logPath);
            if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);
            var logEntry = System.Text.Json.JsonSerializer.Serialize(new
            {
                id = $"log_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Guid.NewGuid():N}",
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                location = "RetrainingBackgroundService.cs:18",
                message = "RetrainingBackgroundService constructor",
                data = new { },
                sessionId = "debug-session",
                runId = "startup",
                hypothesisId = "D"
            });
            File.AppendAllText(logPath, logEntry + Environment.NewLine);
        }
        catch { }
        // #endregion
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // #region agent log
        try
        {
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".cursor", "debug.log");
            var logEntry = System.Text.Json.JsonSerializer.Serialize(new
            {
                id = $"log_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Guid.NewGuid():N}",
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                location = "RetrainingBackgroundService.cs:23",
                message = "RetrainingBackgroundService ExecuteAsync start",
                data = new { },
                sessionId = "debug-session",
                runId = "startup",
                hypothesisId = "D"
            });
            File.AppendAllText(logPath, logEntry + Environment.NewLine);
        }
        catch { }
        // #endregion
        _logger.LogInformation("Retraining background service gestart. Check interval: {Interval} minuten", 
            _checkInterval.TotalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Maak scope voor scoped services
                using (var scope = _serviceProvider.CreateScope())
                {
                    var retrainingService = scope.ServiceProvider.GetService<ModelRetrainingService>();
                    
                    if (retrainingService != null)
                    {
                        var result = retrainingService.CheckAndRetrainIfNeeded();
                        
                        if (result.Retrained)
                        {
                            _logger.LogInformation(
                                "Automatische retraining uitgevoerd: {Reason}. Training data: {TrainingData}, Feedback: {Feedback}",
                                result.Reason, result.TrainingDataCount, result.FeedbackCount);
                        }
                        else
                        {
                            _logger.LogDebug("Retraining check: {Reason}", result.Reason);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fout tijdens automatische retraining check");
            }

            // Wacht tot volgende check
            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("Retraining background service gestopt");
    }
}


