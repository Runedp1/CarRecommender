namespace CarRecommender;

/// <summary>
/// Geavanceerde scoring service die slimmere en consistentere scores genereert.
/// 
/// Implementeert:
/// - Budget-logica: auto's dicht bij max-budget scoren hoger
/// - Feature-scores: aparte scores per feature (prijs, vermogen, bouwjaar, brandstof, merk, carrosserie, transmissie)
/// - Normalisatie: numerieke features worden genormaliseerd naar 0-1
/// - Utility-functie: combineert feature-scores met gewichten
/// - Similarity-integratie: combineert utility-score met cosine similarity
/// - Transparantie: retourneert deel-scores voor uitleg
/// </summary>
public class AdvancedScoringService
{
    /// <summary>
    /// Configuratie voor scoring gewichten.
    /// Deze gewichten bepalen hoe belangrijk elke feature is in de totale score.
    /// </summary>
    public class ScoringWeights
    {
        /// <summary>
        /// Gewicht voor prijs-score (t.o.v. budget).
        /// Standaard: 0.25 (25% van utility-score).
        /// </summary>
        public double PriceWeight { get; set; } = 0.25;

        /// <summary>
        /// Gewicht voor vermogen-score (t.o.v. sportief/comfortabel voorkeur).
        /// Standaard: 0.20 (20% van utility-score).
        /// </summary>
        public double PowerWeight { get; set; } = 0.20;

        /// <summary>
        /// Gewicht voor bouwjaar-score (t.o.v. nieuw/oud voorkeur).
        /// Standaard: 0.15 (15% van utility-score).
        /// </summary>
        public double YearWeight { get; set; } = 0.15;

        /// <summary>
        /// Gewicht voor brandstof-match.
        /// Standaard: 0.15 (15% van utility-score).
        /// </summary>
        public double FuelWeight { get; set; } = 0.15;

        /// <summary>
        /// Gewicht voor merk-match.
        /// Standaard: 0.10 (10% van utility-score).
        /// </summary>
        public double BrandWeight { get; set; } = 0.10;

        /// <summary>
        /// Gewicht voor carrosserie-match.
        /// Standaard: 0.10 (10% van utility-score).
        /// </summary>
        public double BodyTypeWeight { get; set; } = 0.10;

        /// <summary>
        /// Gewicht voor transmissie-match.
        /// Standaard: 0.05 (5% van utility-score).
        /// </summary>
        public double TransmissionWeight { get; set; } = 0.05;

        /// <summary>
        /// Gewicht voor similarity-score (cosine similarity) in finale ranking.
        /// Standaard: 0.6 (60% van finale score).
        /// </summary>
        public double SimilarityWeight { get; set; } = 0.6;

        /// <summary>
        /// Gewicht voor utility-score in finale ranking.
        /// Standaard: 0.4 (40% van finale score).
        /// </summary>
        public double UtilityWeight { get; set; } = 0.4;

        /// <summary>
        /// Valideert dat gewichten optellen tot ~1.0 en normaliseert indien nodig.
        /// </summary>
        public void Normalize()
        {
            double totalFeatureWeight = PriceWeight + PowerWeight + YearWeight + FuelWeight + 
                                       BrandWeight + BodyTypeWeight + TransmissionWeight;
            
            if (totalFeatureWeight > 0.001) // Voorkom delen door nul
            {
                // Normaliseer feature gewichten
                PriceWeight /= totalFeatureWeight;
                PowerWeight /= totalFeatureWeight;
                YearWeight /= totalFeatureWeight;
                FuelWeight /= totalFeatureWeight;
                BrandWeight /= totalFeatureWeight;
                BodyTypeWeight /= totalFeatureWeight;
                TransmissionWeight /= totalFeatureWeight;
            }

            // Normaliseer finale gewichten
            double totalFinalWeight = SimilarityWeight + UtilityWeight;
            if (totalFinalWeight > 0.001)
            {
                SimilarityWeight /= totalFinalWeight;
                UtilityWeight /= totalFinalWeight;
            }
        }
    }

    /// <summary>
    /// Resultaat van feature scoring met transparantie.
    /// Bevat deel-scores per feature voor uitleg aan gebruiker.
    /// </summary>
    public class FeatureScoreResult
    {
        /// <summary>
        /// Prijs-score (0-1): hoe goed matcht de prijs met het budget?
        /// </summary>
        public double PriceScore { get; set; }

