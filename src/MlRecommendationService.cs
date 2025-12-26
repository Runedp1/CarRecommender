using Microsoft.ML;
using Microsoft.ML.Data;

namespace CarRecommender;

/// <summary>
/// ML.NET service voor machine learning-gebaseerde recommendation optimalisatie.
/// 
/// Deze service gebruikt ML.NET om:
/// - Recommendation scores te optimaliseren op basis van car features
/// - Populairiteit te voorspellen op basis van historische patterns
/// - User rating componenten te berekenen (toekomstig, wanneer ratings beschikbaar zijn)
/// 
/// Voor nu gebruikt het een regression model dat leert van feature patterns.
/// In de toekomst kan dit uitgebreid worden met collaborative filtering wanneer user ratings beschikbaar zijn.
/// </summary>
public class MlRecommendationService
{
    private readonly MLContext _mlContext;
    private ITransformer? _trainedModel;
    private PredictionEngine<CarFeatureData, ScorePrediction>? _predictionEngine;
    private bool _isModelTrained = false;
    private DateTime _lastTrainingTime = DateTime.MinValue;
    private int _trainingDataCount = 0;

    /// <summary>
    /// Input data voor ML model - features van een auto.
    /// </summary>
    public class CarFeatureData
    {
        [LoadColumn(0)]
        public float NormalizedPrice { get; set; }

        [LoadColumn(1)]
        public float NormalizedYear { get; set; }

        [LoadColumn(2)]
        public float NormalizedPower { get; set; }

        [LoadColumn(3)]
        public string Brand { get; set; } = string.Empty;

        [LoadColumn(4)]
        public string Fuel { get; set; } = string.Empty;

        [LoadColumn(5)]
        public string Transmission { get; set; } = string.Empty;

        [LoadColumn(6)]
        public string BodyType { get; set; } = string.Empty;

        [LoadColumn(7)]
        public float Label { get; set; } // Target: de score die we willen voorspellen
    }

    /// <summary>
    /// Output van ML model - voorspelde score.
    /// </summary>
    public class ScorePrediction
    {
        [ColumnName("Score")]
        public float PredictedScore { get; set; }
    }

