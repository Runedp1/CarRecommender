using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CarRecommender.Web.Services;
using CarRecommender.Web.Models;

namespace CarRecommender.Web.Pages;

public class CarsModel : PageModel
{
    private readonly CarApiClient _apiClient;
    private readonly ILogger<CarsModel> _logger;

    public List<Car>? AllCars { get; set; }
    public string? ErrorMessage { get; set; }

    public CarsModel(CarApiClient apiClient, ILogger<CarsModel> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task OnGetAsync()
    {
        try
        {
            AllCars = await _apiClient.GetAllCarsAsync();
            
            if (AllCars == null || AllCars.Count == 0)
            {
                ErrorMessage = "Geen auto's gevonden.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fout bij het ophalen van auto's");
            ErrorMessage = "Er is een fout opgetreden bij het verbinden met de API. Controleer of de API bereikbaar is.";
        }
    }
}


