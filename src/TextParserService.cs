using System.Text.RegularExpressions;

namespace CarRecommender;

/// <summary>
/// Parse gebruikersinput tekst naar UserPreferences met gewichten.
/// Herkent informele Nederlandse taal en gradaties van belangrijkheid.
/// </summary>
public class TextParserService
{
    /// <summary>
    /// Parse tekst input naar UserPreferences met gewichten.
    /// Herkent gradaties van belangrijkheid en informele omschrijvingen.
    /// </summary>
    public UserPreferences ParsePreferencesFromText(string inputText)
    {
        UserPreferences prefs = new UserPreferences();
        string lowerText = inputText.ToLower();

        // Parse elke voorkeur met bijbehorende gewichten
        ExtractBudget(lowerText, prefs);
        ExtractFuelPreference(lowerText, prefs);
        ExtractBrandPreference(lowerText, prefs);
        ExtractTransmissionPreference(lowerText, prefs);
        ExtractPowerPreference(lowerText, prefs);
        ExtractBodyTypePreference(lowerText, prefs);
        ExtractComfortPreference(lowerText, prefs);

        return prefs;
    }

    /// <summary>
    /// Bepaalt gewicht op basis van belangrijkheidsmarkers in de tekst.
    /// Returns: 1.5 (cruciaal), 1.0 (belangrijk), 0.6 (liever), 0.3 (optioneel)
    /// </summary>
    private double DetectImportanceWeight(string text, string keywordContext)
    {
        // Check context rond keyword voor belangrijkheidsmarkers
        
        // MUST-HAVE (gewicht 1.5): "moet", "absoluut", "cruciaal", "geen" (als negatie)
        var mustHaveMarkers = new[] { "moet", "absoluut", "cruciaal", "essentieel", "verplicht", "noodzakelijk" };
        if (mustHaveMarkers.Any(m => text.Contains(m)))
        {
            return 1.5;
        }

        // BELANGRIJK (gewicht 1.0): "belangrijk", "wilt", "gaarne", "nodig", "nodig heb"
        var importantMarkers = new[] { "belangrijk", "wilt", "wil", "gaarne", "nodig", "nodig heb", "nodig hebben" };
        if (importantMarkers.Any(m => text.Contains(m)))
        {
            return 1.0;
        }

        // LIEVER (gewicht 0.6): "liever", "bij voorkeur", "zou leuk zijn", "graag"
        var preferMarkers = new[] { "liever", "bij voorkeur", "zou leuk zijn", "zou fijn zijn", "graag", "prefer" };
        if (preferMarkers.Any(m => text.Contains(m)))
        {
            return 0.6;
        }

        // OPTIONEEL (gewicht 0.3): contextueel geïmpliceerd, of als er geen marker is
        return 0.3;
    }

    /// <summary>
    /// Extraheert budget met gewicht op basis van context.
    /// Herkent: "max 20k", "rond de 25.000", "niet meer dan €18k", "budget tot 30k"
    /// </summary>
    private void ExtractBudget(string text, UserPreferences prefs)
    {
        // Zoek naar budget patronen met context voor gewicht
        var patterns = new[]
        {
            @"(?:max|maximum|maximaal|tot|max\.)\s*[^\d]*(\d+(?:[.,]\d+)?)\s*(?:[km]|duizend|\.000|mille)?",
            @"(?:rond|ongeveer|circa|zo'n|zowat)\s*(?:de|het)?\s*(\d+(?:[.,]\d+)?)\s*(?:[km]|duizend|\.000|mille|euro)?",
            @"budget[^\d]*(\d+(?:[.,]\d+)?)\s*(?:[km]|duizend|\.000)?",
            @"(?:niet\s+meer\s+dan|maximaal|max\.)\s*[€]?\s*(\d+(?:[.,]\d+)?)\s*(?:[km]|duizend|\.000)?",
            @"(\d+(?:[.,]\d+)?)\s*(?:[km]|duizend|\.000|mille)?\s*(?:euro|€|budget)"
        };

        double maxBudget = 0;
        string matchedContext = "";

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (match.Success && match.Groups.Count > 1)
            {
                string numberStr = match.Groups[1].Value.Replace(",", ".");
                if (double.TryParse(numberStr, out double budget))
                {
                    // "mille" = 1000, "k" = 1000
                    if (text.Contains("duizend") || text.Contains(" k") || text.Contains("k "))
                    {
                        budget *= 1000;
                    }
                    // Als getal klein (< 100), vermenigvuldig met 1000 (bijv. "20" = 20.000)
                    else if (budget < 100)
                    {
                        budget *= 1000;
                    }
                    maxBudget = budget;
                    matchedContext = match.Value;
                    break;
                }
            }
        }

