namespace CarRecommender;

/// <summary>
/// ML evaluatie service - implementeert train/test split, evaluatie metrics en hyperparameter tuning.
/// 
/// ML Pipeline Stappen:
/// 1. DATA PREPROCESSING: Filter en valideer auto's met geldige waarden
/// 2. TRAIN/TEST SPLIT: Split dataset in training (80%) en test (20%) sets
/// 3. TRAINING: Train recommendation model op training set
/// 4. EVALUATIE: Evalueer model op test set met verschillende metrics
/// 5. HYPERPARAMETER TUNING: Optimaliseer similarity gewichten door meerdere configuraties te testen
/// 
/// Relevant voor ML & Forecasting vak:
/// - Demonstreert standaard ML workflow: preprocessing → train → evaluate → tune
/// - Gebruikt verschillende evaluatie metrics (Precision@K, Recall@K, MAE, RMSE)
/// - Hyperparameter tuning toont hoe model parameters geoptimaliseerd worden
/// </summary>
public class MlEvaluationService : IMlEvaluationService
{
    private readonly ICarRepository _carRepository;
    private readonly IRecommendationService _recommendationService;
    private readonly HyperparameterTuningService _hyperparameterTuningService;
    private readonly ForecastingService _forecastingService;
    
    // ML Pipeline configuratie
    private const double TRAIN_TEST_SPLIT_RATIO = 0.8; // 80% training, 20% test
    private const int PRECISION_RECALL_K = 10; // Top 10 recommendations voor Precision@K en Recall@K
    
    /// <summary>
    /// Constructor - initialiseert services via dependency injection.
    /// </summary>
    public MlEvaluationService(
        ICarRepository carRepository,
        IRecommendationService recommendationService,
        HyperparameterTuningService hyperparameterTuningService,
        ForecastingService forecastingService)
    {
        _carRepository = carRepository ?? throw new ArgumentNullException(nameof(carRepository));
        _recommendationService = recommendationService ?? throw new ArgumentNullException(nameof(recommendationService));
        _hyperparameterTuningService = hyperparameterTuningService ?? throw new ArgumentNullException(nameof(hyperparameterTuningService));
        _forecastingService = forecastingService ?? throw new ArgumentNullException(nameof(forecastingService));
    }
    
    /// <summary>
    /// Voert volledige ML evaluatie uit: train/test split, traint model, evalueert metrics en voert hyperparameter tuning uit.
    /// 
    /// ML Pipeline Stap 1-5: Complete evaluatie workflow
    /// </summary>
    public MlEvaluationResult EvaluateModel()
    {
        // ML Pipeline Stap 1: DATA PREPROCESSING
        // Filter auto's met geldige waarden voor ML training
        var allCars = _carRepository.GetAllCars();
        var validCars = allCars
            .Where(c => c.Power > 0 && c.Budget > 0 && c.Year >= 1900 && c.Year <= DateTime.Now.Year + 1)
            .ToList();
        
        if (validCars.Count < 20)
        {
            // Te weinig data voor train/test split
            return new MlEvaluationResult
            {
                IsValid = false,
                ErrorMessage = "Onvoldoende data voor ML evaluatie (minimaal 20 auto's vereist)"
            };
        }
        
        // ML Pipeline Stap 2: TRAIN/TEST SPLIT
        // Split dataset in training (80%) en test (20%) sets
        // Gebruik stratified split op basis van bouwjaar ranges voor betere representatie
        var splitResult = PerformTrainTestSplit(validCars);
        var trainingCars = splitResult.TrainingSet;
        var testCars = splitResult.TestSet;
        
        // ML Pipeline Stap 3: TRAINING
        // Voor dit project gebruiken we de bestaande recommendation service
        // In een echte ML pipeline zou hier een model getraind worden op de training set
        // We simuleren training door recommendations te genereren voor training auto's
        
        // ML Pipeline Stap 4: EVALUATIE
        // Evalueer recommendations op test set met verschillende metrics
        var evaluationMetrics = EvaluateRecommendations(trainingCars, testCars);
        
        // ML Pipeline Stap 5: HYPERPARAMETER TUNING
        // Optimaliseer similarity gewichten door meerdere configuraties te testen
        var hyperparameterResults = _hyperparameterTuningService.TuneHyperparameters(trainingCars, testCars);
        
        // Forecasting/Trend analyse
        var forecastingResults = _forecastingService.AnalyzeTrends(validCars);
        
        return new MlEvaluationResult
        {
            IsValid = true,
            TrainingSetSize = trainingCars.Count,
            TestSetSize = testCars.Count,
            PrecisionAtK = evaluationMetrics.PrecisionAtK,
            RecallAtK = evaluationMetrics.RecallAtK,
            MeanAbsoluteError = evaluationMetrics.MAE,
            RootMeanSquaredError = evaluationMetrics.RMSE,
            BestHyperparameters = hyperparameterResults.BestConfiguration,
            HyperparameterResults = hyperparameterResults.AllResults,
            ForecastingResults = forecastingResults,
            EvaluationTimestamp = DateTime.UtcNow
        };
    }
    
