namespace CarRecommender;

/// <summary>
/// Implementatie van IRecommendationService - coördineert het recommendation proces.
/// Gebruikt nieuwe AI structuur: RuleBasedFilter (Old AI) + Content-based recommender (New AI).
/// 
/// Business logica laag:
/// - Deze laag bevat alle recommendation algoritmes en business regels
/// - Gebruikt dependency injection om ICarRepository te krijgen (data laag)
/// - Onafhankelijk van hoe data wordt opgeslagen (CSV, SQL, etc.)
/// 
/// AI Architectuur (MEERDERE OPTIES):
/// - Old AI: RuleBasedFilter bepaalt candidate set (harde filters)
/// - New AI Optie 1: CarFeatureVector + SimilarityService (Cosine similarity)
/// - New AI Optie 2: KNN (K-Nearest Neighbours uit Les 4 - Euclidische afstand)
/// - Ranking: RankingService voor finale volgorde
/// - ML.NET: Optioneel voor popularity prediction
/// 
/// Voor Azure deployment:
/// - Deze service blijft hetzelfde werken
/// - Alleen de ICarRepository implementatie wordt gewisseld (CSV → SQL)
/// </summary>
public class RecommendationService : IRecommendationService
{
    private readonly ICarRepository _carRepository;
    private readonly RecommendationEngine _engine; // Used by RecommendSimilarCars (legacy endpoint)
    private readonly TextParserService _textParser;
    private readonly ExplanationBuilder _explanationBuilder;
    
    // Core AI components
     private readonly RuleBasedFilter _ruleBasedFilter;
    private readonly CarFeatureVectorFactory _featureVectorFactory;
    private readonly SimilarityService _similarityService;
    private readonly KnnRecommendationService _knnService;  // ← NIEUW: KNN uit Les 4
    private readonly RankingService _rankingService;
    private readonly MlRecommendationService _mlService;
    private readonly AdvancedScoringService _advancedScoringService;
    
    private bool _isInitialized = false;
    private bool _isModelTrained = false;

    /// <summary>
    /// Constructor - initialiseert alle services met dependency injection.
    /// </summary>
    public RecommendationService(
        ICarRepository carRepository,
        RuleBasedFilter ruleBasedFilter,
        CarFeatureVectorFactory featureVectorFactory,
        SimilarityService similarityService,
        KnnRecommendationService knnService,
        RankingService rankingService,
        MlRecommendationService mlService,
        AdvancedScoringService advancedScoringService)
    {
        _carRepository = carRepository ?? throw new ArgumentNullException(nameof(carRepository));

        // Legacy / stateless helpers (mogen met new)
        _engine = new RecommendationEngine();
        _textParser = new TextParserService();
        _explanationBuilder = new ExplanationBuilder();

        // Core AI components via DI (GEEN new)
        _ruleBasedFilter = ruleBasedFilter;
        _featureVectorFactory = featureVectorFactory;
        _similarityService = similarityService;
        _knnService = knnService;
        _rankingService = rankingService;
        _mlService = mlService;
        _advancedScoringService = advancedScoringService;
    }


    /// <summary>
    /// Initialiseert de feature vector factory met alle auto's.
    /// Traint ook het ML model als dat nog niet gebeurd is.
    /// Moet worden aangeroepen voordat RecommendFromText wordt gebruikt.
    /// </summary>
    private void EnsureInitialized()
    {
        if (_isInitialized)
            return;

        Console.WriteLine("[EnsureInitialized] Start initialisatie...");
        List<Car> allCars = _carRepository.GetAllCars();
        Console.WriteLine($"[EnsureInitialized] Auto's geladen: {allCars.Count}");
        
        _featureVectorFactory.Initialize(allCars);
        _knnService.Initialize(allCars);  //Initialiseer KNN met alle auto's
        Console.WriteLine("[EnsureInitialized] FeatureVectorFactory + KNN geïnitialiseerd");
        
        // ML model training wordt uitgevoerd in achtergrond via MlModelTrainingBackgroundService
        // Dit voorkomt dat training de opstarttijd blokkeert
        // Het model wordt actief zodra training voltooid is
        
        _isInitialized = true;
        Console.WriteLine("[EnsureInitialized] Initialisatie voltooid");
    }