        /// <summary>
        /// Vermogen-score (0-1): hoe goed matcht het vermogen met de voorkeur?
        /// </summary>
        public double PowerScore { get; set; }

        /// <summary>
        /// Bouwjaar-score (0-1): hoe goed matcht het bouwjaar met de voorkeur?
        /// </summary>
        public double YearScore { get; set; }

        /// <summary>
        /// Brandstof-score (0-1): exacte match = 1.0, mismatch = 0.0.
        /// </summary>
        public double FuelScore { get; set; }

        /// <summary>
        /// Merk-score (0-1): exacte match = 1.0, mismatch = 0.0.
        /// </summary>
        public double BrandScore { get; set; }

        /// <summary>
        /// Carrosserie-score (0-1): exacte match = 1.0, mismatch = 0.0.
        /// </summary>
        public double BodyTypeScore { get; set; }

        /// <summary>
        /// Transmissie-score (0-1): exacte match = 1.0, mismatch = 0.0.
        /// </summary>
        public double TransmissionScore { get; set; }

        /// <summary>
        /// Totale utility-score (gewogen som van alle feature-scores).
        /// </summary>
        public double UtilityScore { get; set; }

        /// <summary>
        /// Cosine similarity-score tussen feature vectors.
        /// </summary>
        public double SimilarityScore { get; set; }

        /// <summary>
        /// Finale gecombineerde score (similarity + utility).
        /// </summary>
        public double FinalScore { get; set; }
    }

    private readonly ScoringWeights _weights;
    private readonly CarFeatureVectorFactory _featureVectorFactory;
    private readonly SimilarityService _similarityService;
    private readonly MlRecommendationService? _mlService;
    private readonly ICarRepository? _carRepository; // Voor ML service die alle auto's nodig heeft

    public AdvancedScoringService(
        ScoringWeights? weights = null,
        CarFeatureVectorFactory? featureVectorFactory = null,
        SimilarityService? similarityService = null,
        MlRecommendationService? mlService = null,
        ICarRepository? carRepository = null)
    {
        _weights = weights ?? new ScoringWeights();
        _weights.Normalize();
        _featureVectorFactory = featureVectorFactory ?? new CarFeatureVectorFactory();
        _similarityService = similarityService ?? new SimilarityService();
        _mlService = mlService;
        _carRepository = carRepository;
    }

    /// <summary>
    /// Berekent geavanceerde scores voor een auto op basis van user preferences.
    /// Retourneert FeatureScoreResult met alle deel-scores voor transparantie.
    /// </summary>
    public FeatureScoreResult CalculateScores(
        Car car,
        UserPreferences preferences,
        CarFeatureVector idealVector,
        List<Car> allCars)
    {
        var result = new FeatureScoreResult();

        // 1. Bereken feature-scores
        result.PriceScore = CalculatePriceScore(car, preferences, allCars);
        result.PowerScore = CalculatePowerScore(car, preferences, allCars);
        result.YearScore = CalculateYearScore(car, preferences, allCars);
        result.FuelScore = CalculateFuelScore(car, preferences);
        result.BrandScore = CalculateBrandScore(car, preferences);
        result.BodyTypeScore = CalculateBodyTypeScore(car, preferences);
        result.TransmissionScore = CalculateTransmissionScore(car, preferences);

        // 2. Bereken utility-score (gewogen som van feature-scores)
        result.UtilityScore = CalculateUtilityScore(result);

        // 3. Bereken similarity-score (cosine similarity tussen feature vectors)
        CarFeatureVector carVector = _featureVectorFactory.CreateVector(car);
        result.SimilarityScore = _similarityService.CalculateCosineSimilarity(carVector, idealVector);

        // 4. Bereken finale score (gecombineerde similarity + utility)
        result.FinalScore = (result.SimilarityScore * _weights.SimilarityWeight) +
                           (result.UtilityScore * _weights.UtilityWeight);

        // Clamp naar 0-1 bereik
        result.FinalScore = Math.Max(0.0, Math.Min(1.0, result.FinalScore));

        return result;
    }

