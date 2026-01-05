namespace CarRecommender;

/// <summary>
/// Bouwt uitleg teksten voor recommendations op basis van echte data.
/// Gebruikt alleen informatie uit Car en UserPreferences.
/// </summary>
public class ExplanationBuilder
{
    public ExplanationBuilder()
    {
    }

    /// <summary>
    /// Genereert een Nederlandstalige uitleg waarom een auto wordt aanbevolen.
    /// Toont gewichten voor transparantie over belangrijke voorkeuren.
    /// Kan ook collaborative filtering uitleg toevoegen.
    /// </summary>
    public string BuildExplanation(Car car, UserPreferences prefs, double similarityScore, CollaborativeScore? collaborativeScore = null)
    {
        List<string> reasons = new List<string>();

        // Voeg redenen toe met gewichten
        if (prefs.PreferredBrand != null)
        {
            string brandMatch = MatchBrand(car.Brand, prefs.PreferredBrand);
            if (!string.IsNullOrEmpty(brandMatch))
            {
                double brandWeight = prefs.PreferenceWeights.GetValueOrDefault("brand", 1.0);
                string weightText = GetWeightDescription(brandWeight);
                reasons.Add($"{brandMatch}");
            }
        }

        if (prefs.PreferredFuel != null)
        {
            string fuelMatch = MatchFuel(car.Fuel, prefs.PreferredFuel);
            if (!string.IsNullOrEmpty(fuelMatch))
            {
                double fuelWeight = prefs.PreferenceWeights.GetValueOrDefault("fuel", 1.0);
                string weightText = GetWeightDescription(fuelWeight);
                reasons.Add($"{fuelMatch}");
            }
        }

        if (prefs.MaxBudget.HasValue && car.Budget > 0 && car.Budget <= (decimal)prefs.MaxBudget.Value)
        {
            double budgetWeight = prefs.PreferenceWeights.GetValueOrDefault("budget", 1.0);
            string weightText = GetWeightDescription(budgetWeight);
            reasons.Add($"blijft onder je budget van €{prefs.MaxBudget.Value:N0}");
        }

        // Power check - kan score zijn (0-1) of exacte PK
        if (prefs.MinPower.HasValue && car.Power > 0)
        {
            double powerWeight = prefs.PreferenceWeights.GetValueOrDefault("power", 1.0);
            string weightText = GetWeightDescription(powerWeight);
            
            bool powerMatches = false;
            if (prefs.MinPower.Value <= 1.0)
            {
                // Score - check of vermogen voldoende is
                powerMatches = true; // Voor nu altijd true bij score
                string powerDesc = GetInformalPowerDescription(prefs.MinPower.Value);
                reasons.Add($"heeft {car.Power} PK vermogen");
            }
            else
            {
                // Exacte KW waarde
                powerMatches = car.Power >= prefs.MinPower.Value;
                if (powerMatches)
                {
                    reasons.Add($"heeft {car.Power} PK vermogen");
                }
            }
        }
        else if (car.Power > 0)
        {
            reasons.Add($"heeft {car.Power} PK vermogen");
        }

        if (prefs.BodyTypePreference != null)
        {
            string bodyMatch = MatchBodyType(car.BodyType, prefs.BodyTypePreference);
            if (!string.IsNullOrEmpty(bodyMatch))
            {
                double bodyWeight = prefs.PreferenceWeights.GetValueOrDefault("bodytype", 1.0);
                string weightText = GetWeightDescription(bodyWeight);
                reasons.Add($"{bodyMatch}");
            }
        }
        // Als er geen voorkeur is maar de auto heeft wel een body type, toon het als informatie
        else if (!string.IsNullOrWhiteSpace(car.BodyType))
        {
            string bodyTypeName = GetBodyTypeName(car.BodyType);
            if (!string.IsNullOrEmpty(bodyTypeName))
            {
                reasons.Add($"heeft {bodyTypeName} carrosserie");
            }
        }

        if (prefs.AutomaticTransmission.HasValue)
        {
            double transWeight = prefs.PreferenceWeights.GetValueOrDefault("transmission", 1.0);
            string weightText = GetWeightDescription(transWeight);
            
            // Check of de auto de gewenste transmissie heeft
            bool carIsAutomatic = !string.IsNullOrWhiteSpace(car.Transmission) && 
                                  car.Transmission.ToLower().Contains("automatic");
            bool userWantsAutomatic = prefs.AutomaticTransmission.Value;
            
            if (carIsAutomatic == userWantsAutomatic)
            {
                string transText = userWantsAutomatic ? "automaat" : "schakel";
                reasons.Add($"heeft {transText} transmissie");
            }
        }
        // Als er geen voorkeur is maar de auto heeft wel transmissie info, toon het als informatie
        else if (!string.IsNullOrWhiteSpace(car.Transmission))
        {
            string transText = GetTransmissionName(car.Transmission);
            if (!string.IsNullOrEmpty(transText))
            {
                reasons.Add($"heeft {transText} transmissie");
            }
        }

        // Bouw de uitleg
        string explanation = $"Deze {car.Brand} {car.Model} ({car.Year}) voldoet aan jouw voorkeuren";

        if (reasons.Count > 0)
        {
            explanation += ": " + string.Join(", ", reasons);
        }
        else
        {
            explanation += " op basis van algemene similarity";
        }

        explanation += $". Match: {(similarityScore * 100):F1}%";

        return explanation;
    }

