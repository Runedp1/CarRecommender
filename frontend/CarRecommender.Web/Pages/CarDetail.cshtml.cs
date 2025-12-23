using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CarRecommender.Web.Services;
using CarRecommender.Web.Models;

namespace CarRecommender.Web.Pages;

public class CarDetailModel : PageModel
{
    private readonly CarApiClient _apiClient;
    private readonly ILogger<CarDetailModel> _logger;
    private readonly IConfiguration _configuration;

    public Car? Car { get; set; }
    public List<string> Images { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public string ApiBaseUrl { get; set; } = string.Empty;

    public CarDetailModel(CarApiClient apiClient, ILogger<CarDetailModel> logger, IConfiguration configuration)
    {
        _apiClient = apiClient;
        _logger = logger;
        _configuration = configuration;
        ApiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? string.Empty;
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        try
        {
            Car = await _apiClient.GetCarByIdAsync(id);
            
            if (Car == null)
            {
                ErrorMessage = "Auto niet gevonden.";
                return Page();
            }

            // Haal alle images op voor deze auto
            Images = await _apiClient.GetCarImagesAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fout bij het ophalen van auto details voor ID {CarId}", id);
            ErrorMessage = "Er is een fout opgetreden bij het ophalen van de auto details.";
        }

        return Page();
    }
}

