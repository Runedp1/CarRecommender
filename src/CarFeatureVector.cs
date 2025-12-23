namespace CarRecommender;

/// <summary>
/// New AI - Content-based recommender.
/// Representeert een auto als numerieke feature vector voor similarity berekening.
/// 
/// De vector bevat:
/// - Genormaliseerde numerieke waarden (prijs, km, bouwjaar, vermogen/kw)
/// - One-hot encoding voor categorische velden (merk, brandstof, transmissie, carrosserie)
/// </summary>
public class CarFeatureVector
{
    /// <summary>
    /// Genormaliseerde numerieke features (0.0 - 1.0).
    /// </summary>
    public double NormalizedPrice { get; set; }
    public double NormalizedYear { get; set; }
    public double NormalizedPower { get; set; }

    /// <summary>
    /// One-hot encoding voor categorische features.
    /// Elke dictionary bevat alle mogelijke waarden als keys, met 1.0 voor de aanwezige waarde en 0.0 voor de rest.
    /// </summary>
    public Dictionary<string, double> BrandEncoding { get; set; } = new Dictionary<string, double>();
    public Dictionary<string, double> FuelEncoding { get; set; } = new Dictionary<string, double>();
    public Dictionary<string, double> TransmissionEncoding { get; set; } = new Dictionary<string, double>();
    public Dictionary<string, double> BodyTypeEncoding { get; set; } = new Dictionary<string, double>();

    /// <summary>
    /// Converteert de feature vector naar een platte array voor cosine similarity berekening.
    /// </summary>
    public double[] ToArray()
    {
        var list = new List<double>
        {
            NormalizedPrice,
            NormalizedYear,
            NormalizedPower
        };

        // Voeg one-hot encodings toe
        list.AddRange(BrandEncoding.Values);
        list.AddRange(FuelEncoding.Values);
        list.AddRange(TransmissionEncoding.Values);
        list.AddRange(BodyTypeEncoding.Values);

        return list.ToArray();
    }

    /// <summary>
    /// Berekent de dimensie van de feature vector.
    /// </summary>
    public int Dimension => 3 + BrandEncoding.Count + FuelEncoding.Count + TransmissionEncoding.Count + BodyTypeEncoding.Count;
}

