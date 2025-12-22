using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CarRecommender.Web.Services;
using CarRecommender.Web.Models;

namespace CarRecommender.Web.Pages;

public class IndexModel : PageModel
{
    private readonly CarApiClient _apiClient;
    private readonly ILogger<IndexModel> _logger;

    [BindProperty]
    public string? SearchText { get; set; }

    public List<RecommendationResult>? Recommendations { get; set; }
    public string? ErrorMessage { get; set; }

    public IndexModel(CarApiClient apiClient, ILogger<IndexModel> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public void OnGet()
    {
        // Lege pagina bij GET request
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            ErrorMessage = "Voer alstublieft uw wensen in.";
            return Page();
        }

        try
        {
            Recommendations = await _apiClient.GetRecommendationsFromTextAsync(SearchText, top: 5);
            
            if (Recommendations == null || Recommendations.Count == 0)
            {
                ErrorMessage = "Geen auto's gevonden die voldoen aan uw criteria. Probeer het opnieuw met andere wensen.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fout bij het ophalen van recommendations");
            ErrorMessage = "Er is een fout opgetreden bij het verbinden met de API. Controleer of de API bereikbaar is.";
        }

        return Page();
    }
}
