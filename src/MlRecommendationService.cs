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
    private MLContext? _mlContext;
    private ITransformer? _trainedModel;
    private PredictionEngine<CarFeatureData, ScorePrediction>? _predictionEngine;
    private DataViewSchema? _inputSchema;
    private bool _isModelTrained = false;
    private DateTime _lastTrainingTime = DateTime.MinValue;
    private int _trainingDataCount = 0;
    private bool _isInitialized = false;
    private Exception? _initializationError = null;
    private readonly string? _modelDirectory;

    /// <summary>
    /// Constructor - optioneel model directory voor save/load functionaliteit.
    /// </summary>
    public MlRecommendationService(string? modelDirectory = null)
    {
        _modelDirectory = modelDirectory;
    }

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


    /// <summary>
    /// Initialiseert MLContext lazy - alleen wanneer nodig.
    /// Retourneert false als initialisatie faalt (bijv. native dependencies ontbreken).
    /// </summary>
    private bool EnsureInitialized()
    {
        if (_isInitialized)
            return _initializationError == null;

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
                location = "MlRecommendationService.cs:EnsureInitialized",
                message = "MLContext lazy initialization start",
                data = new { },
                sessionId = "debug-session",
                runId = "startup",
                hypothesisId = "B"
            });
            File.AppendAllText(logPath, logEntry + Environment.NewLine);
        }
        catch { }
        // #endregion

        try
        {
            _mlContext = new MLContext(seed: 0);
            _isInitialized = true;
            _initializationError = null;
            // #region agent log
            try
            {
                var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".cursor", "debug.log");
                var logEntry = System.Text.Json.JsonSerializer.Serialize(new
                {
                    id = $"log_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Guid.NewGuid():N}",
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    location = "MlRecommendationService.cs:EnsureInitialized",
                    message = "MLContext lazy initialization success",
                    data = new { },
                    sessionId = "debug-session",
                    runId = "startup",
                    hypothesisId = "B"
                });
                File.AppendAllText(logPath, logEntry + Environment.NewLine);
            }
            catch { }
            // #endregion
            return true;
        }
        catch (Exception ex)
        {
            _isInitialized = true;
            _initializationError = ex;
            // #region agent log
            try
            {
                var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".cursor", "debug.log");
                var logEntry = System.Text.Json.JsonSerializer.Serialize(new
                {
                    id = $"log_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Guid.NewGuid():N}",
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    location = "MlRecommendationService.cs:EnsureInitialized",
                    message = "MLContext lazy initialization failed - ML.NET disabled",
                    data = new { error = ex.Message, stackTrace = ex.StackTrace, type = ex.GetType().Name },
                    sessionId = "debug-session",
                    runId = "startup",
                    hypothesisId = "B"
                });
                File.AppendAllText(logPath, logEntry + Environment.NewLine);
            }
            catch { }
            // #endregion
            return false; // ML.NET niet beschikbaar, maar app kan doorgaan
        }
    }

    /// <summary>
    /// Traint het ML model op basis van car data en hun scores.
    /// 
    /// Deze methode gebruikt de bestaande recommendation scores als training data
    /// om te leren welke features leiden tot hogere scores.
    /// 
    /// Kan ook user feedback gebruiken voor continue learning.
    /// NOTE: AggregatedFeedback type removed - userFeedback parameter kept for API compatibility but ignored.
    /// </summary>
    public void TrainModel(List<Car> cars, List<RecommendationResult> recommendationResults, Dictionary<int, object>? userFeedback = null)
    {
        // Lazy initialize ML.NET - als het faalt, skip training maar crash niet
        if (!EnsureInitialized())
        {
            _isModelTrained = false;
            return; // ML.NET niet beschikbaar, maar app kan doorgaan
        }

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

                // Bepaal label: gebruik recommendation score
                // NOTE: User feedback support removed - AggregatedFeedback type no longer exists
                float label = (float)result.SimilarityScore;

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
            if (_mlContext == null)
            {
                _isModelTrained = false;
                return; // ML.NET niet beschikbaar
            }
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
            _inputSchema = dataView.Schema; // Bewaar schema voor later gebruik bij save/load

            // Sla model altijd op naar disk (zodat het bij volgende opstart geladen kan worden)
            if (_trainedModel != null && _inputSchema != null)
            {
                try
                {
                    var modelPath = GetModelPath();
                    var modelDir = Path.GetDirectoryName(modelPath);
                    
                    Console.WriteLine($"[ML] Probeer model op te slaan naar: {modelPath}");
                    Console.WriteLine($"[ML] Model directory: {modelDir}");
                    
                    // Zorg dat directory bestaat
                    if (!string.IsNullOrEmpty(modelDir) && !Directory.Exists(modelDir))
                    {
                        Console.WriteLine($"[ML] Maak directory aan: {modelDir}");
                        Directory.CreateDirectory(modelDir);
                    }
                    
                    // Check write permissions
                    if (!string.IsNullOrEmpty(modelDir) && Directory.Exists(modelDir))
                    {
                        try
                        {
                            var testFile = Path.Combine(modelDir, "test_write.tmp");
                            File.WriteAllText(testFile, "test");
                            File.Delete(testFile);
                            Console.WriteLine($"[ML] Write permissions OK voor directory: {modelDir}");
                        }
                        catch (Exception permEx)
                        {
                            Console.WriteLine($"[ML] ERROR: Geen write permissions voor directory {modelDir}: {permEx.Message}");
                            throw;
                        }
                    }
                    
                    // Sla model op
                    _mlContext.Model.Save(_trainedModel, _inputSchema, modelPath);
                    
                    // Verifieer dat bestand bestaat
                    if (File.Exists(modelPath))
                    {
                        var fileInfo = new FileInfo(modelPath);
                        Console.WriteLine($"[ML] ✅ Model succesvol opgeslagen naar: {modelPath}");
                        Console.WriteLine($"[ML] ✅ Model bestandsgrootte: {fileInfo.Length:N0} bytes");
                        Console.WriteLine($"[ML] ✅ Model laatste wijziging: {fileInfo.LastWriteTime}");
                    }
                    else
                    {
                        Console.WriteLine($"[ML] ❌ ERROR: Model bestand bestaat niet na opslaan! Pad: {modelPath}");
                        throw new InvalidOperationException($"Model bestand niet aangemaakt na Save: {modelPath}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ML] ❌ ERROR: Kon model niet opslaan: {ex.Message}");
                    Console.WriteLine($"[ML] ❌ ERROR Stack trace: {ex.StackTrace}");
                    // Gooi exception door zodat we weten dat save gefaald is
                    throw new InvalidOperationException($"Fout bij opslaan ML model: {ex.Message}", ex);
                }
            }

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
        // Lazy initialize ML.NET - als het faalt, retourneer neutrale score
        if (!EnsureInitialized() || !_isModelTrained || _predictionEngine == null)
        {
            // ML.NET niet beschikbaar of geen getraind model - retourneer neutrale score
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
    /// NOTE: AggregatedFeedback type removed - userFeedback parameter kept for API compatibility but ignored.
    /// </summary>
    public void RetrainModel(
        List<Car> cars, 
        List<RecommendationResult> newRecommendations, 
        Dictionary<int, object>? userFeedback = null)
    {
        // Voor incremental learning: combineer nieuwe data met bestaande patterns
        // In een echte implementatie zou je hier oude training data kunnen bewaren
        // Voor nu trainen we opnieuw met alle beschikbare data
        
        TrainModel(cars, newRecommendations, userFeedback);
    }

    /// <summary>
    /// Slaat het getrainde model op naar disk.
    /// </summary>
    /// <param name="modelPath">Pad waar het model moet worden opgeslagen. Als null, gebruikt het de standaard locatie.</param>
    public void SaveModel(string? modelPath = null)
    {
        if (!EnsureInitialized() || _trainedModel == null || _mlContext == null || _inputSchema == null)
        {
            throw new InvalidOperationException("Model is niet getraind. Train eerst het model voordat u het opslaat.");
        }

        try
        {
            var path = modelPath ?? GetModelPath();
            var modelDir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(modelDir) && !Directory.Exists(modelDir))
            {
                Directory.CreateDirectory(modelDir);
            }
            
            _mlContext.Model.Save(_trainedModel, _inputSchema, path);
            Console.WriteLine($"[ML] Model opgeslagen naar: {path}");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Fout bij opslaan van model: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Laadt een getraind model van disk.
    /// Zoekt eerst in de opgegeven directory, dan in wwwroot/data (deployment locatie), en ten slotte in persistente locatie.
    /// </summary>
    /// <param name="modelPath">Pad naar het opgeslagen model. Als null, gebruikt het de standaard locatie.</param>
    public bool LoadModel(string? modelPath = null)
    {
        if (!EnsureInitialized() || _mlContext == null)
        {
            return false;
        }

        try
        {
            // Als expliciet pad gegeven, probeer die eerst
            if (!string.IsNullOrEmpty(modelPath) && File.Exists(modelPath))
            {
                return LoadModelFromPath(modelPath);
            }

            // Zoek in meerdere locaties (voor Azure: eerst wwwroot/data, dan persistente locatie)
            var pathsToTry = new List<string>();
            
            // 1. Standaard locatie (zoals geconfigureerd)
            var defaultPath = GetModelPath();
            if (!string.IsNullOrEmpty(defaultPath))
            {
                pathsToTry.Add(defaultPath);
            }
            
            // 2. In Azure: probeer eerst wwwroot/data (waar het wordt gedeployed)
            var currentDir = Directory.GetCurrentDirectory();
            if (currentDir.Contains("site\\wwwroot", StringComparison.OrdinalIgnoreCase) || 
                currentDir.Contains("site/wwwroot", StringComparison.OrdinalIgnoreCase))
            {
                var wwwrootDataPath = Path.Combine(currentDir, "data", "recommendation_model.mlnet");
                if (!pathsToTry.Contains(wwwrootDataPath))
                {
                    pathsToTry.Insert(0, wwwrootDataPath); // Prioriteit: probeer eerst
                }
            }
            
            // 3. In Azure: probeer persistente locatie (C:\home\data of D:\home\data)
            if (currentDir.Contains("home", StringComparison.OrdinalIgnoreCase))
            {
                // Probeer C:\home\data
                var cHomeDataPath = Path.Combine(@"C:\home", "data", "recommendation_model.mlnet");
                if (Directory.Exists(Path.GetDirectoryName(cHomeDataPath)) && !pathsToTry.Contains(cHomeDataPath))
                {
                    pathsToTry.Add(cHomeDataPath);
                }
                
                // Probeer D:\home\data
                var dHomeDataPath = Path.Combine(@"D:\home", "data", "recommendation_model.mlnet");
                if (Directory.Exists(Path.GetDirectoryName(dHomeDataPath)) && !pathsToTry.Contains(dHomeDataPath))
                {
                    pathsToTry.Add(dHomeDataPath);
                }
            }

            // Probeer elk pad
            foreach (var path in pathsToTry)
            {
                if (File.Exists(path))
                {
                    Console.WriteLine($"[ML] Model gevonden op: {path}");
                    return LoadModelFromPath(path);
                }
                else
                {
                    Console.WriteLine($"[ML] Model niet gevonden op: {path}");
                }
            }

            Console.WriteLine($"[ML] Model bestand niet gevonden in alle geprobeerde locaties");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ML] Fout bij laden van model: {ex.Message}");
            _isModelTrained = false;
            return false;
        }
    }

    /// <summary>
    /// Laadt het model van een specifiek pad.
    /// </summary>
    private bool LoadModelFromPath(string path)
    {
        try
        {
            // Laad model van disk
            DataViewSchema schema;
            _trainedModel = _mlContext.Model.Load(path, out schema);
            
            // Maak prediction engine voor snelle voorspellingen
            _predictionEngine = _mlContext.Model.CreatePredictionEngine<CarFeatureData, ScorePrediction>(_trainedModel);
            
            _isModelTrained = true;
            
            // Probeer last modified tijd van bestand te lezen
            var fileInfo = new FileInfo(path);
            _lastTrainingTime = fileInfo.LastWriteTimeUtc;
            _trainingDataCount = 0; // We weten niet hoeveel training data gebruikt werd
            
            Console.WriteLine($"[ML] ✅ Model succesvol geladen van: {path}");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ML] ❌ Fout bij laden van model van {path}: {ex.Message}");
            _isModelTrained = false;
            return false;
        }
    }

    /// <summary>
    /// Bepaalt het pad waar het model wordt opgeslagen/geplaatst.
    /// </summary>
    private string GetModelPath()
    {
        if (!string.IsNullOrEmpty(_modelDirectory))
        {
            return Path.Combine(_modelDirectory, "recommendation_model.mlnet");
        }

        // Standaard locatie: data directory of current directory
        var dataDir = Path.Combine(Directory.GetCurrentDirectory(), "data");
        if (!Directory.Exists(dataDir))
        {
            dataDir = Directory.GetCurrentDirectory();
        }
        return Path.Combine(dataDir, "recommendation_model.mlnet");
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
            TrainingDataCount = _trainingDataCount,
            ModelPath = GetModelPath(),
            ModelExists = File.Exists(GetModelPath())
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
    public string? ModelPath { get; set; }
    public bool ModelExists { get; set; }
}

