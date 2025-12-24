using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CarRecommender.Web.Services;
using CarRecommender.Web.Models;

namespace CarRecommender.Web.Pages;

public class IndexModel : PageModel
{
    private readonly CarApiClient _apiClient;
    private readonly ILogger<IndexModel> _logger;
    private readonly IConfiguration _configuration;

    [BindProperty]
    public string? SearchText { get; set; }

    public List<RecommendationResult>? Recommendations { get; set; }
    public string? ErrorMessage { get; set; }
    public string ApiBaseUrl { get; set; } = string.Empty;

    public IndexModel(CarApiClient apiClient, ILogger<IndexModel> logger, IConfiguration configuration)
    {
        _apiClient = apiClient;
        _logger = logger;
        _configuration = configuration;
        ApiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? string.Empty;
    }
    
    /// <summary>
    /// Genereert een volledige image URL voor een auto.
    /// 
    /// BUG FIX: Deze methode zorgt ervoor dat elke auto een unieke, deterministische image URL krijgt.
    /// 
    /// Strategie:
    /// 1. Gebruik ImageUrl van API als primaire bron (als die bestaat en niet leeg is)
    /// 2. Als ImageUrl relatief is (begint met /), voeg API base URL toe voor volledige URL
    /// 3. Als ImageUrl al volledig is (http/https), gebruik die direct
    /// 4. Als geen ImageUrl beschikbaar is, retourneer null zodat SVG fallback wordt gebruikt
    /// 
    /// Waarom dit werkt:
    /// - De backend (CarRepository.AssignImagePaths) vult ImageUrl voor elke auto bij het laden
    /// - ImageUrl bevat relatief pad zoals "/images/Acura_MDX_2011_...jpg" als match gevonden is
    /// - Frontend voegt API base URL toe om volledige URL te maken: "http://localhost:5000/images/..."
    /// - Elke auto krijgt zijn eigen ImageUrl op basis van merk+model matching
    /// </summary>
    public string? GetCarImageUrl(string? imageUrl, string brand, string model, int carId)
    {
        // BUG FIX: Filter externe URLs die stockfoto's tonen - alleen lokale image paths of null gebruiken
        // PROBLEEM: Backend geeft soms externe URLs (auto-data.net, unsplash, etc.) die stockfoto's tonen
        // OPLOSSING: Negeer externe URLs die niet van onze eigen API komen, retourneer null voor SVG fallback
        
        if (!string.IsNullOrWhiteSpace(imageUrl))
        {
            // BUG FIX: Als ImageUrl een externe URL is (niet van onze eigen API), negeer deze
            // Externe services zoals auto-data.net, unsplash, etc. geven vaak stockfoto's die niet bij de auto horen
            if (imageUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || 
                imageUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                // Alleen onze eigen API base URL accepteren, alle andere externe URLs negeren
                if (!string.IsNullOrEmpty(ApiBaseUrl) && imageUrl.StartsWith(ApiBaseUrl, StringComparison.OrdinalIgnoreCase))
                {
                    // Dit is van onze eigen API - gebruik deze
                    return imageUrl;
                }
                // Externe URL die niet van onze API komt - negeer en gebruik SVG fallback
                _logger.LogWarning("Externe image URL genegeerd voor {Brand} {Model} (ID: {CarId}): {ImageUrl}. Gebruikt SVG fallback.", 
                    brand, model, carId, imageUrl);
                return null;
            }
            
            // BUG FIX: Als ImageUrl relatief pad is (begint met /), voeg API base URL toe
            // Bijvoorbeeld: "/images/Acura_MDX_2011_...jpg" -> "http://localhost:5000/images/Acura_MDX_2011_...jpg"
            if (imageUrl.StartsWith("/"))
            {
                // Verwijder trailing slash van base URL als die er is
                var baseUrl = ApiBaseUrl.TrimEnd('/');
                if (!string.IsNullOrEmpty(baseUrl))
                {
                    return $"{baseUrl}{imageUrl}";
                }
                // Als ApiBaseUrl leeg is, gebruik relatief pad (werkt als frontend en backend opzelfde domain)
                return imageUrl;
            }
        }
        
        // BUG FIX: Stap 2 - Geen foto beschikbaar
        // Als ImageUrl leeg/null is, betekent dit dat backend geen match heeft gevonden
        // Retourneer null zodat frontend SVG fallback gebruikt (professionele auto icon)
        // GEEN willekeurige foto's - alleen echte auto foto's of SVG fallback
        return null;
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
            else
            {
                // BUG FIX: Debug logging om te zien of ImageUrl correct wordt doorgegeven
                // Log eerste paar recommendations om te controleren of ImageUrl gevuld is
                foreach (var rec in Recommendations.Take(3))
                {
                    _logger.LogInformation(
                        "Recommendation: {Brand} {Model} (ID: {CarId}) - ImageUrl: '{ImageUrl}'", 
                        rec.Car.Brand, 
                        rec.Car.Model, 
                        rec.Car.Id, 
                        rec.Car.ImageUrl ?? "(null)");
                }
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