    /// <summary>
    /// Berekent prijs-score op basis van budget-logica.
    /// 
    /// ECONOMISCHE LOGICA:
    /// - Als gebruiker max-budget opgeeft (bv. 25k), willen we auto's die dicht bij dat budget liggen
    ///   (bv. 23-25k) hoger scoren dan auto's die véél goedkoper zijn (bv. 8k).
    /// - Reden: gebruikers geven max-budget op omdat ze die capaciteit hebben. Auto's dicht bij budget
    ///   bieden waarschijnlijk meer waarde/features voor het beschikbare budget.
    /// - Auto's boven budget krijgen een penalty (maar niet te hard, misschien is het net iets boven).
    /// - Auto's ver onder budget krijgen een kleinere penalty (ze zijn nog steeds bruikbaar, maar niet optimaal).
    /// 
    /// SCORING FUNCTIE:
    /// - Auto's tussen 85-100% van max-budget: score 0.9-1.0 (optimaal)
    /// - Auto's tussen 70-85% van max-budget: score 0.7-0.9 (goed)
    /// - Auto's tussen 50-70% van max-budget: score 0.5-0.7 (redelijk)
    /// - Auto's onder 50% van max-budget: score 0.0-0.5 (te goedkoop, waarschijnlijk niet optimaal)
    /// - Auto's boven max-budget: score 0.0-0.3 (penalty, maar niet volledig uitsluiten)
    /// </summary>
    private double CalculatePriceScore(Car car, UserPreferences preferences, List<Car> allCars)
    {
        if (!preferences.MaxBudget.HasValue || preferences.MaxBudget.Value <= 0)
        {
            // Geen budget voorkeur: gebruik neutrale score gebaseerd op dataset gemiddelde
            var validCars = allCars.Where(c => c.Budget > 0).ToList();
            if (validCars.Count == 0)
                return 0.5;

            double avgBudget = (double)validCars.Average(c => c.Budget);
            double carBudget = (double)car.Budget;

            // Score gebaseerd op afstand tot gemiddelde (normaliseer naar 0-1)
            double maxBudgetInDataset = (double)validCars.Max(c => c.Budget);
            double minBudgetInDataset = (double)validCars.Min(c => c.Budget);
            
            if (maxBudgetInDataset <= minBudgetInDataset)
                return 0.5;

            // Hoe dichter bij gemiddelde, hoe hoger de score
            double distanceFromAvg = Math.Abs(carBudget - avgBudget) / (maxBudgetInDataset - minBudgetInDataset);
            return Math.Max(0.0, Math.Min(1.0, 1.0 - distanceFromAvg));
        }

        double maxBudget = preferences.MaxBudget.Value;
        double carPrice = (double)car.Budget;

        // Auto boven budget: penalty
        if (carPrice > maxBudget)
        {
            // Bereken hoeveel procent boven budget
            double overBudgetRatio = (carPrice - maxBudget) / maxBudget;
            
            // Kleine overschrijding (< 5%): lichte penalty (0.3-0.5)
            if (overBudgetRatio <= 0.05)
            {
                return 0.5 - (overBudgetRatio / 0.05) * 0.2; // 0.3-0.5
            }
            // Grote overschrijding (> 5%): zware penalty (0.0-0.3)
            else
            {
                double penalty = 0.3 - Math.Min(0.3, (overBudgetRatio - 0.05) * 2.0);
                return Math.Max(0.0, penalty);
            }
        }

        // Auto onder budget: continue scorefunctie gebaseerd op afstand tot max-budget
        double budgetRatio = carPrice / maxBudget; // 0.0 - 1.0

        // Optimaal bereik: 85-100% van max-budget
        if (budgetRatio >= 0.85)
        {
            // Lineaire schaal van 0.9 naar 1.0
            return 0.9 + ((budgetRatio - 0.85) / 0.15) * 0.1;
        }
        // Goed bereik: 70-85% van max-budget
        else if (budgetRatio >= 0.70)
        {
            // Lineaire schaal van 0.7 naar 0.9
            return 0.7 + ((budgetRatio - 0.70) / 0.15) * 0.2;
        }
        // Redelijk bereik: 50-70% van max-budget
        else if (budgetRatio >= 0.50)
        {
            // Lineaire schaal van 0.5 naar 0.7
            return 0.5 + ((budgetRatio - 0.50) / 0.20) * 0.2;
        }
        // Te goedkoop: onder 50% van max-budget
        else
        {
            // Lineaire schaal van 0.0 naar 0.5
            return (budgetRatio / 0.50) * 0.5;
        }
    }

