namespace CarRecommender;

/// <summary>
/// Hyperparameter tuning service - optimaliseert similarity gewichten door meerdere configuraties te testen.
/// 
/// ML Pipeline Stap 5: HYPERPARAMETER TUNING
/// 
/// Hyperparameter tuning is een belangrijk onderdeel van ML model optimalisatie.
/// Door verschillende combinaties van parameters (in dit geval similarity gewichten) te testen,
/// kunnen we de beste configuratie vinden die de beste resultaten geeft op de test set.
/// 
/// Relevant voor ML & Forecasting vak:
/// - Demonstreert hoe hyperparameters geoptimaliseerd worden
/// - Toont grid search aanpak (testen van meerdere combinaties)
/// - Evalueert elke configuratie op basis van evaluatie metrics
/// </summary>
public class HyperparameterTuningService
{
    private readonly ICarRepository _carRepository;
    private readonly RecommendationEngine _recommendationEngine;
    
    /// <summary>
    /// Constructor - initialiseert services.
    /// </summary>
    public HyperparameterTuningService(ICarRepository carRepository)
    {
        _carRepository = carRepository ?? throw new ArgumentNullException(nameof(carRepository));
        _recommendationEngine = new RecommendationEngine();
    }
    
    /// <summary>
    /// ML Pipeline Stap 5: HYPERPARAMETER TUNING
    /// Optimaliseert similarity gewichten door meerdere configuraties te testen op train/test split.
    /// 
    /// Grid Search aanpak: Test verschillende combinaties van gewichten:
    /// - Power weight: 0.15 - 0.35 (in stappen van 0.05)
    /// - Budget weight: 0.25 - 0.40 (in stappen van 0.05)
    /// - Year weight: 0.15 - 0.30 (in stappen van 0.05)
    /// - Fuel weight: 0.20 - 0.35 (in stappen van 0.05)
    /// 
    /// Elke configuratie wordt geëvalueerd op basis van gemiddelde similarity score accuracy.
    /// </summary>
    public HyperparameterTuningResult TuneHyperparameters(List<Car> trainingSet, List<Car> testSet)
    {
        var results = new List<HyperparameterResult>();
        
        // Bereken min/max waarden voor normalisatie (van alle auto's)
        var allCars = _carRepository.GetAllCars();
        var validCars = allCars.Where(c => c.Power > 0 && c.Budget > 0 && c.Year > 1900).ToList();
        
        if (validCars.Count == 0)
        {
            return new HyperparameterTuningResult
            {
                BestConfiguration = null,
                AllResults = results
            };
        }
        
        int minPower = validCars.Min(c => c.Power);
        int maxPower = validCars.Max(c => c.Power);
        decimal minBudget = validCars.Min(c => c.Budget);
        decimal maxBudget = validCars.Max(c => c.Budget);
        int minYear = validCars.Min(c => c.Year);
        int maxYear = validCars.Max(c => c.Year);
        
        // Grid Search: Test verschillende combinaties van gewichten
        // Om de zoektijd te beperken, testen we een subset van combinaties
        var powerWeights = new[] { 0.15, 0.20, 0.25, 0.30, 0.35 };
        var budgetWeights = new[] { 0.25, 0.30, 0.35, 0.40 };
        var yearWeights = new[] { 0.15, 0.20, 0.25, 0.30 };
        var fuelWeights = new[] { 0.20, 0.25, 0.30, 0.35 };
        
        // Test subset van combinaties (volledige grid search zou te lang duren)
        // Test elke 2e waarde voor efficiency
        int testCount = 0;
        const int MAX_TESTS = 20; // Beperk aantal tests voor performance
        
        foreach (var powerWeight in powerWeights.Where((_, i) => i % 2 == 0))
        {
            foreach (var budgetWeight in budgetWeights.Where((_, i) => i % 2 == 0))
            {
                foreach (var yearWeight in yearWeights.Where((_, i) => i % 2 == 0))
                {
                    // Bereken fuel weight zodat som = 1.0
                    double fuelWeight = 1.0 - powerWeight - budgetWeight - yearWeight;
                    
                    if (fuelWeight < 0.15 || fuelWeight > 0.40)
                        continue; // Skip ongeldige combinaties
                    
                    if (testCount >= MAX_TESTS)
                        break;
                    
                    // Test deze configuratie
                    var config = new HyperparameterConfiguration
                    {
                        PowerWeight = powerWeight,
                        BudgetWeight = budgetWeight,
                        YearWeight = yearWeight,
                        FuelWeight = fuelWeight
                    };
                    
                    double score = EvaluateConfiguration(config, trainingSet, testSet, minPower, maxPower, minBudget, maxBudget, minYear, maxYear);
                    
                    results.Add(new HyperparameterResult
                    {
                        Configuration = config,
                        Score = score
                    });
                    
                    testCount++;
                }
                
                if (testCount >= MAX_TESTS)
                    break;
            }
            
            if (testCount >= MAX_TESTS)
                break;
        }
        
        // Sorteer op score (hoger is beter) en pak beste configuratie
        results = results.OrderByDescending(r => r.Score).ToList();
        
        return new HyperparameterTuningResult
        {
            BestConfiguration = results.Count > 0 ? results[0].Configuration : null,
            AllResults = results
        };
    }
    
