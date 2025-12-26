namespace CarRecommender;

/// <summary>
/// New AI - Content-based recommender.
/// Berekent cosine similarity tussen feature vectors.
/// 
/// Cosine similarity meet de hoek tussen twee vectoren:
/// - 1.0 = identiek (hoek 0°)
/// - 0.0 = orthogonaal (hoek 90°)
/// - -1.0 = tegenovergesteld (hoek 180°)
/// 
/// Voor auto recommendations gebruiken we alleen positieve waarden (0.0 - 1.0).
/// </summary>
public class SimilarityService
{
    /// <summary>
    /// Berekent cosine similarity tussen twee feature vectors.
    /// Retourneert een waarde tussen 0.0 en 1.0.
    /// </summary>
    public double CalculateCosineSimilarity(CarFeatureVector vector1, CarFeatureVector vector2)
    {
        double[] v1 = vector1.ToArray();
        double[] v2 = vector2.ToArray();

        // Zorg dat beide vectoren dezelfde dimensie hebben
        if (v1.Length != v2.Length)
        {
            throw new ArgumentException($"Feature vectors hebben verschillende dimensies: {v1.Length} vs {v2.Length}");
        }

        // Bereken dot product
        double dotProduct = 0.0;
        for (int i = 0; i < v1.Length; i++)
        {
            dotProduct += v1[i] * v2[i];
        }

        // Bereken magnitudes (lengtes)
        double magnitude1 = CalculateMagnitude(v1);
        double magnitude2 = CalculateMagnitude(v2);

        // Voorkom delen door nul
        if (magnitude1 == 0.0 || magnitude2 == 0.0)
        {
            return 0.0;
        }

        // Cosine similarity = dot product / (magnitude1 * magnitude2)
        double similarity = dotProduct / (magnitude1 * magnitude2);

        // Clamp naar 0-1 bereik (cosine similarity kan theoretisch -1 tot 1 zijn, maar voor onze features is het altijd positief)
        return Math.Max(0.0, Math.Min(1.0, similarity));
    }

    /// <summary>
    /// Berekent de magnitude (lengte) van een vector.
    /// </summary>
    private double CalculateMagnitude(double[] vector)
    {
        double sumOfSquares = 0.0;
        foreach (double value in vector)
        {
            sumOfSquares += value * value;
        }
        return Math.Sqrt(sumOfSquares);
    }

    /// <summary>
    /// Berekent cosine similarity tussen een auto en een ideale auto (op basis van preferences).
    /// </summary>
    public double CalculateSimilarity(Car car, CarFeatureVector idealVector, CarFeatureVectorFactory factory)
    {
        CarFeatureVector carVector = factory.CreateVector(car);
        return CalculateCosineSimilarity(carVector, idealVector);
    }
}




