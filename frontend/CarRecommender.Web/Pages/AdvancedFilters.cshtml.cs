using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CarRecommender.Web.Services;
using CarRecommender.Web.Models;

namespace CarRecommender.Web.Pages;

/// <summary>
/// Page model voor Geavanceerde Filters pagina.
/// 
/// VERSCHIL MET TEKST MODUS (Index.cshtml):
/// - Tekst modus: Gebruiker beschrijft wensen in vrije tekst, TextParserService parseert dit
/// - Manuele modus: Gebruiker geeft expliciet alle filters op via dropdowns en inputvelden
/// - Geen tekst parsing nodig, directe mapping naar API request
/// </summary>
public class AdvancedFiltersModel : PageModel
{
    private readonly CarApiClient _apiClient;
    private readonly ILogger<AdvancedFiltersModel> _logger;
    private readonly IConfiguration _configuration;

    [BindProperty]
    public ManualFilterRequest? FilterRequest { get; set; }

    public List<RecommendationResult>? Recommendations { get; set; }
    public string? ErrorMessage { get; set; }
    public string ApiBaseUrl { get; set; } = string.Empty;

    // Dropdown opties 
    public List<string> Brands { get; set; } = new List<string>
    {
        "", "BMW", "Audi", "Mercedes-Benz", "Volkswagen", "Ford", "Opel", "Peugeot", 
        "Citroen", "Renault", "Toyota", "Honda", "Nissan", "Mazda", "Volvo", 
        "Skoda", "Seat", "Fiat", "Alfa Romeo", "Jaguar", "Land Rover", "Mini", 
        "Porsche", "Tesla", "Hyundai", "Kia", "Lexus", "Dacia", "Suzuki", 
        "Mitsubishi", "Subaru"
    };

    public List<string> FuelTypes { get; set; } = new List<string>
    {
        "", "petrol", "diesel", "hybrid"
    };

    public List<string> TransmissionTypes { get; set; } = new List<string>
    {
        "", "Automaat", "Schakel"
    };

    public List<string> BodyTypes { get; set; } = new List<string>
    {
        "", "suv", "sedan", "hatchback", "station", "cabrio", "coupe"
    };

    public AdvancedFiltersModel(CarApiClient apiClient, ILogger<AdvancedFiltersModel> logger, IConfiguration configuration)
    {
        _apiClient = apiClient;
        _logger = logger;
        _configuration = configuration;
        ApiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? string.Empty;
    }

    public void OnGet()
    {
        // Initialiseer leeg filter request
        FilterRequest = new ManualFilterRequest
        {
            Top = 5
        };
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (FilterRequest == null)
        {
            ErrorMessage = "Filter request is ongeldig.";
            return Page();
        }

        // Valideer dat er ten minste één filter is ingesteld
        bool hasAnyFilter = 
            FilterRequest.MinPrice.HasValue ||
            FilterRequest.MaxPrice.HasValue ||
            !string.IsNullOrWhiteSpace(FilterRequest.Brand) ||
            !string.IsNullOrWhiteSpace(FilterRequest.Model) ||
            !string.IsNullOrWhiteSpace(FilterRequest.Fuel) ||
            FilterRequest.Transmission.HasValue ||
            !string.IsNullOrWhiteSpace(FilterRequest.BodyType) ||
            FilterRequest.MinYear.HasValue ||
            FilterRequest.MaxYear.HasValue ||
            FilterRequest.MinPower.HasValue;

        if (!hasAnyFilter)
        {
            ErrorMessage = "Stel ten minste één filter in om te zoeken.";
            return Page();
        }

        // Converteer transmissie string naar bool
        if (!string.IsNullOrWhiteSpace(Request.Form["TransmissionString"]))
        {
            string transStr = Request.Form["TransmissionString"].ToString();
            if (transStr == "Automaat")
                FilterRequest.Transmission = true;
            else if (transStr == "Schakel")
                FilterRequest.Transmission = false;
        }

        // Zet standaard top waarde
        if (!FilterRequest.Top.HasValue || FilterRequest.Top.Value < 1)
        {
            FilterRequest.Top = 5;
        }

        try
        {
            Recommendations = await _apiClient.GetRecommendationsFromManualFiltersAsync(FilterRequest);
            
            if (Recommendations == null || Recommendations.Count == 0)
            {
                ErrorMessage = "Geen auto's gevonden die voldoen aan uw criteria. Probeer het opnieuw met andere filters.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fout bij het ophalen van recommendations met manuele filters");
            ErrorMessage = "Er is een fout opgetreden bij het verbinden met de API. Controleer of de API bereikbaar is.";
        }

        return Page();
    }

    /// <summary>
    /// Genereert een volledige image URL voor een auto (zelfde logica als Index.cshtml.cs).
    /// </summary>
    public string? GetCarImageUrl(string? imageUrl, string brand, string model, int carId)
    {
        if (!string.IsNullOrWhiteSpace(imageUrl))
        {
            if (imageUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || 
                imageUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrEmpty(ApiBaseUrl) && imageUrl.StartsWith(ApiBaseUrl, StringComparison.OrdinalIgnoreCase))
                {
                    return imageUrl;
                }
                return null;
            }
            
            if (imageUrl.StartsWith("/"))
            {
                var baseUrl = ApiBaseUrl.TrimEnd('/');
                if (!string.IsNullOrEmpty(baseUrl))
                {
                    return $"{baseUrl}{imageUrl}";
                }
                return imageUrl;
            }
        }
        
        return null;
    }

    /// <summary>
    /// Formatteert transmissie string naar Nederlandse termen.
    /// </summary>
    public string FormatTransmission(string? transmission)
    {
        if (string.IsNullOrWhiteSpace(transmission))
            return "Niet opgegeven";
        
        string lower = transmission.ToLower().Trim();
        
        if (lower.Contains("automatic") || lower.Contains("automaat") || lower.Contains("automatisch") || lower.Contains("cvt") || lower.Contains("dct"))
            return "Automaat";
        
        if (lower.Contains("manual") || lower.Contains("handmatig") || lower.Contains("schakel") || lower.Contains("handbak"))
            return "Handmatig";
        
        return char.ToUpper(transmission[0]) + (transmission.Length > 1 ? transmission.Substring(1).ToLower() : "");
    }
    
    /// <summary>
    /// Dit is niet meer nodig dus simpel op * 1.0 gezet
    /// </summary>
    public int ConvertKwToHp(int powerKw)
    {
        return (int)Math.Round(powerKw * 1.0);
    }

    /// <summary>
    /// Formatteert body type naar Nederlandse termen met hoofdletters.
    /// </summary>
    public string FormatBodyType(string? bodyType)
    {
        if (string.IsNullOrWhiteSpace(bodyType))
            return "Niet opgegeven";
        
        string lower = bodyType.ToLower().Trim();
        
        // Map naar Nederlandse termen met hoofdletters
        return lower switch
        {
            "suv" => "SUV",
            "sedan" => "Sedan",
            "hatchback" => "Hatchback",
            "station" => "Station",
            "cabrio" => "Cabrio",
            "coupe" => "Coupé",
            "wagon" => "Station",
            "convertible" => "Cabrio",
            _ => char.ToUpper(bodyType[0]) + (bodyType.Length > 1 ? bodyType.Substring(1).ToLower() : "")
        };
    }
}








