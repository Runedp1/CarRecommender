using Microsoft.AspNetCore.Mvc;
using CarRecommender;

namespace CarRecommender.Api.Controllers;

/// <summary>
/// API Controller voor auto data endpoints.
/// Deze controller handelt alle HTTP requests af voor auto informatie.
/// 
/// API laag (Presentation layer):
/// - Deze laag ontvangt HTTP requests en geeft JSON responses terug
/// - Gebruikt dependency injection om ICarRepository te krijgen
/// - Valideert input en geeft foutmeldingen terug als iets misgaat
/// 
/// Azure Deployment Uitleg:
/// - Deze controller werkt identiek in Development (lokaal) en Production (Azure)
/// - Via dependency injection krijgt het ICarRepository, die data uit CSV laadt
/// - In Azure wordt de CSV automatisch meegenomen tijdens deployment
/// - Foutafhandeling zorgt voor nette HTTP status codes (404 voor niet-bestaande auto's, 500 voor serverfouten)
/// - Logging gebeurt automatisch naar Azure App Service logs via ILogger
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CarsController : ControllerBase
{
    private readonly ICarRepository _carRepository;
    private readonly ILogger<CarsController> _logger;

    /// <summary>
    /// Constructor - krijgt ICarRepository en ILogger via dependency injection.
    /// Dependency injection zorgt ervoor dat Azure automatisch de juiste services injecteert.
    /// </summary>
    public CarsController(ICarRepository carRepository, ILogger<CarsController> logger)
    {
        _carRepository = carRepository ?? throw new ArgumentNullException(nameof(carRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// GET /api/cars
    /// Haalt een lijst van alle auto's op, met optionele paginatie.
    /// 
    /// Query parameters:
    /// - page: Pagina nummer (standaard 1)
    /// - pageSize: Aantal items per pagina (standaard 20, max 100)
    /// 
    /// Azure: Dit endpoint werkt in Production zonder Swagger UI.
    /// Fouten worden netjes afgehandeld: 400 voor ongeldige parameters, 500 voor serverfouten.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<Car>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult GetCars([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            // Valideer paginatie parameters
            if (page < 1)
            {
                return BadRequest(new { error = "Page moet minimaal 1 zijn." });
            }

            if (pageSize < 1 || pageSize > 100)
            {
                return BadRequest(new { error = "PageSize moet tussen 1 en 100 zijn." });
            }

            // Haal alle auto's op
            var allCars = _carRepository.GetAllCars();
            var totalCount = allCars.Count;

            // Paginatie toepassen
            var pagedCars = allCars
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Maak response met paginatie metadata
            var result = new PagedResult<Car>
            {
                Items = pagedCars,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            // Log de exception voor Azure App Service logs
            _logger.LogError(ex, "Fout bij ophalen van auto's lijst (page={Page}, pageSize={PageSize})", page, pageSize);
            // Exception wordt opgevangen door globale exception handler die 500 teruggeeft
            throw;
        }
    }

    /// <summary>
    /// GET /api/cars/{id}
    /// Haalt details op van één specifieke auto op basis van ID.
    /// 
    /// Azure: Retourneert 404 NotFound met duidelijke foutmelding als auto niet bestaat.
    /// Dit is belangrijk voor Azure monitoring - 404 betekent "niet gevonden", niet "serverfout".
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Car), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult GetCarById(int id)
    {
        try
        {
            var car = _carRepository.GetCarById(id);

            if (car == null)
            {
                // Log dat auto niet gevonden is (niveau Warning, niet Error - dit is normaal gedrag)
                _logger.LogWarning("Auto met ID {CarId} niet gevonden", id);
                return NotFound(new { error = $"Auto met ID {id} niet gevonden." });
            }

            return Ok(car);
        }
        catch (Exception ex)
        {
            // Log de exception voor Azure App Service logs
            _logger.LogError(ex, "Fout bij ophalen van auto met ID {CarId}", id);
            // Exception wordt opgevangen door globale exception handler die 500 teruggeeft
            throw;
        }
    }
}

/// <summary>
/// Helper class voor paginatie resultaten.
/// </summary>
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}

