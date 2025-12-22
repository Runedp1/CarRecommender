namespace CarRecommender;

/// <summary>
/// Car model - de basisklasse voor een auto.
/// Bevat alle info die we nodig hebben voor recommendations.
/// </summary>
public class Car
{
    public int Id { get; set; }
    public string Brand { get; set; } = string.Empty;  // merk
    public string Model { get; set; } = string.Empty;  // model
    public int Power { get; set; }  // vermogen (in pk of kW)
    public string Fuel { get; set; } = string.Empty;  // brandstof
    public decimal Budget { get; set; }  // budget/prijs
    public int Year { get; set; }  // bouwjaar
    public string ImagePath { get; set; } = string.Empty;  // pad naar de afbeelding (bijv. "images/brand/model/id.jpg")
    public string ImageUrl { get; set; } = string.Empty;  // URL naar een externe afbeelding van de auto
}

/// <summary>
/// Resultaat van een recommendation: de auto + hoe vergelijkbaar die is (score 0-1).
/// </summary>
public class RecommendationResult
{
    public Car Car { get; set; } = null!;
    public double SimilarityScore { get; set; }  // Score tussen 0 en 1 (1 = meest vergelijkbaar)
    public string Explanation { get; set; } = string.Empty;  // Uitleg waarom deze auto wordt aanbevolen
}

/// <summary>
/// Voorkeuren van de gebruiker, afgeleid uit tekst input.
/// Bevat gewichten per voorkeur om belang aan te geven (0.0 = optioneel, 1.5 = cruciaal).
/// </summary>
public class UserPreferences
{
    public double? MaxBudget { get; set; }  // Maximum budget in euro's
    public string? PreferredFuel { get; set; }  // Voorkeur brandstof (petrol/diesel/hybrid/electric)
    public bool? AutomaticTransmission { get; set; }  // true = automaat, false = schakel, null = geen voorkeur
    public double? MinPower { get; set; }  // Vermogen score (0.0 = laag, 1.0 = hoog) of exact KW als > 100
    public string? BodyTypePreference { get; set; }  // Voorkeur body type (suv/hatchback/sedan/station/etc.)
    public double ComfortVsSportScore { get; set; } = 0.5;  // 0 = puur sportief, 1 = puur comfort, 0.5 = neutraal
    
    /// <summary>
    /// Gewichten per voorkeur om belang aan te geven.
    /// Keys: "budget", "fuel", "transmission", "power", "bodytype", "comfort"
    /// Values: 0.0 (optioneel) tot 1.5 (cruciaal/must-have)
    /// </summary>
    public Dictionary<string, double> PreferenceWeights { get; set; } = new Dictionary<string, double>();
}


