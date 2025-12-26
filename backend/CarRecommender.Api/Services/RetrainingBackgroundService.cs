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
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
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