    /// <summary>
    /// Geeft leesbare beschrijving van gewicht.
    /// </summary>
    private string GetWeightDescription(double weight)
    {
        if (weight >= 1.4) return "cruciaal";
        if (weight >= 0.9) return "belangrijk";
        if (weight >= 0.5) return "liever";
        return "optioneel";
    }

    /// <summary>
    /// Converteert vermogen score (0-1) naar informele beschrijving.
    /// </summary>
    private string GetInformalPowerDescription(double powerScore)
    {
        if (powerScore >= 0.7) return "veel vermogen";
        if (powerScore >= 0.4) return "voldoende vermogen";
        return "gemiddeld vermogen";
    }

    /// <summary>
    /// Check of merk match en geef beschrijving terug.
    /// </summary>
    private string MatchBrand(string carBrand, string preferredBrand)
    {
        string carBrandLower = carBrand.ToLower().Trim();
        string prefLower = preferredBrand.ToLower().Trim();

        // Exacte match
        if (carBrandLower == prefLower)
        {
            return $"is van het merk {carBrand}";
        }

        // Gedeeltelijke match (bijv. "mercedes" matcht "mercedes-benz")
        if (carBrandLower.Contains(prefLower) || prefLower.Contains(carBrandLower))
        {
            return $"is van het merk {carBrand}";
        }

        // Speciale gevallen
        if ((prefLower == "vw" || prefLower == "volks") && carBrandLower.Contains("volkswagen"))
        {
            return "is van het merk Volkswagen";
        }
        if (prefLower == "benz" && carBrandLower.Contains("mercedes"))
        {
            return "is van het merk Mercedes-Benz";
        }

        return string.Empty;
    }

    /// <summary>
    /// Check of brandstof match en geef beschrijving terug.
    /// </summary>
    private string MatchFuel(string carFuel, string preferredFuel)
    {
        string carFuelLower = carFuel.ToLower();
        string prefLower = preferredFuel.ToLower();

        if (carFuelLower.Contains(prefLower) || prefLower.Contains(carFuelLower))
        {
            return $"een {GetFuelName(carFuel)}-motor heeft";
        }

        // Gedeeltelijke matches
        if ((prefLower.Contains("electric") && carFuelLower.Contains("electric")) ||
            (prefLower.Contains("hybrid") && carFuelLower.Contains("hybrid")))
        {
            return $"een {GetFuelName(carFuel)}-motor heeft";
        }

        return string.Empty;
    }

    /// <summary>
    /// Geef leesbare brandstof naam.
    /// </summary>
    private string GetFuelName(string fuel)
    {
        string fuelLower = fuel.ToLower();
        if (fuelLower.Contains("electric")) return "elektrisch";
        if (fuelLower.Contains("hybrid")) return "hybride";
        if (fuelLower.Contains("diesel")) return "diesel";
        if (fuelLower.Contains("petrol") || fuelLower.Contains("benzine")) return "benzine";
        return fuel;
    }

