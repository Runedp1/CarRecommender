using Microsoft.Extensions.Hosting;
using CarRecommender;

namespace CarRecommender.Api.Services;

/// <summary>
/// Background service die ML.NET model traint in de achtergrond na applicatie opstart.
/// Dit voorkomt dat training de opstarttijd blokkeert.
/// 
/// ML.NET Training:
/// - Start automatisch 5 seconden na applicatie opstart
/// - Gebruikt sample van 50 auto's voor training data generatie
/// - Traint ML.NET regression model op basis van recommendation scores
/// - Model wordt actief zodra training voltooid is
/// </summary>
public class MlModelTrainingBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MlModelTrainingBackgroundService> _logger;
    private readonly MlRecommendationService _mlService;

    public MlModelTrainingBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<MlModelTrainingBackgroundService> logger,
        MlRecommendationService mlService)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mlService = mlService ?? throw new ArgumentNullException(nameof(mlService));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wacht 5 seconden na opstart zodat applicatie volledig geladen is
        Console.WriteLine("[ML Training] Wacht 5 seconden voor applicatie initialisatie...");
        _logger.LogInformation("[ML Training] Wacht 5 seconden voor applicatie initialisatie...");
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        Console.WriteLine("[ML Training] ⏳ Check ML.NET model status...");
        _logger.LogInformation("[ML Training] ⏳ Check ML.NET model status...");

        try
        {
            // Probeer eerst een bestaand model te laden (als het bestaat)
            Console.WriteLine("[ML Training] 🔍 Probeer bestaand model te laden...");
            _logger.LogInformation("[ML Training] 🔍 Probeer bestaand model te laden...");
            
            bool modelLoaded = _mlService.LoadModel();
            
            if (modelLoaded && _mlService.IsModelTrained)
            {
                var stats = _mlService.GetModelStatistics();
                Console.WriteLine($"[ML Training] ✅✅✅ Bestaand ML.NET model geladen! (getraind op {stats.LastTrainingTime:dd/MM/yyyy HH:mm:ss})");
                _logger.LogInformation("[ML Training] ✅✅✅ Bestaand ML.NET model geladen! (getraind op {LastTraining})", stats.LastTrainingTime);
                return; // Model geladen, geen training nodig
            }
            
            // Geen model gevonden, train een nieuw model
            Console.WriteLine("[ML Training] ⏳ Geen bestaand model gevonden. Start ML.NET model training in achtergrond...");
            _logger.LogInformation("[ML Training] ⏳ Geen bestaand model gevonden. Start ML.NET model training in achtergrond...");

            // Maak scope voor scoped services
            using (var scope = _serviceProvider.CreateScope())
            {
                var carRepository = scope.ServiceProvider.GetRequiredService<ICarRepository>();
                var recommendationService = scope.ServiceProvider.GetRequiredService<IRecommendationService>();

                var allCars = carRepository.GetAllCars();
                Console.WriteLine($"[ML Training] 📊 Geladen {allCars.Count} auto's voor training");
                _logger.LogInformation("[ML Training] 📊 Geladen {Count} auto's voor training", allCars.Count);

                if (allCars.Count == 0)
                {
                    Console.WriteLine("[ML Training] ⚠️ Geen auto's beschikbaar voor training");
                    _logger.LogWarning("[ML Training] ⚠️ Geen auto's beschikbaar voor training");
                    return;
                }

                // Genereer training data (kleinere sample voor performance)
                // Gebruik random sampling voor betere representatie van de dataset
                var trainingResults = new List<RecommendationResult>();
                var sampleSize = Math.Min(50, allCars.Count); // Max 50 auto's voor training
                
                // Filter eerst geldige auto's (met geldige data)
                var validCars = allCars
                    .Where(c => c.Power > 0 && c.Budget > 0 && c.Year >= 1900)
                    .ToList();
                
                if (validCars.Count == 0)
                {
                    Console.WriteLine("[ML Training] ⚠️ Geen geldige auto's beschikbaar voor training");
                    _logger.LogWarning("[ML Training] ⚠️ Geen geldige auto's beschikbaar voor training");
                    return;
                }
                
                // Random sample voor betere representatie (met seed voor reproduceerbaarheid)
                // Seed = 42 zorgt ervoor dat dezelfde random selectie wordt gebruikt bij elke training
                // Dit maakt training reproduceerbaar terwijl het nog steeds representatief is
                var random = new Random(42);
                var sampledCars = validCars
                    .OrderBy(x => random.Next())
                    .Take(sampleSize)
                    .ToList();
                
                Console.WriteLine($"[ML Training] 🔄 Genereer training data van {sampledCars.Count} willekeurige auto's (uit {validCars.Count} geldige auto's)...");
                _logger.LogInformation("[ML Training] 🔄 Genereer training data van {SampleSize} willekeurige auto's (uit {ValidCount} geldige auto's)...", 
                    sampledCars.Count, validCars.Count);

                int processedCount = 0;
                for (int i = 0; i < sampledCars.Count; i++)
                {
                    if (stoppingToken.IsCancellationRequested)
                        break;

                    var targetCar = sampledCars[i];

                    // Genereer recommendations voor deze auto
                    var recommendations = recommendationService.RecommendSimilarCars(targetCar, 10);
                    trainingResults.AddRange(recommendations);
                    processedCount++;

                    if (processedCount % 10 == 0)
                    {
                        Console.WriteLine($"[ML Training] 📈 Verwerkt {processedCount}/{sampledCars.Count} auto's... ({trainingResults.Count} recommendations gegenereerd)");
                        _logger.LogInformation("[ML Training] 📈 Verwerkt {Progress}/{Total} auto's... ({Recommendations} recommendations gegenereerd)", 
                            processedCount, sampledCars.Count, trainingResults.Count);
                    }
                }

                if (trainingResults.Count > 0)
                {
                    Console.WriteLine($"[ML Training] ✅ Training data gegenereerd: {trainingResults.Count} recommendations");
                    _logger.LogInformation("[ML Training] ✅ Training data gegenereerd: {Count} recommendations", trainingResults.Count);
                    
                    // Train ML model (gebruik de gedeelde singleton instantie)
                    Console.WriteLine("[ML Training] 🧠 Start ML.NET model training...");
                    _logger.LogInformation("[ML Training] 🧠 Start ML.NET model training...");
                    _mlService.TrainModel(allCars, trainingResults);
                    
                    if (_mlService.IsModelTrained)
                    {
                        Console.WriteLine("[ML Training] ✅✅✅ ML.NET model succesvol getraind! Model is nu actief.");
                        _logger.LogInformation("[ML Training] ✅✅✅ ML.NET model succesvol getraind! Model is nu actief.");
                    }
                    else
                    {
                        Console.WriteLine("[ML Training] ⚠️ ML.NET model training mislukt - recommendations werken zonder ML component");
                        _logger.LogWarning("[ML Training] ⚠️ ML.NET model training mislukt - recommendations werken zonder ML component");
                    }
                }
                else
                {
                    Console.WriteLine("[ML Training] ⚠️ Geen training data gegenereerd - skip training");
                    _logger.LogWarning("[ML Training] ⚠️ Geen training data gegenereerd - skip training");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ML Training] ❌ Fout tijdens ML.NET model training: {ex.Message}");
            _logger.LogError(ex, "[ML Training] ❌ Fout tijdens ML.NET model training");
        }
    }
}

