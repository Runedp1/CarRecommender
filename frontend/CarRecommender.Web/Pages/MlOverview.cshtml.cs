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
            EvaluationResult = await _apiClient.GetMlEvaluationAsync();

            if (EvaluationResult == null || !EvaluationResult.IsValid)
            {
                ErrorMessage = EvaluationResult?.ErrorMessage ?? "Geen evaluatie resultaten beschikbaar";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fout bij het ophalen van ML evaluatie resultaten");
            ErrorMessage = "Er is een fout opgetreden bij het verbinden met de API. Controleer of de API bereikbaar is.";
        }

        return Page();
    }
}