    /// <summary>
    /// Berekent vermogen-score op basis van sportief/comfortabel voorkeur.
    /// 
    /// LOGICA:
    /// - Sportief (ComfortVsSportScore < 0.5): hoger vermogen = hogere score
    /// - Comfortabel (ComfortVsSportScore > 0.5): redelijk vermogen (niet te laag, niet te hoog) = hogere score
    /// - Neutraal (ComfortVsSportScore ≈ 0.5): gemiddeld vermogen = hogere score
    /// </summary>
    private double CalculatePowerScore(Car car, UserPreferences preferences, List<Car> allCars)
    {
        if (car.Power <= 0)
            return 0.5;

        var validCars = allCars.Where(c => c.Power > 0).ToList();
        if (validCars.Count == 0)
            return 0.5;

        int minPower = validCars.Min(c => c.Power);
        int maxPower = validCars.Max(c => c.Power);
        double normalizedPower = (double)(car.Power - minPower) / (maxPower - minPower); // 0-1

        // Sportief voorkeur
        if (preferences.ComfortVsSportScore < 0.4)
        {
            // Hoger vermogen = hogere score (lineair)
            return normalizedPower;
        }
        // Comfortabel voorkeur
        else if (preferences.ComfortVsSportScore > 0.6)
        {
            // Ideaal vermogen voor comfort: rond 0.3-0.5 van het bereik (120-180 KW typisch)
            double idealNormalized = 0.4; // 40% van het bereik
            double distanceFromIdeal = Math.Abs(normalizedPower - idealNormalized);
            return Math.Max(0.0, 1.0 - (distanceFromIdeal * 2.0)); // Penalty voor afwijking
        }
        // Neutraal
        else
        {
            // Gemiddeld vermogen = hogere score
            double idealNormalized = 0.5;
            double distanceFromIdeal = Math.Abs(normalizedPower - idealNormalized);
            return Math.Max(0.0, 1.0 - (distanceFromIdeal * 2.0));
        }
    }

    /// <summary>
    /// Berekent bouwjaar-score op basis van nieuw/oud voorkeur.
    /// 
    /// LOGICA:
    /// - Nieuwere auto's krijgen over het algemeen hogere score (betere technologie, minder slijtage)
    /// - Maar als gebruiker expliciet "oud" vraagt, krijgen oudere auto's hogere score
    /// </summary>
    private double CalculateYearScore(Car car, UserPreferences preferences, List<Car> allCars)
    {
        if (car.Year < 1900)
            return 0.5;

        var validCars = allCars.Where(c => c.Year >= 1900).ToList();
        if (validCars.Count == 0)
            return 0.5;

        int minYear = validCars.Min(c => c.Year);
        int maxYear = validCars.Max(c => c.Year);
        double normalizedYear = (double)(car.Year - minYear) / (maxYear - minYear); // 0-1

        // Standaard: nieuwere auto's krijgen hogere score
        // (normalizedYear = 1.0 voor nieuwste auto)
        return normalizedYear;
    }

    /// <summary>
    /// Berekent brandstof-score: exacte match = 1.0, mismatch = 0.0.
    /// </summary>
    private double CalculateFuelScore(Car car, UserPreferences preferences)
    {
        if (string.IsNullOrWhiteSpace(preferences.PreferredFuel))
            return 0.5; // Geen voorkeur: neutrale score

        string carFuel = (car.Fuel ?? "").ToLower().Trim();
        string preferredFuel = preferences.PreferredFuel.ToLower().Trim();

        // Exacte match
        if (carFuel == preferredFuel)
            return 1.0;

        // Gedeeltelijke match (bijv. "plug-in hybrid" matcht "hybrid")
        if ((carFuel.Contains("hybrid") && preferredFuel.Contains("hybrid")) ||
            (carFuel.Contains("electric") && preferredFuel.Contains("electric")) ||
            (carFuel.Contains("petrol") && preferredFuel.Contains("petrol")) ||
            (carFuel.Contains("diesel") && preferredFuel.Contains("diesel")))
        {
            return 0.7; // Gedeeltelijke match krijgt 70% score
        }

        return 0.0; // Geen match
    }

