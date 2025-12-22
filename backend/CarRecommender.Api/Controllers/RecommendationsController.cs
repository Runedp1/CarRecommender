using Microsoft.AspNetCore.Mvc;
using CarRecommender;

namespace CarRecommender.Api.Controllers;

/// <summary>
/// API Controller voor recommendation endpoints.
/// Deze controller handelt alle HTTP requests af voor auto aanbevelingen.
/// 
/// API laag (Presentation layer):
/// - Deze laag ontvangt HTTP requests en geeft JSON responses terug
/// - Gebruikt dependency injection om IRecommendationService en ICarRepository te krijgen
/// - Valideert input en geeft foutmeldingen terug als iets misgaat
/// 
/// Azure Deployment Uitleg:
/// - Deze controller gebruikt de recommendation business logica (RecommendationService)
/// - De business logica werkt identiek in Development en Production
/// - Via dependency injection krijgen we automatisch de juiste services (singleton voor repository, scoped voor service)
/// - Foutafhandeling zorgt voor nette HTTP status codes:
///   - 404: Auto niet gevonden (duidelijke foutmelding)
///   - 400: Ongeldige request parameters
///   - 500: Onverwachte serverfouten (exception wordt gelogd maar niet naar client gestuurd)
/// - Logging gebeurt automatisch naar Azure App Service logs
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class RecommendationsController : ControllerBase
{
    private readonly IRecommendationService _recommendationService;
    private readonly ICarRepository _carRepository;
    private readonly ILogger<RecommendationsController> _logger;

    /// <summary>
    /// Constructor - krijgt services via dependency injection.
    /// Azure injecteert automatisch de juiste services bij het starten van de Web App.
    /// </summary>
    public RecommendationsController(
        IRecommendationService recommendationService,
        ICarRepository carRepository,
        ILogger<RecommendationsController> logger)
    {
        _recommendationService = recommendationService ?? throw new ArgumentNullException(nameof(recommendationService));
        _carRepository = carRepository ?? throw new ArgumentNullException(nameof(carRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// GET /api/recommendations/{id}?top=5
    /// Haalt top-N recommendations op voor een specifieke auto op basis van ID.
    /// 
    /// Query parameters:
    /// - top: Aantal recommendations om terug te geven (standaard 5, max 20)
    /// 
    /// Azure: Werkt in Production zonder Swagger. Retourneert 404 als auto niet bestaat.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(List<RecommendationResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult GetRecommendations(int id, [FromQuery] int top = 5)
    {
        try
        {
            // Valideer top parameter
            if (top < 1 || top > 20)
            {
                return BadRequest(new { error = "Top parameter moet tussen 1 en 20 zijn." });
            }

            // Haal target auto op
            var targetCar = _carRepository.GetCarById(id);
            if (targetCar == null)
            {
                // Log dat auto niet gevonden is (niveau Warning, niet Error)
                _logger.LogWarning("Recommendations gevraagd voor niet-bestaande auto ID {CarId}", id);
                return NotFound(new { error = $"Auto met ID {id} niet gevonden." });
            }

            // Genereer recommendations via business logica service
            var recommendations = _recommendationService.RecommendSimilarCars(targetCar, top);

            return Ok(recommendations);
        }
        catch (Exception ex)
        {
            // Log de exception voor Azure App Service logs
            _logger.LogError(ex, "Fout bij genereren van recommendations voor auto ID {CarId} met top={Top}", id, top);
            // Exception wordt opgevangen door globale exception handler die 500 teruggeeft
            throw;
        }
    }

    /// <summary>
    /// POST /api/recommendations/text
    /// Genereert recommendations op basis van tekst input van de gebruiker.
    /// 
    /// Request body:
    /// {
    ///   "text": "Ik zou liever een automaat hebben met veel vermogen, max 25k euro",
    ///   "top": 5
    /// }
    /// 
    /// Azure: Dit endpoint gebruikt NLP (Natural Language Processing) om tekst te parsen.
    /// De TextParserService extraheert voorkeuren uit Nederlandse tekst en genereert recommendations.
    /// Werkt identiek in Development en Production - alleen logging verschilt.
    /// </summary>
    [HttpPost("text")]
    [ProducesResponseType(typeof(List<RecommendationResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult GetRecommendationsFromText([FromBody] TextRecommendationRequest request)
    {
        try
        {
            // Valideer request
            if (request == null || string.IsNullOrWhiteSpace(request.Text))
            {
                return BadRequest(new { error = "Text veld is verplicht." });
            }

            // Valideer top parameter
            int top = request.Top ?? 5;
            if (top < 1 || top > 20)
            {
                return BadRequest(new { error = "Top parameter moet tussen 1 en 20 zijn." });
            }

            // Genereer recommendations op basis van tekst via business logica service
            // Deze service gebruikt TextParserService voor NLP parsing en RecommendationEngine voor similarity berekening
            var recommendations = _recommendationService.RecommendFromText(request.Text, top);

            return Ok(recommendations);
        }
        catch (Exception ex)
        {
            // Log de exception voor Azure App Service logs
            _logger.LogError(ex, "Fout bij tekst-gebaseerde recommendations voor tekst: {Text}", 
                request?.Text?.Substring(0, Math.Min(50, request.Text?.Length ?? 0)));
            // Exception wordt opgevangen door globale exception handler die 500 teruggeeft
            throw;
        }
    }
}

/// <summary>
/// Request model voor tekst-gebaseerde recommendations.
/// </summary>
public class TextRecommendationRequest
{
    /// <summary>
    /// Tekst input van de gebruiker met voorkeuren (bijv. "Ik zou liever een automaat hebben met veel vermogen, max 25k euro").
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Aantal recommendations om terug te geven (optioneel, standaard 5).
    /// </summary>
    public int? Top { get; set; }
}

