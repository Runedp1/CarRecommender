using Microsoft.AspNetCore.Mvc;

namespace CarRecommender.Api.Controllers;

/// <summary>
/// Home Controller voor het root endpoint.
/// Dit endpoint geeft een overzicht van de beschikbare API endpoints.
/// Werkt zowel lokaal als in Azure Production.
/// </summary>
[ApiController]
[Route("/")]
public class HomeController : ControllerBase
{
    /// <summary>
    /// GET /
    /// Retourneert een welkomstbericht met een overzicht van beschikbare endpoints.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult Get()
    {
        // Haal de base URL op (werkt zowel lokaal als in Azure)
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        
        return Ok(new
        {
            welkom = "Welkom bij de Car Recommender API!",
            beschrijving = "Deze API helpt je bij het vinden van auto's op basis van je voorkeuren.",
            endpoints = new
            {
                health = $"{baseUrl}/api/health",
                cars = $"{baseUrl}/api/cars",
                carDetails = $"{baseUrl}/api/cars/{{id}}",
                recommendations = $"{baseUrl}/api/recommendations/{{id}}?top=5",
                recommendationsFromText = $"{baseUrl}/api/recommendations/text"
            },
            voorbeelden = new
            {
                lijstVanAutos = $"{baseUrl}/api/cars",
                autoDetails = $"{baseUrl}/api/cars/1",
                aanbevelingenVoorAuto = $"{baseUrl}/api/recommendations/1?top=5"
            }
        });
    }
}