    /// <summary>
    /// Traint het ML model op basis van bestaande recommendation patterns.
    /// Gebruikt een sample van auto's en genereert initial recommendations om training data te maken.
    /// </summary>
    private void TrainMlModel(List<Car> allCars)
    {
        try
        {
            // Genereer training data door recommendations te maken voor een sample van auto's
            // Dit simuleert historische recommendation data
            var trainingResults = new List<RecommendationResult>();
            var sampleSize = Math.Min(50, allCars.Count); // Gebruik max 50 auto's voor training
            
            for (int i = 0; i < sampleSize; i++)
            {
                var targetCar = allCars[i];
                
                // Skip auto's zonder geldige data
                if (targetCar.Power <= 0 || targetCar.Budget <= 0 || targetCar.Year < 1900)
                    continue;

                // Genereer recommendations voor deze auto
                var recommendations = RecommendSimilarCars(targetCar, 10);
                trainingResults.AddRange(recommendations);
            }

            // Train het ML model met de gegenereerde recommendations
            if (trainingResults.Count > 0)
            {
                _mlService.TrainModel(allCars, trainingResults);
                _isModelTrained = _mlService.IsModelTrained;
            }
        }
        catch (Exception ex)
        {
            // Log error maar blijf werken zonder ML
            Console.WriteLine($"ML model training failed during initialization: {ex.Message}");
            _isModelTrained = false;
        }
    }

    /// <summary>
    /// Vindt de meest vergelijkbare auto's voor een target auto.
    /// Sorteert op similarity en geeft top N terug.
    /// </summary>
    public List<RecommendationResult> RecommendSimilarCars(Car target, int n)
    {
        List<Car> allCars = _carRepository.GetAllCars();
        List<RecommendationResult> results = new List<RecommendationResult>();

        // Null check
        if (allCars == null || target == null)
            return results;

        // Bereken min/max waarden voor normalisatie (alleen voor auto's met geldige waarden)
        // Dit is nodig voor de similarity berekening
        var validCars = allCars.Where(c => c.Power > 0 && c.Budget > 0 && c.Year > 1900).ToList();
        
        if (validCars.Count == 0)
        {
            return results; // Geen geldige data
        }

        // Vind min/max waarden in dataset
        int minPower = validCars.Min(c => c.Power);
        int maxPower = validCars.Max(c => c.Power);
        decimal minBudget = validCars.Min(c => c.Budget);
        decimal maxBudget = validCars.Max(c => c.Budget);
        int minYear = validCars.Min(c => c.Year);
        int maxYear = validCars.Max(c => c.Year);

        // Bereken similarity voor elke auto (behalve de target zelf)
        foreach (Car car in allCars)
        {
            // Skip target auto (geen recommendation van zichzelf)
            if (car.Id == target.Id || 
                (car.Brand == target.Brand && car.Model == target.Model && car.Year == target.Year))
            {
                continue;
            }

            // Bereken similarity
            double similarity = _engine.CalculateSimilarity(
                target, 
                car, 
                minPower, maxPower, 
                minBudget, maxBudget, 
                minYear, maxYear);

            // Toevoegen aan resultaten
            results.Add(new RecommendationResult
            {
                Car = car,
                SimilarityScore = similarity
            });
        }
        // Sorteer op similarity, verwijder dubbele modellen (behoud hoogste similarity per merk+model), en pak top N
        var finalResults = results
            .OrderByDescending(r => r.SimilarityScore)
            .GroupBy(r => new { r.Car.Brand, r.Car.Model }) // Groepeer alleen op merk + model (zonder brandstof voor meer diversiteit)
            .Select(g => g.OrderByDescending(r => r.SimilarityScore).First()) // Behoud de auto met de hoogste similarity per groep
            .Take(n)
            .ToList();
        return finalResults;
    }

    /// <summary>
    /// Efficiënte versie van RecommendSimilarCars die alleen door een specifieke candidate set itereert.
    /// Gebruikt voor ML evaluatie om alleen de training set te gebruiken in plaats van alle auto's.
    /// Public zodat MlEvaluationService deze kan gebruiken.
    /// </summary>
    public List<RecommendationResult> RecommendSimilarCarsFromSet(Car target, List<Car> candidateCars, int n)
    {
        if (candidateCars == null || candidateCars.Count == 0 || target == null)
            return new List<RecommendationResult>();

        // Bereken min/max waarden voor normalisatie (alleen voor candidate auto's met geldige waarden)
        var validCars = candidateCars.Where(c => c.Power > 0 && c.Budget > 0 && c.Year > 1900).ToList();
        
        if (validCars.Count == 0)
            return new List<RecommendationResult>();

        // Vind min/max waarden in candidate set
        int minPower = validCars.Min(c => c.Power);
        int maxPower = validCars.Max(c => c.Power);
        decimal minBudget = validCars.Min(c => c.Budget);
        decimal maxBudget = validCars.Max(c => c.Budget);
        int minYear = validCars.Min(c => c.Year);
        int maxYear = validCars.Max(c => c.Year);

        // Bereken similarity alleen voor candidate auto's (veel sneller!)
        var results = new List<RecommendationResult>();
        
        foreach (Car car in candidateCars)
        {
            // Skip target auto
            if (car.Id == target.Id || 
                (car.Brand == target.Brand && car.Model == target.Model && car.Year == target.Year))
            {
                continue;
            }

            // Bereken similarity
            double similarity = _engine.CalculateSimilarity(
                target, 
                car, 
                minPower, maxPower, 
                minBudget, maxBudget, 
                minYear, maxYear);

            results.Add(new RecommendationResult
            {
                Car = car,
                SimilarityScore = similarity
            });
        }

        // Sorteer en pak top N
        return results
            .OrderByDescending(r => r.SimilarityScore)
            .GroupBy(r => new { r.Car.Brand, r.Car.Model })
            .Select(g => g.OrderByDescending(r => r.SimilarityScore).First())
            .Take(n)
            .ToList();
    }

