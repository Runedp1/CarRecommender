using Microsoft.AspNetCore.Mvc;
using CarRecommender;

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
    private readonly ICarRepository? _carRepository;

    public HealthController(ICarRepository? carRepository = null)
    {
        _carRepository = carRepository;
    }

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

    /// <summary>
    /// GET /api/health/info
    /// Retourneert informatie over de geladen dataset.
    /// </summary>
    [HttpGet("info")]
    [ProducesResponseType(typeof(HealthInfo), StatusCodes.Status200OK)]
    public IActionResult GetInfo()
    {
        var cars = _carRepository?.GetAllCars() ?? new List<Car>();
        var info = new HealthInfo
        {
            Status = "OK",
            TotalCars = cars.Count,
            DatasetLoaded = _carRepository != null,
            ExpectedCars = 65128, // Verwacht aantal uit df_master_v8_def.csv
            Warning = cars.Count < 10000 ? $"⚠️ Slechts {cars.Count} auto's geladen - verwacht ~65128. Mogelijk wordt een oud/ander bestand geladen!" : null
        };

        return Ok(info);
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

/// <summary>
/// Health info response model met dataset informatie.
/// </summary>
public class HealthInfo
{
    /// <summary>
    /// Status van de API.
    /// </summary>
    public string Status { get; set; } = "OK";

    /// <summary>
    /// Aantal auto's in de geladen dataset.
    /// </summary>
    public int TotalCars { get; set; }

    /// <summary>
    /// Of de dataset succesvol is geladen.
    /// </summary>
    public bool DatasetLoaded { get; set; }

    /// <summary>
    /// Verwacht aantal auto's in de dataset.
    /// </summary>
    public int ExpectedCars { get; set; }

    /// <summary>
    /// Waarschuwing als er een probleem is met de dataset.
    /// </summary>
    public string? Warning { get; set; }
}