        if (maxBudget > 0)
        {
            prefs.MaxBudget = maxBudget;
            double weight = DetectImportanceWeight(text, matchedContext);
            prefs.PreferenceWeights["budget"] = weight;
        }
    }

    /// <summary>
    /// Extraheert merk voorkeur met gewicht.
    /// Herkent veelvoorkomende automerken in Nederlandse en Engelse tekst.
    /// </summary>
    private void ExtractBrandPreference(string text, UserPreferences prefs)
    {
        // Mapping van Nederlandse/Engelse merknamen naar genormaliseerde merknamen
        var brandMappings = new Dictionary<string, string[]>
        {
            { "bmw", new[] { "bmw" } },
            { "audi", new[] { "audi" } },
            { "mercedes-benz", new[] { "mercedes", "mercedes-benz", "mercedes benz", "benz" } },
            { "volkswagen", new[] { "volkswagen", "vw", "volks" } },
            { "ford", new[] { "ford" } },
            { "opel", new[] { "opel" } },
            { "peugeot", new[] { "peugeot" } },
            { "citroen", new[] { "citroen", "citroën" } },
            { "renault", new[] { "renault" } },
            { "toyota", new[] { "toyota" } },
            { "honda", new[] { "honda" } },
            { "nissan", new[] { "nissan" } },
            { "mazda", new[] { "mazda" } },
            { "volvo", new[] { "volvo" } },
            { "skoda", new[] { "skoda", "škoda" } },
            { "seat", new[] { "seat" } },
            { "fiat", new[] { "fiat" } },
            { "alfa romeo", new[] { "alfa romeo", "alfaromeo", "alfa" } },
            { "jaguar", new[] { "jaguar" } },
            { "land rover", new[] { "land rover", "landrover" } },
            { "mini", new[] { "mini" } },
            { "porsche", new[] { "porsche" } },
            { "tesla", new[] { "tesla" } },
            { "hyundai", new[] { "hyundai" } },
            { "kia", new[] { "kia" } },
            { "lexus", new[] { "lexus" } },
            { "dacia", new[] { "dacia" } },
            { "suzuki", new[] { "suzuki" } },
            { "mitsubishi", new[] { "mitsubishi" } },
            { "subaru", new[] { "subaru" } }
        };

        string? matchedBrand = null;
        double weight = 1.0;

        // Check op negaties eerst
        bool isNegative = ContainsKeywords(text, new[] { "geen", "niet", "absoluut geen", "liever niet" });

        // Zoek naar merken in de tekst
        foreach (var brandMapping in brandMappings)
        {
            string normalizedBrand = brandMapping.Key;
            string[] keywords = brandMapping.Value;

            if (ContainsKeywords(text, keywords))
            {
                matchedBrand = normalizedBrand;
                
                // Als negatie, verhoog gewicht (must-not)
                if (isNegative)
                {
                    weight = 1.5;
                }
                else
                {
                    // Detecteer gewicht uit context
                    weight = DetectImportanceWeight(text, keywords[0]);
                }
                break; // Neem eerste match
            }
        }

        if (matchedBrand != null)
        {
            prefs.PreferredBrand = matchedBrand;
            prefs.PreferenceWeights["brand"] = weight;
        }
    }

    /// <summary>
    /// Extraheert brandstof voorkeur met gewicht.
    /// Herkent: "benzine", "geen diesel" (must-not), "hybride zou leuk zijn" (liever)
    /// </summary>
    private void ExtractFuelPreference(string text, UserPreferences prefs)
    {
        string? fuel = null;
        double weight = 1.0; // Standaard belangrijk
        
        // Check op negaties eerst (must-not)
        bool isNegative = ContainsKeywords(text, new[] { "geen", "niet", "absoluut geen", "liever niet" });
        
        // Prioriteit: electric > hybrid > diesel > petrol
        if (ContainsKeywords(text, new[] { "elektrisch", "electric", "ev", "e-auto" }))
        {
            fuel = "electric";
        }
        else if (ContainsKeywords(text, new[] { "hybride", "hybrid" }))
        {
            fuel = "hybrid";
        }
        else if (ContainsKeywords(text, new[] { "diesel" }))
        {
            fuel = "diesel";
        }
        else if (ContainsKeywords(text, new[] { "benzine", "petrol", "gasoline", "benzinemotor", "benzine motor" }))
        {
            fuel = "petrol";
        }

        if (fuel != null)
        {
            prefs.PreferredFuel = fuel;
            
            // Als negatie, verhoog gewicht (must-not)
            if (isNegative)
            {
                weight = 1.5;
            }
            else
            {
                // Detecteer gewicht uit context
                weight = DetectImportanceWeight(text, fuel);
            }
            
            prefs.PreferenceWeights["fuel"] = weight;
        }
    }

    /// <summary>
    /// Extraheert transmissie voorkeur met gewicht.
    /// Herkent: "automaat", "handbak mag ook" (liever), "geen automaat" (must-not)
    /// </summary>
    private void ExtractTransmissionPreference(string text, UserPreferences prefs)
    {
        bool? transmission = null;
        double weight = 1.0;

        // Check op negaties
        bool isNegative = ContainsKeywords(text, new[] { "geen automaat", "geen automatisch" });

        if (ContainsKeywords(text, new[] { "automaat", "automatic", "automatisch" }))
        {
            transmission = true;
            if (isNegative)
            {
                transmission = false; // "geen automaat" = schakel
                weight = 1.5;
            }
            else
            {
                weight = DetectImportanceWeight(text, "automaat");
            }
        }
        else if (ContainsKeywords(text, new[] { "schakel", "manual", "handgeschakeld", "handbak" }))
        {
            transmission = false;
            weight = DetectImportanceWeight(text, "schakel");
        }

        if (transmission.HasValue)
        {
            prefs.AutomaticTransmission = transmission;
            prefs.PreferenceWeights["transmission"] = weight;
        }
    }

    /// <summary>
    /// Extraheert vermogen voorkeur met gewicht.
    /// Herkent informele omschrijvingen: "veel vermogen" (0.8), "sterke motor" (0.8), 
    /// "voldoende voor snelweg" (0.5), of exacte waarden "200 KW", "minstens 150 KW"
    /// </summary>
    private void ExtractPowerPreference(string text, UserPreferences prefs)
    {
        double? powerValue = null;
        double weight = 1.0;

        // Eerst checken op expliciete vermogen eisen (exacte getallen)
        powerValue = ExtractExplicitPower(text);

        // Als geen expliciet vermogen, check op informele omschrijvingen
        if (!powerValue.HasValue)
        {
            // "Veel vermogen" / "veel pk" / "sterke motor" / "krachtig" → 0.8 (hoog)
            if (ContainsKeywords(text, new[] { "veel vermogen", "veel pk", "sterke motor", "krachtig", "sterk", "veel power" }))
            {
                powerValue = 0.8;
                weight = DetectImportanceWeight(text, "vermogen");
            }
            // "Voldoende voor snelweg" / "genoeg vermogen" → 0.5 (medium)
            else if (ContainsKeywords(text, new[] { "voldoende", "genoeg", "voor snelweg", "snelweg" }))
            {
                powerValue = 0.5;
                weight = DetectImportanceWeight(text, "vermogen");
            }
            // Sportieve keywords → 0.8
            else if (ContainsKeywords(text, new[] { "sportief", "sporty", "snel", "prestaties", "performance", "sportwagen" }))
            {
                powerValue = 0.8;
                weight = DetectImportanceWeight(text, "sportief");
            }
        }
        else
        {
            // Expliciet vermogen heeft standaard hoog gewicht
            weight = 1.0;
        }

        if (powerValue.HasValue)
        {
            prefs.MinPower = powerValue.Value;
            prefs.PreferenceWeights["power"] = weight;
        }
    }

    /// <summary>
    /// Extraheert expliciet vermogen uit tekst (exacte getallen).
    /// </summary>
    private double? ExtractExplicitPower(string text)
    {
        var patterns = new[]
        {
            @"(?:minstens|min|min\.|tenminste)\s*(\d+)\s*(?:kw|pk|kW)",
            @"(\d+)\s*(?:kw|pk|kW)\s*(?:minstens|min|of\s*meer)?",
            @"(\d+)\s*(?:pk)\s*(?:vermogen|power)?"
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (match.Success && match.Groups.Count > 1)
            {
                if (int.TryParse(match.Groups[1].Value, out int power))
                {
                    // Als PK (> 200), converteer naar KW
                    if (power > 200)
                    {
                        power = (int)(power * 0.736);
                    }
                    // Als > 100, is het waarschijnlijk KW (exacte waarde)
                    // Anders is het een score (0.0-1.0)
                    if (power <= 100)
                    {
                        return power / 100.0; // Normaliseer naar 0.0-1.0
                    }
                    return power; // Exacte KW waarde
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Extraheert body type voorkeur met gewicht.
    /// </summary>
    private void ExtractBodyTypePreference(string text, UserPreferences prefs)
    {
        string? bodyType = null;
        double weight = 1.0;

        if (ContainsKeywords(text, new[] { "suv", "jeep", "terreinwagen", "4x4" }))
        {
            bodyType = "suv";
        }
        else if (ContainsKeywords(text, new[] { "break", "station", "stationwagen", "combi", "stationwagon" }))
        {
            bodyType = "station";
        }
        else if (ContainsKeywords(text, new[] { "sedan", "berline", "limousine", "limo" }))
        {
            bodyType = "sedan";
        }
        else if (ContainsKeywords(text, new[] { "hatchback", "stadsauto", "klein", "compact", "stadsmobiel" }))
        {
            bodyType = "hatchback";
        }
        else if (ContainsKeywords(text, new[] { "cabrio", "convertible", "open" }))
        {
            bodyType = "cabrio";
        }

        if (bodyType != null)
        {
            prefs.BodyTypePreference = bodyType;
            weight = DetectImportanceWeight(text, bodyType);
            prefs.PreferenceWeights["bodytype"] = weight;
        }
    }

    /// <summary>
    /// Extraheert comfort/stijl voorkeur met gewicht.
    /// Update ComfortVsSportScore op basis van keywords.
    /// </summary>
    private void ExtractComfortPreference(string text, UserPreferences prefs)
    {
        // Sportieve keywords
        bool hasSportKeywords = ContainsKeywords(text, new[] 
        { 
            "sportief", "sporty", "snel", "krachtig", "prestaties", 
            "performance", "sportwagen", "prestatie"
        });

        // Comfort keywords
        bool hasComfortKeywords = ContainsKeywords(text, new[] 
        { 
            "comfortabel", "comfort", "rustig", "zuinig", "economisch", 
            "verbruik", "efficiënt", "relax", "ontspannen", "iets comfortabels"
        });

        // Lange ritten → comfort
        if (ContainsKeywords(text, new[] { "lange ritten", "lange afstanden", "lange reis", "lang rijden" }))
        {
            hasComfortKeywords = true;
        }

        if (hasSportKeywords)
        {
            prefs.ComfortVsSportScore = 0.2; // Sportief
            if (!prefs.PreferenceWeights.ContainsKey("comfort"))
            {
                prefs.PreferenceWeights["comfort"] = DetectImportanceWeight(text, "sportief");
            }
        }
        else if (hasComfortKeywords)
        {
            prefs.ComfortVsSportScore = 0.8; // Comfort
            prefs.PreferenceWeights["comfort"] = DetectImportanceWeight(text, "comfortabel");
        }
        else
        {
            prefs.ComfortVsSportScore = 0.5; // Neutraal
        }
    }

    /// <summary>
    /// Helper: check of tekst één van de keywords bevat.
    /// </summary>
    private bool ContainsKeywords(string text, string[] keywords)
    {
        return keywords.Any(keyword => text.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }
}