    /// <summary>
    /// Berekent merk-score: exacte match = 1.0, mismatch = 0.0.
    /// </summary>
    private double CalculateBrandScore(Car car, UserPreferences preferences)
    {
        if (string.IsNullOrWhiteSpace(preferences.PreferredBrand))
            return 0.5; // Geen voorkeur: neutrale score

        string carBrand = (car.Brand ?? "").ToLower().Trim();
        string preferredBrand = preferences.PreferredBrand.ToLower().Trim();

        // Exacte match
        if (carBrand == preferredBrand)
            return 1.0;

        // Gedeeltelijke match (bijv. "mercedes-benz" matcht "mercedes")
        if (carBrand.Contains(preferredBrand) || preferredBrand.Contains(carBrand))
            return 0.8; // Gedeeltelijke match krijgt 80% score

        return 0.0; // Geen match
    }

    /// <summary>
    /// Berekent carrosserie-score: exacte match = 1.0, mismatch = 0.0.
    /// </summary>
    private double CalculateBodyTypeScore(Car car, UserPreferences preferences)
    {
        if (string.IsNullOrWhiteSpace(preferences.BodyTypePreference))
            return 0.5; // Geen voorkeur: neutrale score

        string carBodyType = (car.BodyType ?? "").ToLower().Trim();
        string preferredBodyType = preferences.BodyTypePreference.ToLower().Trim();

        // Exacte match
        if (carBodyType == preferredBodyType)
            return 1.0;

        // Gedeeltelijke match
        if (carBodyType.Contains(preferredBodyType) || preferredBodyType.Contains(carBodyType))
            return 0.7; // Gedeeltelijke match krijgt 70% score

        return 0.0; // Geen match
    }

    /// <summary>
    /// Berekent transmissie-score: exacte match = 1.0, mismatch = 0.0.
    /// </summary>
    private double CalculateTransmissionScore(Car car, UserPreferences preferences)
    {
        if (!preferences.AutomaticTransmission.HasValue)
            return 0.5; // Geen voorkeur: neutrale score

        bool wantsAutomatic = preferences.AutomaticTransmission.Value;
        string carTransmission = (car.Transmission ?? "").ToLower().Trim();

        bool isAutomatic = carTransmission.Contains("automatic") || carTransmission.Contains("auto");
        bool isManual = carTransmission.Contains("manual") || carTransmission.Contains("manual");

        if (wantsAutomatic && isAutomatic)
            return 1.0;
        if (!wantsAutomatic && isManual)
            return 1.0;

        return 0.0; // Mismatch
    }

    /// <summary>
    /// Berekent utility-score als gewogen som van alle feature-scores.
    /// Dit is de deterministische component van de score (zonder ML/ratings).
    /// </summary>
    private double CalculateUtilityScore(FeatureScoreResult featureScores)
    {
        double utility = 
            (featureScores.PriceScore * _weights.PriceWeight) +
            (featureScores.PowerScore * _weights.PowerWeight) +
            (featureScores.YearScore * _weights.YearWeight) +
            (featureScores.FuelScore * _weights.FuelWeight) +
            (featureScores.BrandScore * _weights.BrandWeight) +
            (featureScores.BodyTypeScore * _weights.BodyTypeWeight) +
            (featureScores.TransmissionScore * _weights.TransmissionWeight);

        return Math.Max(0.0, Math.Min(1.0, utility));
    }

    /// <summary>
    /// ML-gebaseerde user-rating component: gebruikt ML.NET om populairiteit te voorspellen.
    /// 
    /// IMPLEMENTATIE:
    /// - Gebruikt ML.NET model om score te voorspellen op basis van car features
    /// - Leert van historische recommendation patterns
    /// - Retourneert genormaliseerde score (0-1) die gebruikt kan worden als extra component
    /// 
    /// TOEKOMSTIGE UITBREIDING:
    /// - Kan uitgebreid worden met echte user ratings wanneer beschikbaar
    /// - Kan collaborative filtering toevoegen voor personalisatie
    /// </summary>
    public double GetUserRatingComponent(int carId)
    {
        // Als ML service niet beschikbaar is, retourneer neutrale score
        if (_mlService == null || !_mlService.IsModelTrained)
        {
            return 0.0; // Geen impact als ML niet beschikbaar is
        }

        // Haal auto op (als repository beschikbaar is)
        if (_carRepository == null)
        {
            return 0.0;
        }

        var car = _carRepository.GetCarById(carId);
        if (car == null)
        {
            return 0.0;
        }

        // Haal alle auto's op voor normalisatie
        var allCars = _carRepository.GetAllCars();
        
        // Gebruik ML service om user rating component te berekenen
        return _mlService.GetUserRatingComponent(carId, car, allCars);
    }
}

