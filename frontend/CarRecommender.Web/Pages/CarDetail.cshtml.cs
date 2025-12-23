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

    /// <summary>
    /// Handles GET requests to /car/{id} route.
    /// Dynamisch ophalen van auto details op basis van route parameter.
    /// 
    /// BUG FIX: Deze methode leest de 'id' route parameter uit en haalt de juiste auto op via de API.
    /// VOOR: Mogelijk hardcoded ID of verkeerde parameter mapping leidde tot altijd dezelfde auto (BMW X5).
    /// NU: De route parameter {id} wordt correct gemapped naar de int id parameter, 
    ///     en gebruikt om GET /api/cars/{id} aan te roepen voor de juiste auto.
    /// </summary>
    public async Task<IActionResult> OnGetAsync(int id)
    {
        try
        {
            // BUG FIX: Gebruik de id route parameter om de juiste auto op te halen
            // Deze id komt van de URL (/car/{id}) en wordt dynamisch doorgegeven vanuit de recommendation kaart
            Car = await _apiClient.GetCarByIdAsync(id);
            
            if (Car == null)
            {
                ErrorMessage = "Auto niet gevonden.";
                return Page();
            }

            // Haal alle images op voor deze auto (gebruikt dezelfde id parameter)
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

