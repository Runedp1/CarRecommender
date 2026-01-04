using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CarRecommender.Web.Services;
using CarRecommender.Web.Models;

namespace CarRecommender.Web.Pages;

public class MlOverviewModel : PageModel
{
    private readonly CarApiClient _apiClient;
    private readonly ILogger<MlOverviewModel> _logger;

    public MlEvaluationResult? EvaluationResult { get; set; }
    public MlModelStatus? ModelStatus { get; set; }
    public string? ErrorMessage { get; set; }

    public MlOverviewModel(CarApiClient apiClient, ILogger<MlOverviewModel> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            _logger.LogInformation("[MlOverview] Start ophalen ML evaluatie resultaten en model status...");
            
            // Haal zowel evaluatie als model status op
            var evaluationTask = _apiClient.GetMlEvaluationAsync();
            var statusTask = _apiClient.GetMlStatusAsync();
            
            await Task.WhenAll(evaluationTask, statusTask);
            
            EvaluationResult = await evaluationTask;
            ModelStatus = await statusTask;

            if (EvaluationResult == null)
            {
                ErrorMessage = "Geen evaluatie resultaten ontvangen van de API.";
                _logger.LogWarning("[MlOverview] EvaluationResult is null");
            }
            else if (!EvaluationResult.IsValid)
            {
                ErrorMessage = EvaluationResult.ErrorMessage ?? "ML evaluatie is mislukt zonder specifieke foutmelding.";
                _logger.LogWarning("[MlOverview] EvaluationResult.IsValid = false. ErrorMessage: {ErrorMessage}", ErrorMessage);
            }
            else
            {
                _logger.LogInformation("[MlOverview] ML evaluatie succesvol opgehaald. TrainingSet: {TrainingSize}, TestSet: {TestSize}", 
                    EvaluationResult.TrainingSetSize, EvaluationResult.TestSetSize);
            }
            
            if (ModelStatus != null)
            {
                _logger.LogInformation("[MlOverview] ML model status opgehaald. IsTrained: {IsTrained}, LastTraining: {LastTraining}", 
                    ModelStatus.IsTrained, ModelStatus.LastTrainingTime);
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "[MlOverview] HTTP fout bij het ophalen van ML evaluatie resultaten");
            ErrorMessage = ex.Message; // Gebruik de specifieke error message van de HttpClient
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "[MlOverview] Timeout bij het ophalen van ML evaluatie resultaten");
            ErrorMessage = "ML evaluatie duurt te lang. Dit kan 30-60 seconden duren. Probeer het later opnieuw of wacht even en ververs de pagina.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MlOverview] Onverwachte fout bij het ophalen van ML evaluatie resultaten");
            ErrorMessage = $"Er is een fout opgetreden: {ex.Message}. Controleer of de API bereikbaar is op http://localhost:5283";
        }

        return Page();
    }
}