    /// <summary>
    /// ML Pipeline Stap 2: TRAIN/TEST SPLIT
    /// Split dataset in training en test sets met stratified sampling op basis van bouwjaar ranges.
    /// 
    /// Stratified sampling zorgt voor betere representatie van verschillende bouwjaren in beide sets.
    /// Dit is belangrijk voor ML omdat het voorkomt dat het model alleen leert op bepaalde jaren.
    /// </summary>
    private TrainTestSplitResult PerformTrainTestSplit(List<Car> cars)
    {
        // Stratified split: groep auto's per bouwjaar range
        var yearRanges = new[]
        {
            (MinYear: 1990, MaxYear: 2000),
            (MinYear: 2001, MaxYear: 2010),
            (MinYear: 2011, MaxYear: 2015),
            (MinYear: 2016, MaxYear: 2020),
            (MinYear: 2021, MaxYear: 2025)
        };
        
        var trainingSet = new List<Car>();
        var testSet = new List<Car>();
        
        var random = new Random(42); // Fixed seed voor reproduceerbaarheid
        
        // Split per jaar range voor stratified sampling
        foreach (var range in yearRanges)
        {
            var carsInRange = cars.Where(c => c.Year >= range.MinYear && c.Year <= range.MaxYear).ToList();
            
            // Shuffle voor randomisatie
            var shuffled = carsInRange.OrderBy(x => random.Next()).ToList();
            
            int splitIndex = (int)(shuffled.Count * TRAIN_TEST_SPLIT_RATIO);
            
            trainingSet.AddRange(shuffled.Take(splitIndex));
            testSet.AddRange(shuffled.Skip(splitIndex));
        }
        
        // Als er auto's zijn buiten de ranges, voeg ze toe aan training set
        var carsInRanges = trainingSet.Concat(testSet).Select(c => c.Id).ToHashSet();
        var remainingCars = cars.Where(c => !carsInRanges.Contains(c.Id)).ToList();
        trainingSet.AddRange(remainingCars.Take((int)(remainingCars.Count * TRAIN_TEST_SPLIT_RATIO)));
        testSet.AddRange(remainingCars.Skip((int)(remainingCars.Count * TRAIN_TEST_SPLIT_RATIO)));
        
        return new TrainTestSplitResult
        {
            TrainingSet = trainingSet,
            TestSet = testSet
        };
    }
    
