namespace CarRecommender;

/// <summary>
/// Bouwt uitleg teksten voor recommendations op basis van echte data.
/// Gebruikt alleen informatie uit Car en UserPreferences.
/// </summary>
public class ExplanationBuilder
{
    /// <summary>
    /// Genereert een Nederlandstalige uitleg waarom een auto wordt aanbevolen.
    /// Toont gewichten voor transparantie over belangrijke voorkeuren.
    /// </summary>
    public string BuildExplanation(Car car, UserPreferences prefs, double similarityScore)
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
                reasons.Add($"{brandMatch} (belangrijkheid: {weightText})");
            }
        }

        if (prefs.PreferredFuel != null)
        {
            string fuelMatch = MatchFuel(car.Fuel, prefs.PreferredFuel);
            if (!string.IsNullOrEmpty(fuelMatch))
            {
                double fuelWeight = prefs.PreferenceWeights.GetValueOrDefault("fuel", 1.0);
                string weightText = GetWeightDescription(fuelWeight);
                reasons.Add($"{fuelMatch} (belangrijkheid: {weightText})");
            }
        }

        if (prefs.MaxBudget.HasValue && car.Budget > 0 && car.Budget <= (decimal)prefs.MaxBudget.Value)
        {
            double budgetWeight = prefs.PreferenceWeights.GetValueOrDefault("budget", 1.0);
            string weightText = GetWeightDescription(budgetWeight);
            reasons.Add($"blijft onder je budget van €{prefs.MaxBudget.Value:N0} (belangrijkheid: {weightText})");
        }

        // Power check - kan score zijn (0-1) of exacte KW
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
                reasons.Add($"heeft {car.Power} KW vermogen (past bij '{powerDesc}' - {weightText})");
            }
            else
            {
                // Exacte KW waarde
                powerMatches = car.Power >= prefs.MinPower.Value;
                if (powerMatches)
                {
                    reasons.Add($"heeft {car.Power} KW vermogen (minstens {prefs.MinPower.Value} KW - {weightText})");
                }
            }
        }
        else if (car.Power > 0)
        {
            reasons.Add($"heeft {car.Power} KW vermogen");
        }

        if (prefs.BodyTypePreference != null)
        {
            string bodyMatch = MatchBodyType(car.Model, prefs.BodyTypePreference);
            if (!string.IsNullOrEmpty(bodyMatch))
            {
                double bodyWeight = prefs.PreferenceWeights.GetValueOrDefault("bodytype", 1.0);
                string weightText = GetWeightDescription(bodyWeight);
                reasons.Add($"{bodyMatch} (belangrijkheid: {weightText})");
            }
        }

        if (prefs.AutomaticTransmission.HasValue)
        {
            double transWeight = prefs.PreferenceWeights.GetValueOrDefault("transmission", 1.0);
            string weightText = GetWeightDescription(transWeight);
            // Note: we hebben geen Transmission property in Car, dus voor nu alleen in uitleg als context
            string transText = prefs.AutomaticTransmission.Value ? "automaat" : "schakel";
            // reasons.Add($"heeft {transText} transmissie ({weightText})"); // Toekomstig
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

        explanation += $". Similarity score: {similarityScore:F3}";

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
    /// Check of model match met body type voorkeur.
    /// </summary>
    private string MatchBodyType(string model, string preferredBodyType)
    {
        string modelLower = model.ToLower();
        string prefLower = preferredBodyType.ToLower();

        // Simpele matching op model naam keywords
        if (prefLower == "suv" && (
            modelLower.Contains("x3") || modelLower.Contains("x5") || 
            modelLower.Contains("q5") || modelLower.Contains("q7") ||
            modelLower.Contains("tiguan") || modelLower.Contains("touareg")))
        {
            return "een SUV koetswerk heeft";
        }

        if (prefLower == "station" && (
            modelLower.Contains("touring") || modelLower.Contains("combi") ||
            modelLower.Contains("break") || modelLower.Contains("estate")))
        {
            return "een stationwagen koetswerk heeft";
        }

        return string.Empty;
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

