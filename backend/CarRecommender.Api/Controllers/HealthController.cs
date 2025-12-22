using Microsoft.AspNetCore.Mvc;

namespace CarRecommender.Api.Controllers;

/// <summary>
/// Health check endpoint voor monitoring en deployment verificatie.
/// Dit endpoint wordt gebruikt door Azure App Service om te controleren of de API draait.
/// 
/// API laag (Presentation layer):
/// - Eenvoudig endpoint dat de status van de applicatie teruggeeft
/// - Geen business logica, alleen status informatie
/// 
/// Azure Deployment Uitleg:
/// - Azure App Service gebruikt dit endpoint automatisch voor health checks
/// - Als dit endpoint 200 OK teruggeeft, weet Azure dat de applicatie gezond is
/// - Dit endpoint werkt altijd, zelfs als andere endpoints problemen hebben
/// - Geen logging nodig - dit is een simpele status check
/// - Belangrijk: Dit endpoint moet snel zijn en geen dependencies hebben
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    /// <summary>
    /// GET /api/health
    /// Retourneert de status van de API.
    /// 
    /// Azure: Dit endpoint wordt gebruikt door Azure App Service voor health monitoring.
    /// Als dit endpoint 200 OK teruggeeft, weet Azure dat de applicatie draait.
    /// Test dit endpoint na deployment: https://&lt;jouw-app-name&gt;.azurewebsites.net/api/health
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(HealthStatus), StatusCodes.Status200OK)]
    public IActionResult GetHealth()
    {
        // Simpele health check - geen dependencies, altijd snel
        // Azure gebruikt dit om te controleren of de Web App draait
        return Ok(new HealthStatus { Status = "OK" });
    }
}

/// <summary>
/// Health status response model.
/// </summary>
public class HealthStatus
{
    /// <summary>
    /// Status van de API ("OK" als alles goed werkt).
    /// </summary>
    public string Status { get; set; } = "OK";
}

