namespace CarRecommender;

/// <summary>
/// K-Nearest Neighbours (KNN) recommendation service.
/// Gebaseerd op Les 4: Spotify Recommendations.
/// 
/// KNN vindt de K meest gelijkende auto's door Euclidische afstand te berekenen
/// tussen feature vectors. Dit is een klassiek AI-algoritme (Old AI) dat geen
/// training nodig heeft - het werkt direct op de data.
/// 
/// Algoritme:
/// 1. Converteer alle auto's naar feature vectors (genormaliseerde waarden)
/// 2. Converteer user preferences naar een "ideale auto" vector
/// 3. Bereken Euclidische afstand tussen ideale auto en alle echte auto's
/// 4. Sorteer op afstand (kleinste afstand = meest gelijkend)
/// 5. Retourneer top K resultaten
/// </summary>
public class KnnRecommendationService
{
    private readonly CarFeatureVectorFactory _featureVectorFactory;
    private readonly int _k; // Aantal neighbors (standaard 5)

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="k">Aantal nearest neighbors (standaard 5 voor top 5 recommendations)</param>
    public KnnRecommendationService(int k = 5)
    {
        _featureVectorFactory = new CarFeatureVectorFactory();
        _k = k;
    }

    /// <summary>
    /// Initialiseert de feature vector factory met alle beschikbare auto's.
    /// Moet worden aangeroepen voordat FindNearestNeighbors gebruikt kan worden.
    /// </summary>
    public void Initialize(List<Car> allCars)
    {
        _featureVectorFactory.Initialize(allCars);
    }

    /// <summary>
    /// Vindt de K nearest neighbors voor een gegeven auto.
    /// Gebruikt Euclidische afstand zoals in Les 4.
    /// </summary>
    /// <param name="targetCar">De auto waarvoor we neighbors zoeken</param>
    /// <param name="candidateCars">Lijst van kandidaat auto's (na filtering)</param>
    /// <returns>Top K meest gelijkende auto's met afstand-score</returns>
    public List<RecommendationResult> FindNearestNeighbors(Car targetCar, List<Car> candidateCars)
    {
        if (candidateCars == null || candidateCars.Count == 0)
        {
            return new List<RecommendationResult>();
        }

        // 1. Maak feature vector van de target auto (de "ideale" auto)
        CarFeatureVector targetVector = _featureVectorFactory.CreateVector(targetCar);

        // 2. Bereken afstanden naar alle kandidaat auto's
        var distances = new List<(Car car, double distance)>();

        foreach (var candidate in candidateCars)
        {
            // Skip de target auto zelf als die in de kandidaten zit
            if (candidate.Id == targetCar.Id)
                continue;

            CarFeatureVector candidateVector = _featureVectorFactory.CreateVector(candidate);
            double distance = CalculateEuclideanDistance(targetVector, candidateVector);
            
            distances.Add((candidate, distance));
        }

        // 3. Sorteer op afstand (kleinste eerst = meest gelijkend)
        var sortedByDistance = distances
            .OrderBy(x => x.distance)
            .Take(_k)
            .ToList();

        // 4. Converteer naar RecommendationResult met similarity score
        // Similarity score = 1 / (1 + distance)
        // - distance = 0 → similarity = 1.0 (identiek)
        // - distance = 1 → similarity = 0.5
        // - distance → ∞ → similarity → 0
        var results = sortedByDistance.Select(x => new RecommendationResult
        {
            Car = x.car,
            SimilarityScore = ConvertDistanceToSimilarity(x.distance),
            Explanation = $"KNN distance: {x.distance:F4} (rank {sortedByDistance.IndexOf(x) + 1}/{_k})"
        }).ToList();

        return results;
    }