    public MlRecommendationService()
    {
        // #region agent log
        try
        {
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".cursor", "debug.log");
            var logDir = Path.GetDirectoryName(logPath);
            if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);
            var logEntry = System.Text.Json.JsonSerializer.Serialize(new
            {
                id = $"log_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Guid.NewGuid():N}",
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                location = "MlRecommendationService.cs:65",
                message = "MlRecommendationService constructor start",
                data = new { },
                sessionId = "debug-session",
                runId = "startup",
                hypothesisId = "A"
            });
            File.AppendAllText(logPath, logEntry + Environment.NewLine);
        }
        catch { }
        // #endregion
        try
        {
            // #region agent log
            try
            {
                var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".cursor", "debug.log");
                var logEntry = System.Text.Json.JsonSerializer.Serialize(new
                {
                    id = $"log_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Guid.NewGuid():N}",
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    location = "MlRecommendationService.cs:67",
                    message = "MLContext creation start",
                    data = new { },
                    sessionId = "debug-session",
                    runId = "startup",
                    hypothesisId = "B"
                });
                File.AppendAllText(logPath, logEntry + Environment.NewLine);
            }
            catch { }
            // #endregion
            _mlContext = new MLContext(seed: 0);
            // #region agent log
            try
            {
                var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".cursor", "debug.log");
                var logEntry = System.Text.Json.JsonSerializer.Serialize(new
                {
                    id = $"log_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Guid.NewGuid():N}",
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    location = "MlRecommendationService.cs:69",
                    message = "MLContext creation success",
                    data = new { },
                    sessionId = "debug-session",
                    runId = "startup",
                    hypothesisId = "B"
                });
                File.AppendAllText(logPath, logEntry + Environment.NewLine);
            }
            catch { }
            // #endregion
        }
        catch (Exception ex)
        {
            // #region agent log
            try
            {
                var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".cursor", "debug.log");
                var logEntry = System.Text.Json.JsonSerializer.Serialize(new
                {
                    id = $"log_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Guid.NewGuid():N}",
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    location = "MlRecommendationService.cs:71",
                    message = "MLContext creation failed",
                    data = new { error = ex.Message, stackTrace = ex.StackTrace, type = ex.GetType().Name },
                    sessionId = "debug-session",
                    runId = "startup",
                    hypothesisId = "B"
                });
                File.AppendAllText(logPath, logEntry + Environment.NewLine);
            }
            catch { }
            // #endregion
            throw;
        }
        // #region agent log
        try
        {
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".cursor", "debug.log");
            var logEntry = System.Text.Json.JsonSerializer.Serialize(new
            {
                id = $"log_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Guid.NewGuid():N}",
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                location = "MlRecommendationService.cs:73",
                message = "MlRecommendationService constructor success",
                data = new { },
                sessionId = "debug-session",
                runId = "startup",
                hypothesisId = "A"
            });
            File.AppendAllText(logPath, logEntry + Environment.NewLine);
        }
        catch { }
        // #endregion
    }

    /// <summary>
    /// Traint het ML model op basis van car data en hun scores.
    /// 
    /// Deze methode gebruikt de bestaande recommendation scores als training data
    /// om te leren welke features leiden tot hogere scores.
    /// 
    /// Kan ook user feedback gebruiken voor continue learning.
    /// </summary>
    public void TrainModel(List<Car> cars, List<RecommendationResult> recommendationResults, Dictionary<int, AggregatedFeedback>? userFeedback = null)
    {
        if (cars == null || cars.Count == 0 || recommendationResults == null || recommendationResults.Count == 0)
        {
            // Geen training data beschikbaar - gebruik fallback
            _isModelTrained = false;
            return;
        }

        try
        {
            // Maak training data van cars en hun recommendation scores
            var trainingData = new List<CarFeatureData>();

            foreach (var result in recommendationResults)
            {
                var car = result.Car;
                
                // Skip auto's zonder geldige data
                if (car.Power <= 0 || car.Budget <= 0 || car.Year < 1900)
                    continue;

                // Normaliseer features (gebruik min/max van alle auto's)
                var validCars = cars.Where(c => c.Power > 0 && c.Budget > 0 && c.Year >= 1900).ToList();
                if (validCars.Count == 0)
                    continue;

                int minPower = validCars.Min(c => c.Power);
                int maxPower = validCars.Max(c => c.Power);
                decimal minBudget = validCars.Min(c => c.Budget);
                decimal maxBudget = validCars.Max(c => c.Budget);
                int minYear = validCars.Min(c => c.Year);
                int maxYear = validCars.Max(c => c.Year);

                float normalizedPrice = maxBudget > minBudget 
                    ? (float)((car.Budget - minBudget) / (maxBudget - minBudget))
                    : 0.5f;
                float normalizedYear = maxYear > minYear
                    ? (float)(car.Year - minYear) / (maxYear - minYear)
                    : 0.5f;
                float normalizedPower = maxPower > minPower
                    ? (float)(car.Power - minPower) / (maxPower - minPower)
                    : 0.5f;

                // Bepaal label: gebruik recommendation score, maar pas aan op basis van user feedback
                float label = (float)result.SimilarityScore;
                
                // Als er user feedback is, pas label aan op basis van populairiteit
                if (userFeedback != null && userFeedback.TryGetValue(car.Id, out var feedback))
                {
                    // Combineer recommendation score met populairiteit score
                    // Populairiteit heeft 30% gewicht, recommendation score 70%
                    label = (float)((result.SimilarityScore * 0.7) + (feedback.PopularityScore * 0.3));
                }

                trainingData.Add(new CarFeatureData
                {
                    NormalizedPrice = normalizedPrice,
                    NormalizedYear = normalizedYear,
                    NormalizedPower = normalizedPower,
                    Brand = car.Brand ?? "unknown",
                    Fuel = car.Fuel ?? "unknown",
                    Transmission = car.Transmission ?? "unknown",
                    BodyType = car.BodyType ?? "unknown",
                    Label = label
                });
            }

            if (trainingData.Count < 10)
            {
                // Te weinig training data - gebruik fallback
                _isModelTrained = false;
                return;
            }

            // Converteer naar IDataView
            var dataView = _mlContext.Data.LoadFromEnumerable(trainingData);

            // Feature engineering: combineer numerieke en categorische features
            var dataProcessPipeline = _mlContext.Transforms.Concatenate(
                    "Features",
                    nameof(CarFeatureData.NormalizedPrice),
                    nameof(CarFeatureData.NormalizedYear),
                    nameof(CarFeatureData.NormalizedPower))
                .Append(_mlContext.Transforms.Categorical.OneHotEncoding(
                    outputColumnName: "BrandEncoded",
                    inputColumnName: nameof(CarFeatureData.Brand)))
                .Append(_mlContext.Transforms.Categorical.OneHotEncoding(
                    outputColumnName: "FuelEncoded",
                    inputColumnName: nameof(CarFeatureData.Fuel)))
                .Append(_mlContext.Transforms.Categorical.OneHotEncoding(
                    outputColumnName: "TransmissionEncoded",
                    inputColumnName: nameof(CarFeatureData.Transmission)))
                .Append(_mlContext.Transforms.Categorical.OneHotEncoding(
                    outputColumnName: "BodyTypeEncoded",
                    inputColumnName: nameof(CarFeatureData.BodyType)))
                .Append(_mlContext.Transforms.Concatenate(
                    "AllFeatures",
                    "Features",
                    "BrandEncoded",
                    "FuelEncoded",
                    "TransmissionEncoded",
                    "BodyTypeEncoded"))
                .AppendCacheCheckpoint(_mlContext);

            // Kies trainer: LbfgsPoissonRegression voor goede performance met ML.NET 3.0+
            // Deze trainer is beschikbaar in de standaard ML.NET package
            var trainer = _mlContext.Regression.Trainers.LbfgsPoissonRegression(
                labelColumnName: nameof(CarFeatureData.Label),
                featureColumnName: "AllFeatures");

            var trainingPipeline = dataProcessPipeline.Append(trainer);

            // Train het model
            _trainedModel = trainingPipeline.Fit(dataView);

            // Maak prediction engine voor snelle voorspellingen
            _predictionEngine = _mlContext.Model.CreatePredictionEngine<CarFeatureData, ScorePrediction>(_trainedModel);

            _isModelTrained = true;
            _lastTrainingTime = DateTime.UtcNow;
            _trainingDataCount = trainingData.Count;
        }
        catch (Exception ex)
        {
            // Log error maar gebruik fallback
            Console.WriteLine($"ML model training failed: {ex.Message}");
            _isModelTrained = false;
        }
    }

    /// <summary>
    /// Voorspelt een ML-gebaseerde score voor een auto op basis van zijn features.
    /// 
    /// Deze score kan gebruikt worden als extra component in de finale recommendation score.
    /// </summary>
    public double PredictScore(Car car, List<Car> allCars)
    {
        if (!_isModelTrained || _predictionEngine == null)
        {
            // Geen getraind model - retourneer neutrale score
            return 0.5;
        }

        try
        {
            // Normaliseer features
            var validCars = allCars.Where(c => c.Power > 0 && c.Budget > 0 && c.Year >= 1900).ToList();
            if (validCars.Count == 0)
                return 0.5;

            int minPower = validCars.Min(c => c.Power);
            int maxPower = validCars.Max(c => c.Power);
            decimal minBudget = validCars.Min(c => c.Budget);
            decimal maxBudget = validCars.Max(c => c.Budget);
            int minYear = validCars.Min(c => c.Year);
            int maxYear = validCars.Max(c => c.Year);

            float normalizedPrice = maxBudget > minBudget
                ? (float)((car.Budget - minBudget) / (maxBudget - minBudget))
                : 0.5f;
            float normalizedYear = maxYear > minYear
                ? (float)(car.Year - minYear) / (maxYear - minYear)
                : 0.5f;
            float normalizedPower = maxPower > minPower
                ? (float)(car.Power - minPower) / (maxPower - minPower)
                : 0.5f;

            var input = new CarFeatureData
            {
                NormalizedPrice = normalizedPrice,
                NormalizedYear = normalizedYear,
                NormalizedPower = normalizedPower,
                Brand = car.Brand ?? "unknown",
                Fuel = car.Fuel ?? "unknown",
                Transmission = car.Transmission ?? "unknown",
                BodyType = car.BodyType ?? "unknown",
                Label = 0 // Niet gebruikt voor prediction
            };

            var prediction = _predictionEngine.Predict(input);
            
            // Clamp naar 0-1 bereik
            return Math.Max(0.0, Math.Min(1.0, prediction.PredictedScore));
        }
        catch (Exception ex)
        {
            // Log error maar retourneer neutrale score
            Console.WriteLine($"ML prediction failed: {ex.Message}");
            return 0.5;
        }
    }

    /// <summary>
    /// Berekent een ML-gebaseerde user rating component.
    /// 
    /// Voor nu gebruikt dit de ML score als proxy voor populairiteit.
    /// In de toekomst kan dit uitgebreid worden met echte user ratings.
    /// </summary>
    public double GetUserRatingComponent(int carId, Car car, List<Car> allCars)
    {
        // Gebruik ML score als proxy voor populairiteit/user preference
        // In de toekomst kan dit vervangen worden door echte user ratings
        double mlScore = PredictScore(car, allCars);
        
        // Normaliseer naar 0-1 bereik (ML score is al genormaliseerd)
        return mlScore;
    }

    /// <summary>
    /// Controleert of het model getraind is.
    /// </summary>
    public bool IsModelTrained => _isModelTrained;

    /// <summary>
    /// Retraint het model met nieuwe data (incremental learning).
    /// Combineert oude training data met nieuwe feedback.
    /// </summary>
    public void RetrainModel(
        List<Car> cars, 
        List<RecommendationResult> newRecommendations, 
        Dictionary<int, AggregatedFeedback>? userFeedback = null)
    {
        // Voor incremental learning: combineer nieuwe data met bestaande patterns
        // In een echte implementatie zou je hier oude training data kunnen bewaren
        // Voor nu trainen we opnieuw met alle beschikbare data
        
        TrainModel(cars, newRecommendations, userFeedback);
    }

    /// <summary>
    /// Haalt statistieken op over het getrainde model.
    /// </summary>
    public ModelStatistics GetModelStatistics()
    {
        return new ModelStatistics
        {
            IsTrained = _isModelTrained,
            LastTrainingTime = _lastTrainingTime,
            TrainingDataCount = _trainingDataCount
        };
    }
}

/// <summary>
/// Statistieken over het getrainde ML model.
/// </summary>
public class ModelStatistics
{
    public bool IsTrained { get; set; }
    public DateTime LastTrainingTime { get; set; }
    public int TrainingDataCount { get; set; }
}