    /// <summary>
    /// ML Pipeline Stap 4: EVALUATIE
    /// Evalueer recommendations op test set met Precision@K, Recall@K, MAE en RMSE metrics.
    /// 
    /// Evaluatie metrics zijn essentieel voor ML om te meten hoe goed een model presteert.
    /// - Precision@K: Hoeveel van de top K recommendations zijn relevant?
    /// - Recall@K: Hoeveel relevante items zitten in de top K?
    /// - MAE/RMSE: Hoe goed voorspelt het model prijzen?
    /// </summary>
    private EvaluationMetrics EvaluateRecommendations(List<Car> trainingCars, List<Car> testCars)
    {
        var allPrecisionScores = new List<double>();
        var allRecallScores = new List<double>();
        var pricePredictions = new List<(Car Predicted, Car Actual)>();
        
        // Evalueer voor elke test auto
        int evaluationCount = Math.Min(50, testCars.Count); // Beperk voor performance
        var testSample = testCars.Take(evaluationCount).ToList();
        
        foreach (var testCar in testSample)
        {
            // Genereer recommendations voor deze test auto
            var recommendations = _recommendationService.RecommendSimilarCars(testCar, PRECISION_RECALL_K);
            
            // Bepaal relevante auto's (auto's met vergelijkbare prijs ± 20%)
            // In een echte ML setup zou dit gebaseerd zijn op echte user feedback/interacties
            decimal priceThreshold = testCar.Budget * 0.20m;
            var relevantCars = trainingCars
                .Where(c => Math.Abs(c.Budget - testCar.Budget) <= priceThreshold && c.Id != testCar.Id)
                .ToList();
            
            if (relevantCars.Count > 0)
            {
                // Berekent Precision@K en Recall@K
                double precision = CalculatePrecisionAtK(recommendations, relevantCars, PRECISION_RECALL_K);
                double recall = CalculateRecallAtK(recommendations, relevantCars, PRECISION_RECALL_K);
                
                allPrecisionScores.Add(precision);
                allRecallScores.Add(recall);
            }
            
            // Voor prijsvoorspelling: gebruik gemiddelde prijs van top recommendations als voorspelling
            if (recommendations.Count > 0)
            {
                var predictedPrice = recommendations.Select(r => r.Car.Budget).Average();
                var predictedCar = new Car { Budget = (decimal)predictedPrice };
                pricePredictions.Add((predictedCar, testCar));
            }
        }
        
        // Bereken gemiddelde metrics
        double avgPrecision = allPrecisionScores.Count > 0 ? allPrecisionScores.Average() : 0.0;
        double avgRecall = allRecallScores.Count > 0 ? allRecallScores.Average() : 0.0;
        
        // Bereken MAE en RMSE voor prijsvoorspellingen
        double mae = 0.0;
        double rmse = 0.0;
        
        if (pricePredictions.Count > 0)
        {
            var predictedCars = pricePredictions.Select(p => p.Predicted).ToList();
            var actualCars = pricePredictions.Select(p => p.Actual).ToList();
            
            mae = CalculateMeanAbsoluteError(predictedCars, actualCars);
            rmse = CalculateRootMeanSquaredError(predictedCars, actualCars);
        }
        
        return new EvaluationMetrics
        {
            PrecisionAtK = avgPrecision,
            RecallAtK = avgRecall,
            MAE = mae,
            RMSE = rmse
        };
    }
    
    /// <summary>
    /// Berekent Precision@K metric voor recommendations.
    /// Precision@K = aantal relevante items in top K / K
    /// 
    /// Precision@K is een belangrijke metric voor recommendation systems.
    /// Het meet de accuraatheid van de top K recommendations (hoeveel zijn relevant?).
    /// Hoger is beter (1.0 = alle top K zijn relevant).
    /// </summary>
    public double CalculatePrecisionAtK(List<RecommendationResult> recommendations, List<Car> relevantCars, int k)
    {
        if (recommendations.Count == 0 || relevantCars.Count == 0)
            return 0.0;
        
        var relevantCarIds = relevantCars.Select(c => c.Id).ToHashSet();
        var topK = recommendations.Take(k).ToList();
        
        int relevantCount = topK.Count(r => relevantCarIds.Contains(r.Car.Id));
        
        return (double)relevantCount / k;
    }
    