    /// <summary>
    /// Evalueert een hyperparameter configuratie op basis van gemiddelde similarity score accuracy.
    /// 
    /// Voor elke test auto wordt de similarity berekend met de gegeven gewichten.
    /// De score is het gemiddelde van de similarity scores voor alle test auto's.
    /// </summary>
    private double EvaluateConfiguration(
        HyperparameterConfiguration config,
        List<Car> trainingSet,
        List<Car> testSet,
        int minPower, int maxPower,
        decimal minBudget, decimal maxBudget,
        int minYear, int maxYear)
    {
        double totalScore = 0.0;
        int testCount = 0;
        
        // Test op subset van test set voor performance
        var testSample = testSet.Take(Math.Min(20, testSet.Count)).ToList();
        
        foreach (var testCar in testSample)
        {
            // Vind beste match in training set met deze configuratie
            double bestSimilarity = 0.0;
            
            foreach (var trainingCar in trainingSet)
            {
                if (trainingCar.Id == testCar.Id)
                    continue;
                
                double similarity = _recommendationEngine.CalculateSimilarity(
                    testCar,
                    trainingCar,
                    minPower, maxPower,
                    minBudget, maxBudget,
                    minYear, maxYear,
                    config.PowerWeight,
                    config.BudgetWeight,
                    config.YearWeight,
                    config.FuelWeight);
                
                if (similarity > bestSimilarity)
                {
                    bestSimilarity = similarity;
                }
            }
            
            totalScore += bestSimilarity;
            testCount++;
        }
        
        return testCount > 0 ? totalScore / testCount : 0.0;
    }
}

/// <summary>
/// Hyperparameter configuratie - bevat gewichten voor similarity berekening.
/// </summary>
public class HyperparameterConfiguration
{
    public double PowerWeight { get; set; }
    public double BudgetWeight { get; set; }
    public double YearWeight { get; set; }
    public double FuelWeight { get; set; }
    
    public double TotalWeight => PowerWeight + BudgetWeight + YearWeight + FuelWeight;
}

/// <summary>
/// Resultaat van hyperparameter tuning - bevat configuratie en evaluatie score.
/// </summary>
public class HyperparameterResult
{
    public HyperparameterConfiguration Configuration { get; set; } = null!;
    public double Score { get; set; }
}

/// <summary>
/// Resultaat van volledige hyperparameter tuning - bevat beste configuratie en alle test resultaten.
/// </summary>
public class HyperparameterTuningResult
{
    public HyperparameterConfiguration? BestConfiguration { get; set; }
    public List<HyperparameterResult> AllResults { get; set; } = new();
}