    /// <summary>
    /// Genereert recommendations op basis van tekst input van gebruiker.
    /// Gebruikt nieuwe AI structuur: Old AI (rule-based filters) + New AI (content-based similarity).
    /// </summary>
    public List<RecommendationResult> RecommendFromText(string inputText, int n = 5)
    {
        // Synchronous wrapper voor backward compatibility
        return RecommendFromTextAsync(inputText, n).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Async versie voor collaborative filtering support.
    /// </summary>
    public async Task<List<RecommendationResult>> RecommendFromTextAsync(string inputText, int n = 5)
    {
        EnsureInitialized();
        
        // Null check
        if (string.IsNullOrWhiteSpace(inputText))
            return new List<RecommendationResult>();
        
        Console.WriteLine($"[RecommendFromTextAsync] Start - Input: {inputText.Substring(0, Math.Min(50, inputText.Length))}");
        
        List<Car> allCars = _carRepository.GetAllCars();
        Console.WriteLine($"[RecommendFromTextAsync] Totaal aantal auto's: {allCars.Count}");
        
        // Parse tekst naar preferences
        UserPreferences prefs = _textParser.ParsePreferencesFromText(inputText);
        Console.WriteLine($"[RecommendFromTextAsync] Preferences geparsed");

        // OLD AI: Rule-based filtering - bepaal candidate set met harde filters
        var filterCriteria = _ruleBasedFilter.ConvertPreferencesToCriteria(prefs);
        List<Car> candidateCars = _ruleBasedFilter.FilterCars(allCars, filterCriteria);
        Console.WriteLine($"[RecommendFromTextAsync] Candidate cars na filtering: {candidateCars.Count}");

        // Als geen auto's matchten filters, gebruik alle auto's (maar met lagere scores)
        if (candidateCars.Count == 0)
        {
            candidateCars = allCars;
            Console.WriteLine($"[RecommendFromTextAsync] Geen matches, gebruik alle auto's: {candidateCars.Count}");
        }
        
        // Limiteer candidate cars om performance te verbeteren (max 200 auto's voor snellere response)
        if (candidateCars.Count > 200)
        {
            Console.WriteLine($"[RecommendFromTextAsync] ⚠️ Te veel candidate cars ({candidateCars.Count}), beperk tot 200 voor performance");
            candidateCars = candidateCars.Take(200).ToList();
        }

        // NEW AI: Content-based similarity + Advanced scoring
        // Maak ideale feature vector op basis van preferences
        CarFeatureVector idealVector = _featureVectorFactory.CreateIdealVector(prefs, candidateCars);

        // Bereken similarity en ranking scores voor alle candidate auto's met geavanceerde scoring
        List<RecommendationResult> results = new List<RecommendationResult>();

        int carIndex = 0;
        int processedCount = 0;
        Console.WriteLine($"[RecommendFromTextAsync] Start verwerken van {candidateCars.Count} candidate cars...");
        
        foreach (Car car in candidateCars)
        {
            // Skip auto's zonder geldige data
            if (car.Power <= 0 || car.Budget <= 0 || car.Year < 1900)
                continue;

            // Progress logging elke 50 auto's (vaker voor betere feedback)
            if (carIndex % 50 == 0 && carIndex > 0)
            {
                Console.WriteLine($"[RecommendFromTextAsync] Verwerkt {carIndex}/{candidateCars.Count} auto's ({carIndex * 100 / candidateCars.Count}%)...");
            }

            // Gebruik AdvancedScoringService voor slimmere scores met transparantie
            // OPTIMALISATIE: Gebruik alleen candidateCars voor normalisatie in plaats van allCars (veel sneller!)
            var featureScores = _advancedScoringService.CalculateScores(car, prefs, idealVector, candidateCars);

            // Gebruik finale score uit AdvancedScoringService
            double finalScore = featureScores.FinalScore;

            // Voeg ML-component toe (ML.NET-gebaseerde populairiteit/user rating)
            double userRatingComponent = _advancedScoringService.GetUserRatingComponent(car.Id);
            
            // Combineer deterministische score met ML-gebaseerde component
            // 90% deterministische score, 10% ML-gebaseerde populairiteit
            if (userRatingComponent > 0.0)
            {
                finalScore = (finalScore * 0.9) + (userRatingComponent * 0.1);
            }

            // Voeg collaborative filtering score toe (als beschikbaar) - UITGESCHAKELD VOOR PERFORMANCE
            // Collaborative filtering kan erg lang duren, daarom tijdelijk uitgeschakeld
            CollaborativeScore? collaborativeScore = null;
            // TEMPORARILY DISABLED FOR PERFORMANCE
            // if (_collaborativeService != null) { ... }
            carIndex++;
            processedCount++;

            string explanation = _explanationBuilder.BuildExplanation(car, prefs, finalScore, collaborativeScore);

            results.Add(new RecommendationResult
            {
                Car = car,
                SimilarityScore = finalScore, // Gebruik finale score uit AdvancedScoringService
                Explanation = explanation,
                FeatureScores = featureScores // Voeg feature-scores toe voor transparantie
            });
        }

        Console.WriteLine($"[RecommendFromTextAsync] Verwerkt {processedCount} auto's, start ranking...");
        
        // Sorteer op score met controlled randomness
        var rankedResults = _rankingService.RankWithControlledRandomness(results);
        
        Console.WriteLine($"[RecommendFromTextAsync] Ranking voltooid, selecteer top {n}...");
        
        // Verwijder dubbele modellen (behoud hoogste score per merk+model) en pak top N
        var finalResults = rankedResults
            .GroupBy(r => new { r.Car.Brand, r.Car.Model })
            .Select(g => g.OrderByDescending(r => r.SimilarityScore).First())
            .Take(n)
            .ToList();
        
        Console.WriteLine($"[RecommendFromTextAsync] Klaar! {finalResults.Count} recommendations gegenereerd.");

        return finalResults;
    }

    /// <summary>
    /// KNN-GEBASEERDE RECOMMENDATIONS (LES 4: Spotify Recommendations)
    /// </summary>
    public List<RecommendationResult> RecommendFromTextWithKnn(string inputText, int k = 5)
    {
        EnsureInitialized();
        
        // Null check
        if (string.IsNullOrWhiteSpace(inputText))
            return new List<RecommendationResult>();
        
        Console.WriteLine($"[KNN] Start - Input: {inputText.Substring(0, Math.Min(50, inputText.Length))}");
        
        List<Car> allCars = _carRepository.GetAllCars();
        Console.WriteLine($"[KNN] Totaal aantal auto's: {allCars.Count}");
        
        // Parse tekst naar preferences
        UserPreferences prefs = _textParser.ParsePreferencesFromText(inputText);
        Console.WriteLine($"[KNN] Preferences geparsed: Budget={prefs.MaxBudget}, Fuel={prefs.PreferredFuel}");

        // OLD AI: Rule-based filtering - bepaal candidate set met harde filters
        var filterCriteria = _ruleBasedFilter.ConvertPreferencesToCriteria(prefs);
        List<Car> candidateCars = _ruleBasedFilter.FilterCars(allCars, filterCriteria);
        Console.WriteLine($"[KNN] Candidate cars na filtering: {candidateCars.Count}");

        // Als geen auto's matchten filters, gebruik alle auto's
        if (candidateCars.Count == 0)
        {
            candidateCars = allCars;
            Console.WriteLine($"[KNN] Geen matches, gebruik alle auto's: {candidateCars.Count}");
        }

        // NEW AI: KNN - vind K nearest neighbors via Euclidische afstand
        Console.WriteLine($"[KNN] Start KNN algoritme (K={k})...");
        List<RecommendationResult> knnResults = _knnService.FindNearestNeighborsFromPreferences(
            prefs, 
            candidateCars);
        
        Console.WriteLine($"[KNN] KNN resultaten: {knnResults.Count}");

        // Voeg explanations toe
        foreach (var result in knnResults)
        {
            result.Explanation = _explanationBuilder.BuildExplanation(
                result.Car, 
                prefs, 
                result.SimilarityScore, 
                null);  // Geen collaborative score
        }

        // Deduplicatie: verwijder dubbele merk+model combinaties
        var finalResults = knnResults
            .GroupBy(r => new { r.Car.Brand, r.Car.Model })
            .Select(g => g.OrderByDescending(r => r.SimilarityScore).First())
            .Take(k)
            .ToList();
        
        Console.WriteLine($"[KNN] Klaar! {finalResults.Count} KNN-based recommendations gegenereerd.");

        return finalResults;
    }

    /// <summary>
    /// Filter auto's op basis van user preferences.
    /// Voegt ook realistische filters toe om onrealistische waarden uit te sluiten.
    /// </summary>
    private List<Car> FilterCarsByPreferences(List<Car> cars, UserPreferences prefs)
    {
        var filtered = cars.AsEnumerable();

        // Realistische grenzen (extra veiligheid, hoewel dit al gefilterd zou moeten zijn bij inladen)
        // Gelijk aan IsCarRealistic in CarRepository
        const decimal MIN_REALISTIC_PRICE = 300;
        const decimal MAX_REALISTIC_PRICE = 500000;
        const int MIN_REALISTIC_POWER = 20;
        const int MAX_REALISTIC_POWER = 800;

        // Realistische basis filters (altijd toepassen)
        filtered = filtered.Where(c => 
            c.Budget >= MIN_REALISTIC_PRICE && c.Budget <= MAX_REALISTIC_PRICE &&
            c.Power >= MIN_REALISTIC_POWER && c.Power <= MAX_REALISTIC_POWER);

        // Budget filter (user preference)
        if (prefs.MaxBudget.HasValue)
        {
            filtered = filtered.Where(c => c.Budget <= (decimal)prefs.MaxBudget.Value);
        }

        // Brandstof filter
        if (!string.IsNullOrWhiteSpace(prefs.PreferredFuel))
        {
            string fuelLower = prefs.PreferredFuel.ToLower();
            filtered = filtered.Where(c => c.Fuel.ToLower().Contains(fuelLower));
        }

        // Vermogen filter - alleen als MinPower een exacte KW waarde is (> 100), niet voor scores (0-1)
        if (prefs.MinPower.HasValue && prefs.MinPower.Value > 100)
        {
            // Exacte KW waarde (bijv. "minstens 200 KW")
            filtered = filtered.Where(c => c.Power >= (int)prefs.MinPower.Value);
        }
        else if (prefs.MinPower.HasValue && prefs.MinPower.Value <= 1.0)
        {
            // Score (0-1) - converteer naar minimum vermogen op basis van score
            // Bijv. score 0.8 = 80% van het vermogen bereik
            var validCars = filtered.ToList();
            if (validCars.Any())
            {
                int minPowerInDataset = validCars.Min(c => c.Power);
                int maxPowerInDataset = validCars.Max(c => c.Power);
                int minPowerRequired = (int)(minPowerInDataset + (prefs.MinPower.Value * (maxPowerInDataset - minPowerInDataset)));
                filtered = filtered.Where(c => c.Power >= minPowerRequired);
            }
        }

        // Transmissie en body type filters worden later toegevoegd als we die kolommen hebben

        return filtered.ToList();
    }

    /// <summary>
    /// Berekent gewichten voor similarity op basis van user preferences.
    /// Gebruikt PreferenceWeights dictionary als beschikbaar, anders fallback op comfort/sport score.
    /// </summary>
    private (double WeightPower, double WeightBudget, double WeightYear, double WeightFuel) CalculateWeightsFromPreferences(UserPreferences prefs)
    {
        // Haal gewichten uit preferences (genormaliseerd naar 0-1 voor similarity engine)
        double weightBudget = NormalizePreferenceWeight(prefs.PreferenceWeights.GetValueOrDefault("budget", 1.0));
        double weightFuel = NormalizePreferenceWeight(prefs.PreferenceWeights.GetValueOrDefault("fuel", 1.0));
        double weightPower = NormalizePreferenceWeight(prefs.PreferenceWeights.GetValueOrDefault("power", 1.0));
        double weightYear = 0.2; // Standaard gewicht voor bouwjaar

        // Als geen gewichten, gebruik comfort/sport score als fallback
        if (prefs.PreferenceWeights.Count == 0)
        {
            double basePower = 0.25;
            double baseBudget = 0.30;
            double baseYear = 0.20;
            double baseFuel = 0.25;

            double sportAdjustment = 1.0 - prefs.ComfortVsSportScore;
            weightPower = basePower + (sportAdjustment * 0.15);
            weightBudget = baseBudget + (prefs.ComfortVsSportScore * 0.10);
            weightYear = baseYear;
            weightFuel = baseFuel - (sportAdjustment * 0.05);
        }

        // Normaliseer zodat ze optellen tot 1.0
        double total = weightPower + weightBudget + weightYear + weightFuel;
        if (total > 0)
        {
            return (weightPower / total, weightBudget / total, weightYear / total, weightFuel / total);
        }
        return (0.25, 0.30, 0.20, 0.25); // Fallback
    }

    /// <summary>
    /// Normaliseert preference weight (0.0-1.5) naar similarity weight (0.0-1.0).
    /// </summary>
    private double NormalizePreferenceWeight(double prefWeight)
    {
        // 0.0-1.5 → 0.0-1.0
        return Math.Min(1.0, prefWeight / 1.5);
    }

    /// <summary>
    /// Berekent weighted similarity op basis van user preferences en hun gewichten.
    /// Gebruikt PreferenceWeights om elke feature te wegen.
    /// </summary>
    private double CalculateWeightedSimilarity(Car car, Car idealCar, UserPreferences prefs,
        int minPower, int maxPower, decimal minBudget, decimal maxBudget, int minYear, int maxYear)
    {
        double totalScore = 0.0;
        double totalWeight = 0.0;

        // Budget similarity
        if (prefs.MaxBudget.HasValue && car.Budget > 0)
        {
            double budgetWeight = prefs.PreferenceWeights.GetValueOrDefault("budget", 1.0);
            double budgetSimilarity = CalculateBudgetSimilarity(car, idealCar, minBudget, maxBudget);
            totalScore += budgetSimilarity * budgetWeight;
            totalWeight += budgetWeight;
        }

        // Fuel similarity
        if (!string.IsNullOrWhiteSpace(prefs.PreferredFuel))
        {
            double fuelWeight = prefs.PreferenceWeights.GetValueOrDefault("fuel", 1.0);
            double fuelSimilarity = CalculateFuelSimilarity(car, idealCar);
            totalScore += fuelSimilarity * fuelWeight;
            totalWeight += fuelWeight;
        }

        // Power similarity
        if (prefs.MinPower.HasValue && car.Power > 0)
        {
            double powerWeight = prefs.PreferenceWeights.GetValueOrDefault("power", 1.0);
            double powerSimilarity = CalculatePowerSimilarity(car, idealCar, prefs.MinPower.Value, minPower, maxPower);
            totalScore += powerSimilarity * powerWeight;
            totalWeight += powerWeight;
        }

        // Transmission similarity
        if (prefs.AutomaticTransmission.HasValue && prefs.PreferenceWeights.ContainsKey("transmission"))
        {
            double transWeight = prefs.PreferenceWeights["transmission"];
            // Note: we hebben geen Transmission property in Car, dus skip voor nu
            // In de toekomst kan dit toegevoegd worden
        }

        // Year similarity (altijd meenemen)
        double yearWeight = 0.2;
        double yearSimilarity = CalculateYearSimilarity(car, idealCar, minYear, maxYear);
        totalScore += yearSimilarity * yearWeight;
        totalWeight += yearWeight;

        // Normaliseer
        if (totalWeight > 0)
        {
            return Math.Max(0.0, Math.Min(1.0, totalScore / totalWeight));
        }
        return 0.0;
    }

    /// <summary>
    /// Berekent budget similarity (0-1).
    /// Als er een MaxBudget preference is, geeft hogere score aan auto's die dicht bij het max budget liggen.
    /// </summary>
    private double CalculateBudgetSimilarity(Car car, Car idealCar, decimal minBudget, decimal maxBudget)
    {
        if (maxBudget <= minBudget || car.Budget <= 0 || idealCar.Budget <= 0)
            return 0.5;

        // Als de auto boven het max budget valt, geef lage score
        if (car.Budget > idealCar.Budget && idealCar.Budget > 0)
        {
            // Straf voor auto's boven budget, maar niet te hard (misschien is het net iets boven budget)
            double overBudget = (double)(car.Budget - idealCar.Budget) / (double)idealCar.Budget;
            return Math.Max(0.0, 0.5 - (overBudget * 0.5)); // Max 0.5 straf
        }

        // Als er een max budget is (idealCar.Budget is het max budget), geef hogere score aan auto's dicht bij max budget
        // Bijvoorbeeld: max 25k → auto van 24k krijgt hogere score dan auto van 15k
        // BUG FIX: Verhoog threshold naar 90% en geef sterkere bonus voor auto's dicht bij budget
        if (idealCar.Budget > 0 && car.Budget <= idealCar.Budget)
        {
            // Bereken hoe dicht de auto bij het max budget ligt (0-1, waarbij 1 = dicht bij max budget)
            // Auto's tussen 90-100% van max budget krijgen de hoogste score (optimaal bereik)
            double budgetRatio = (double)car.Budget / (double)idealCar.Budget;
            
            // Geef sterke bonus voor auto's dicht bij max budget (90-100% ratio = optimaal)
            if (budgetRatio >= 0.90)
            {
                // Auto's tussen 90-100% van max budget krijgen 0.90-1.0 score (optimaal bereik)
                return 0.90 + ((budgetRatio - 0.90) / 0.10) * 0.10; // Scale van 0.90-1.0
            }
            else if (budgetRatio >= 0.75)
            {
                // Auto's tussen 75-90% van max budget krijgen 0.70-0.90 score (goed bereik)
                return 0.70 + ((budgetRatio - 0.75) / 0.15) * 0.20; // Scale van 0.70-0.90
            }
            else
            {
                // Auto's onder 75% krijgen lagere score (nog steeds acceptabel maar niet optimaal)
                return (budgetRatio / 0.75) * 0.70; // Scale van 0-0.70
            }
        }

        // Fallback: normale similarity berekening
        double normCar = (double)(car.Budget - minBudget) / (double)(maxBudget - minBudget);
        double normIdeal = (double)(idealCar.Budget - minBudget) / (double)(maxBudget - minBudget);
        double distance = Math.Abs(normCar - normIdeal);
        return 1.0 - distance;
    }

    /// <summary>
    /// Berekent fuel similarity (0-1).
    /// </summary>
    private double CalculateFuelSimilarity(Car car, Car idealCar)
    {
        string fuel1 = car.Fuel.ToLower().Trim();
        string fuel2 = idealCar.Fuel.ToLower().Trim();

        if (fuel1 == fuel2) return 1.0;
        
        // Gedeeltelijke match
        if ((fuel1.Contains("hybrid") && fuel2.Contains("hybrid")) ||
            (fuel1.Contains("electric") && fuel2.Contains("electric")) ||
            (fuel1.Contains("petrol") && fuel2.Contains("petrol")) ||
            (fuel1.Contains("diesel") && fuel2.Contains("diesel")))
        {
            return 0.5;
        }
        return 0.0;
    }

    /// <summary>
    /// Berekent power similarity (0-1).
    /// Als MinPower <= 1.0, is het een score (0-1), anders exacte KW waarde.
    /// </summary>
    private double CalculatePowerSimilarity(Car car, Car idealCar, double minPowerPref, int minPower, int maxPower)
    {
        if (maxPower <= minPower || car.Power <= 0)
            return 0.5;

        double targetPower;
        
        // Als MinPower <= 1.0, is het een score - converteer naar KW
        if (minPowerPref <= 1.0)
        {
            // Score (0.0-1.0) → percentage van bereik
            targetPower = minPower + (minPowerPref * (maxPower - minPower));
        }
        else
        {
            // Exacte KW waarde
            targetPower = minPowerPref;
        }

        double normCar = (double)(car.Power - minPower) / (double)(maxPower - minPower);
        double normTarget = (targetPower - minPower) / (maxPower - minPower);
        double distance = Math.Abs(normCar - normTarget);
        return 1.0 - distance;
    }

    /// <summary>
    /// Berekent year similarity (0-1).
    /// </summary>
    private double CalculateYearSimilarity(Car car, Car idealCar, int minYear, int maxYear)
    {
        if (maxYear <= minYear || car.Year < 1900 || idealCar.Year < 1900)
            return 0.5;

        double normCar = (double)(car.Year - minYear) / (double)(maxYear - minYear);
        double normIdeal = (double)(idealCar.Year - minYear) / (double)(maxYear - minYear);
        double distance = Math.Abs(normCar - normIdeal);
        return 1.0 - distance;
    }

    /// <summary>
    /// Maakt een "ideale" target auto op basis van preferences voor similarity vergelijking.
    /// </summary>
    private Car CreateIdealCarFromPreferences(UserPreferences prefs, List<Car> availableCars)
    {
        Car ideal = new Car
        {
            Brand = "Ideal",
            Model = "Car",
            Year = DateTime.Now.Year - 2,  // Relatief nieuw
            Fuel = prefs.PreferredFuel ?? "petrol",
            Budget = (decimal)(prefs.MaxBudget ?? 30000),
            Power = (int)(prefs.MinPower ?? 120)
        };

        // Als er geen preferences zijn, gebruik gemiddelde van beschikbare auto's
        if (prefs.MaxBudget == null && availableCars.Count > 0)
        {
            var validBudget = availableCars.Where(c => c.Budget > 0).Select(c => (double)c.Budget);
            if (validBudget.Any())
            {
                ideal.Budget = (decimal)validBudget.Average();
            }
        }

        if (prefs.MinPower == null && availableCars.Count > 0)
        {
            var validPower = availableCars.Where(c => c.Power > 0).Select(c => c.Power);
            if (validPower.Any())
            {
                ideal.Power = (int)validPower.Average();
            }
        }

        return ideal;
    }

    /// <summary>
    /// Genereert recommendations op basis van manuele filters (zonder tekst parsing).
    /// Gebruiker geeft expliciet alle voorkeuren op via formulier.
    /// 
    /// VERSCHIL MET TEKST MODUS:
    /// - Tekst modus: Parseert vrije tekst met TextParserService
    /// - Manuele modus: Directe mapping van formulier velden naar FilterCriteria
    /// - Geen gewichten nodig (alle filters zijn even belangrijk)
    /// </summary>
    public List<RecommendationResult> RecommendFromManualFilters(ManualFilterRequest request, int n = 5)
    {
        EnsureInitialized();
        
        List<Car> allCars = _carRepository.GetAllCars();
        
        // Converteer ManualFilterRequest naar FilterCriteria (directe mapping, geen parsing)
        var filterCriteria = new RuleBasedFilter.FilterCriteria
        {
            MinBudget = request.MinPrice,
            MaxBudget = request.MaxPrice,
            PreferredFuel = request.Fuel,
            PreferredBrand = request.Brand,
            PreferredBodyType = request.BodyType,
            AutomaticTransmission = request.Transmission,
            MinYear = request.MinYear,
            MaxYear = request.MaxYear
        };
        
        // Extra filter voor model (niet in FilterCriteria, dus apart toepassen)
        if (!string.IsNullOrWhiteSpace(request.Model))
        {
            string modelLower = request.Model.ToLower().Trim();
            allCars = allCars.Where(c => 
                !string.IsNullOrWhiteSpace(c.Model) && 
                c.Model.ToLower().Trim().Contains(modelLower)).ToList();
        }
        
        // Extra filter voor minimum vermogen (niet in FilterCriteria, dus apart toepassen)
        if (request.MinPower.HasValue)
        {
            allCars = allCars.Where(c => c.Power >= request.MinPower.Value).ToList();
        }
        
        // OLD AI: Rule-based filtering - bepaal candidate set met harde filters
        List<Car> candidateCars = _ruleBasedFilter.FilterCars(allCars, filterCriteria);
        
        // Als geen auto's matchten filters, gebruik alle auto's (maar met lagere scores)
        if (candidateCars.Count == 0)
        {
            candidateCars = allCars;
        }
        
        // NEW AI: Content-based similarity + Advanced scoring
        // Maak UserPreferences object voor similarity berekening
        UserPreferences prefs = new UserPreferences
        {
            MaxBudget = request.MaxPrice.HasValue ? (double)request.MaxPrice.Value : null,
            PreferredFuel = request.Fuel,
            PreferredBrand = request.Brand,
            AutomaticTransmission = request.Transmission,
            BodyTypePreference = request.BodyType,
            MinPower = request.MinPower.HasValue ? request.MinPower.Value : null
        };
        
        // Maak ideale feature vector op basis van preferences
        CarFeatureVector idealVector = _featureVectorFactory.CreateIdealVector(prefs, candidateCars);
        
        // Bereken similarity en ranking scores voor alle candidate auto's met geavanceerde scoring
        List<RecommendationResult> results = new List<RecommendationResult>();
        
        foreach (Car car in candidateCars)
        {
            // Skip auto's zonder geldige data
            if (car.Power <= 0 || car.Budget <= 0 || car.Year < 1900)
                continue;
            
            // Gebruik AdvancedScoringService voor slimmere scores met transparantie
            var featureScores = _advancedScoringService.CalculateScores(car, prefs, idealVector, allCars);
            
            // Gebruik finale score uit AdvancedScoringService
            double finalScore = featureScores.FinalScore;
            
            string explanation = _explanationBuilder.BuildExplanation(car, prefs, finalScore);
            
            results.Add(new RecommendationResult
            {
                Car = car,
                SimilarityScore = finalScore,
                Explanation = explanation,
                FeatureScores = featureScores
            });
        }
        
        // Sorteer op score met controlled randomness
        var rankedResults = _rankingService.RankWithControlledRandomness(results);
        
        // Verwijder dubbele modellen (behoud hoogste score per merk+model) en pak top N
        var finalResults = rankedResults
            .GroupBy(r => new { r.Car.Brand, r.Car.Model })
            .Select(g => g.OrderByDescending(r => r.SimilarityScore).First())
            .Take(n)
            .ToList();

        return finalResults;
    }
}