    /// <summary>
    /// Vindt de K nearest neighbors op basis van user preferences.
    /// Creëert een "ideale auto" vector uit de preferences en zoekt daar neighbors voor.
    /// </summary>
    /// <param name="preferences">User preferences (budget, fuel, power, etc.)</param>
    /// <param name="candidateCars">Lijst van kandidaat auto's (na filtering)</param>
    /// <returns>Top K meest gelijkende auto's</returns>
    public List<RecommendationResult> FindNearestNeighborsFromPreferences(
        UserPreferences preferences, 
        List<Car> candidateCars)
    {
        if (candidateCars == null || candidateCars.Count == 0)
        {
            return new List<RecommendationResult>();
        }

        // 1. Maak een "ideale auto" vector uit de preferences
        CarFeatureVector idealVector = _featureVectorFactory.CreateIdealVector(preferences);

        // 2. Bereken afstanden naar alle kandidaat auto's
        var distances = new List<(Car car, double distance)>();

        foreach (var candidate in candidateCars)
        {
            CarFeatureVector candidateVector = _featureVectorFactory.CreateVector(candidate);
            double distance = CalculateEuclideanDistance(idealVector, candidateVector);
            
            distances.Add((candidate, distance));
        }

        // 3. Sorteer op afstand en neem top K
        var sortedByDistance = distances
            .OrderBy(x => x.distance)
            .Take(_k)
            .ToList();

        // 4. Converteer naar RecommendationResult
        var results = sortedByDistance.Select(x => new RecommendationResult
        {
            Car = x.car,
            SimilarityScore = ConvertDistanceToSimilarity(x.distance),
            Explanation = $"KNN distance: {x.distance:F4} (rank {sortedByDistance.IndexOf(x) + 1}/{_k})"
        }).ToList();

        return results;
    }

    /// <summary>
    /// Berekent Euclidische afstand tussen twee feature vectors.
    /// Formule uit Les 4: d(p, q) = √( (p₁ − q₁)² + (p₂ − q₂)² + … + (pₙ − qₙ)² )
    /// 
    /// Voorbeeld uit les:
    /// p = [3, 8, 7, 5, 2, 9]
    /// q = [10, 8, 6, 6, 4, 5]
    /// d(p, q) = √( (3−10)² + (8−8)² + (7−6)² + (5−6)² + (2−4)² + (9−5)² ) ≈ 8.426
    /// </summary>
    private double CalculateEuclideanDistance(CarFeatureVector vector1, CarFeatureVector vector2)
    {
        double[] v1 = vector1.ToArray();
        double[] v2 = vector2.ToArray();

        // Zorg dat beide vectoren dezelfde dimensie hebben
        if (v1.Length != v2.Length)
        {
            throw new ArgumentException($"Feature vectors hebben verschillende dimensies: {v1.Length} vs {v2.Length}");
        }

        // Bereken sum of squared differences
        double sumOfSquares = 0.0;
        for (int i = 0; i < v1.Length; i++)
        {
            double difference = v1[i] - v2[i];
            sumOfSquares += difference * difference;
        }

        // Return de square root (= Euclidean distance)
        return Math.Sqrt(sumOfSquares);
    }

    /// <summary>
    /// Converteer afstand naar similarity score (0-1).
    /// 
    /// Formule: similarity = 1 / (1 + distance)
    /// 
    /// Reden voor deze formule:
    /// - KNN werkt met afstanden (kleiner = beter)
    /// - Ons recommendation systeem werkt met similarity scores (groter = beter)
    /// - Deze conversie zorgt dat:
    ///   * distance = 0 → similarity = 1.0 (perfecte match)
    ///   * distance = 1 → similarity = 0.5 (gemiddeld)
    ///   * distance → ∞ → similarity → 0 (totaal verschillend)
    /// </summary>
    private double ConvertDistanceToSimilarity(double distance)
    {
        // Voorkom delen door nul (hoewel distance nooit negatief kan zijn)
        if (distance < 0)
            distance = 0;

        return 1.0 / (1.0 + distance);
    }

    /// <summary>
    /// Test/debug methode: bereken afstand tussen twee specifieke auto's.
    /// Handig voor debugging en begrip van het algoritme.
    /// </summary>
    public double CalculateDistance(Car car1, Car car2)
    {
        CarFeatureVector v1 = _featureVectorFactory.CreateVector(car1);
        CarFeatureVector v2 = _featureVectorFactory.CreateVector(car2);
        return CalculateEuclideanDistance(v1, v2);
    }

    /// <summary>
    /// Geeft de huidige K waarde (aantal neighbors).
    /// </summary>
    public int K => _k;
}