    /// <summary>
    /// Check of car body type match met voorkeur.
    /// </summary>
    private string MatchBodyType(string? carBodyType, string preferredBodyType)
    {
        if (string.IsNullOrWhiteSpace(carBodyType))
            return string.Empty;
            
        string carBodyLower = carBodyType.ToLower().Trim();
        string prefLower = preferredBodyType.ToLower().Trim();

        // Exacte match
        if (carBodyLower == prefLower)
        {
            return $"een {GetBodyTypeName(carBodyType)} koetswerk heeft";
        }

        // Gedeeltelijke match
        if (carBodyLower.Contains(prefLower) || prefLower.Contains(carBodyLower))
        {
            return $"een {GetBodyTypeName(carBodyType)} koetswerk heeft";
        }

        // Speciale gevallen
        if ((prefLower == "wagon" || prefLower == "estate") && carBodyLower == "station")
        {
            return "een stationwagen koetswerk heeft";
        }
        if (prefLower == "convertible" && carBodyLower == "cabrio")
        {
            return "een cabrio koetswerk heeft";
        }

        return string.Empty;
    }

    /// <summary>
    /// Geef leesbare body type naam.
    /// </summary>
    private string GetBodyTypeName(string? bodyType)
    {
        if (string.IsNullOrWhiteSpace(bodyType))
            return string.Empty;
            
        string lower = bodyType.ToLower().Trim();
        return lower switch
        {
            "suv" => "SUV",
            "sedan" => "sedan",
            "hatchback" => "hatchback",
            "station" => "stationwagen",
            "cabrio" => "cabrio",
            "coupe" => "coupé",
            "wagon" => "stationwagen",
            "convertible" => "cabrio",
            _ => bodyType
        };
    }

    /// <summary>
    /// Geef leesbare transmissie naam.
    /// </summary>
    private string GetTransmissionName(string? transmission)
    {
        if (string.IsNullOrWhiteSpace(transmission))
            return string.Empty;
            
        string lower = transmission.ToLower().Trim();
        if (lower.Contains("automatic") || lower.Contains("automaat") || lower.Contains("automatisch") || lower.Contains("cvt") || lower.Contains("dct"))
        {
            return "automaat";
        }
        if (lower.Contains("manual") || lower.Contains("handmatig") || lower.Contains("schakel") || lower.Contains("handbak"))
        {
            return "schakel";
        }
        return transmission;
    }

    /// <summary>
    /// Geef beschrijving van vermogen op basis van comfort/sport score.
    /// </summary>
    private string GetPowerDescription(double comfortVsSport)
    {
        if (comfortVsSport < 0.3)
            return "sportief rijden";
        else if (comfortVsSport > 0.7)
            return "comfortabel rijden";
        else
            return "alle doeleinden";
    }
}

/// <summary>
/// Collaborative filtering score voor een auto.
/// NOTE: This class is kept here for compatibility with ExplanationBuilder.
/// The CollaborativeFilteringService that used to populate this has been moved to Legacy.
/// </summary>
public class CollaborativeScore
{
    /// <summary>
    /// Genormaliseerde score (0-1) op basis van gelijkaardige gebruikers.
    /// </summary>
    public double Score { get; set; }

    /// <summary>
    /// Aantal gelijkaardige gebruikers die deze auto hebben beoordeeld.
    /// </summary>
    public int UserCount { get; set; }

    /// <summary>
    /// Gemiddelde rating (1-5) van gelijkaardige gebruikers.
    /// </summary>
    public double AverageRating { get; set; }

    /// <summary>
    /// Of er collaborative data beschikbaar is.
    /// </summary>
    public bool HasCollaborativeData { get; set; }

    /// <summary>
    /// Top ratings (4+ sterren) van gelijkaardige gebruikers.
    /// NOTE: UserRating type removed, this property is kept for compatibility but will be empty.
    /// </summary>
    public List<object> TopRatings { get; set; } = new();
}

