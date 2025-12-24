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
    /// ID PARAMETER FLOW:
    /// ==================
    /// 1. Route: @page "/car/{id}" in CarDetail.cshtml
    ///    - {id} is een route parameter (bijv. /car/123 -> id = 123)
    /// 
    /// 2. Razor Pages routing:
    ///    - URL /car/123 wordt gematcht met @page "/car/{id}"
    ///    - Razor Pages mapt automatisch {id} uit de URL naar de int id parameter hieronder
    /// 
    /// 3. Deze methode ontvangt de id:
    ///    - int id bevat de waarde uit de URL (bijv. 123)
    ///    - Deze id komt van de auto-kaart die linkte naar /car/{id}
    ///    - Elke kaart gebruikt zijn eigen recommendation.Car.Id of car.Id
    /// 
    /// 4. API call:
    ///    - GET /api/cars/{id} wordt aangeroepen met de id parameter
    ///    - De API retourneert de juiste auto data op basis van deze id
    /// 
    /// VOOR: Hardcoded naar één BMW X5 (alle kaarten linkten naar dezelfde auto)
    /// NU: Elke kaart linkt naar zijn eigen detailpagina via unieke car.Id
    /// </summary>
    public async Task<IActionResult> OnGetAsync(int id)
    {
        try
        {
            // ID GEBRUIK: De id parameter komt uit de route {id:int} in de URL
            // Bijvoorbeeld: URL /car/123 -> id = 123
            // Deze id wordt doorgegeven vanuit de auto-kaart via href="/car/@carId"
            // Elke kaart heeft zijn eigen unieke carId uit recommendation.Car.Id of car.Id
            
            // DEBUG: Log de ontvangen ID om te verifiëren dat routing correct werkt
            _logger.LogInformation("CarDetail OnGetAsync aangeroepen met ID: {CarId} (URL: {RequestPath})", id, Request.Path);
            
            // Haal de juiste auto op via de API met de id uit de route
            Car = await _apiClient.GetCarByIdAsync(id);
            
            // DEBUG: Log welke auto is opgehaald
            if (Car != null)
            {
                _logger.LogInformation("Auto opgehaald: {Brand} {Model} (ID: {CarId})", Car.Brand, Car.Model, Car.Id);
            }
            else
            {
                _logger.LogWarning("Geen auto gevonden voor ID: {CarId}", id);
            }
            
            if (Car == null)
            {
                ErrorMessage = "Auto niet gevonden.";
                return Page();
            }

            // Haal alle images op voor deze auto (gebruikt dezelfde id parameter)
            // De id wordt opnieuw gebruikt om GET /api/cars/{id}/images aan te roepen
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