    /// <summary>
    /// Berekent Recall@K metric voor recommendations.
    /// Recall@K = aantal relevante items in top K / totaal aantal relevante items
    /// 
    /// Recall@K meet de volledigheid van de recommendations (hoeveel relevante items worden gevonden?).
    /// Hoger is beter (1.0 = alle relevante items zitten in top K).
    /// </summary>
    public double CalculateRecallAtK(List<RecommendationResult> recommendations, List<Car> relevantCars, int k)
    {
        if (recommendations.Count == 0 || relevantCars.Count == 0)
            return 0.0;
        
        var relevantCarIds = relevantCars.Select(c => c.Id).ToHashSet();
        var topK = recommendations.Take(k).ToList();
        
        int relevantInTopK = topK.Count(r => relevantCarIds.Contains(r.Car.Id));
        
        return (double)relevantInTopK / relevantCars.Count;
    }
    
    /// <summary>
    /// Berekent Mean Absolute Error (MAE) voor prijsvoorspellingen.
    /// MAE = gemiddelde absolute fout tussen voorspelde en werkelijke prijzen
    /// 
    /// MAE is een veelgebruikte metric voor regressie problemen (zoals prijsvoorspelling).
    /// Het meet de gemiddelde absolute afwijking tussen voorspellingen en werkelijke waarden.
    /// Lager is beter (0.0 = perfecte voorspellingen).
    /// </summary>
    public double CalculateMeanAbsoluteError(List<Car> predicted, List<Car> actual)
    {
        if (predicted.Count != actual.Count || predicted.Count == 0)
            return 0.0;
        
        double totalError = 0.0;
        for (int i = 0; i < predicted.Count; i++)
        {
            totalError += Math.Abs((double)(predicted[i].Budget - actual[i].Budget));
        }
        
        return totalError / predicted.Count;
    }
    
    /// <summary>
    /// Berekent Root Mean Squared Error (RMSE) voor prijsvoorspellingen.
    /// RMSE = vierkantswortel van gemiddelde kwadratische fout
    /// 
    /// RMSE is gevoeliger voor grote fouten dan MAE (grote fouten worden zwaarder bestraft).
    /// Dit is nuttig wanneer grote fouten problematischer zijn dan kleine fouten.
    /// Lager is beter (0.0 = perfecte voorspellingen).
    /// </summary>
    public double CalculateRootMeanSquaredError(List<Car> predicted, List<Car> actual)
    {
        if (predicted.Count != actual.Count || predicted.Count == 0)
            return 0.0;
        
        double totalSquaredError = 0.0;
        for (int i = 0; i < predicted.Count; i++)
        {
            double error = (double)(predicted[i].Budget - actual[i].Budget);
            totalSquaredError += error * error;
        }
        
        double meanSquaredError = totalSquaredError / predicted.Count;
        return Math.Sqrt(meanSquaredError);
    }
    
    /// <summary>
    /// Helper class voor train/test split resultaat.
    /// </summary>
    private class TrainTestSplitResult
    {
        public List<Car> TrainingSet { get; set; } = new();
        public List<Car> TestSet { get; set; } = new();
    }
    
    /// <summary>
    /// Helper class voor evaluatie metrics.
    /// </summary>
    private class EvaluationMetrics
    {
        public double PrecisionAtK { get; set; }
        public double RecallAtK { get; set; }
        public double MAE { get; set; }
        public double RMSE { get; set; }
    }
}

/// <summary>
/// Resultaat model voor ML evaluatie.
/// Bevat alle metrics, hyperparameter resultaten en forecasting data.
/// </summary>
public class MlEvaluationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public int TrainingSetSize { get; set; }
    public int TestSetSize { get; set; }
    public double PrecisionAtK { get; set; }
    public double RecallAtK { get; set; }
    public double MeanAbsoluteError { get; set; }
    public double RootMeanSquaredError { get; set; }
    public HyperparameterConfiguration? BestHyperparameters { get; set; }
    public List<HyperparameterResult> HyperparameterResults { get; set; } = new();
    public ForecastingResult? ForecastingResults { get; set; }
    public DateTime EvaluationTimestamp { get; set; }
}

