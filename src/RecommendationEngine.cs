namespace CarRecommender;

/// <summary>
/// Berekent hoe vergelijkbaar auto's zijn (similarity scores).
/// Gebruikt geen ML libraries, alleen simpele wiskunde.
/// </summary>
public class RecommendationEngine
{
    /// <summary>
    /// Berekent hoe vergelijkbaar twee auto's zijn (0-1 score).
    /// Vergelijkt vermogen, prijs, bouwjaar en brandstof.
    /// </summary>
    public double CalculateSimilarity(
        Car car1, 
        Car car2, 
        int minPower, int maxPower,
        decimal minBudget, decimal maxBudget,
        int minYear, int maxYear)
    {
        return CalculateSimilarity(car1, car2, minPower, maxPower, minBudget, maxBudget, minYear, maxYear, 0.25, 0.30, 0.20, 0.25);
    }

    /// <summary>
    /// Berekent similarity met custom gewichten (voor tekst-gebaseerde recommendations).
    /// </summary>
    public double CalculateSimilarity(
        Car car1, 
        Car car2, 
        int minPower, int maxPower,
        decimal minBudget, decimal maxBudget,
        int minYear, int maxYear,
        double weightPower, double weightBudget, double weightYear, double weightFuel)
    {
        // Null checks
        if (car1 == null || car2 == null)
            return 0.0;
        // Normaliseer gewichten (zorg dat ze optellen tot 1.0)
        double totalWeight = weightPower + weightBudget + weightYear + weightFuel;
        if (totalWeight > 0)
        {
            weightPower /= totalWeight;
            weightBudget /= totalWeight;
            weightYear /= totalWeight;
            weightFuel /= totalWeight;
        }

        double powerSimilarity = 0.0;
        double budgetSimilarity = 0.0;
        double yearSimilarity = 0.0;
        double fuelSimilarity = 0.0;

        // Vermogen vergelijken
        if (car1.Power > 0 && car2.Power > 0 && maxPower > minPower)
        {
            // Normaliseer naar 0-1 en bereken verschil
            double normPower1 = NormalizeValue(car1.Power, minPower, maxPower);
            double normPower2 = NormalizeValue(car2.Power, minPower, maxPower);
            double powerDistance = Math.Abs(normPower1 - normPower2);
            
            // Verschil omzetten naar similarity (1 = zelfde, 0 = totaal anders)
            powerSimilarity = 1.0 - powerDistance;
        }

        // 2. Budget similarity (genormaliseerde afstand)
        if (car1.Budget > 0 && car2.Budget > 0 && maxBudget > minBudget)
        {
            double normBudget1 = NormalizeValue((double)car1.Budget, (double)minBudget, (double)maxBudget);
            double normBudget2 = NormalizeValue((double)car2.Budget, (double)minBudget, (double)maxBudget);
            
            double budgetDistance = Math.Abs(normBudget1 - normBudget2);
            budgetSimilarity = 1.0 - budgetDistance;
        }

        // Bouwjaar vergelijken
        if (car1.Year > 1900 && car2.Year > 1900 && maxYear > minYear)
        {
            double normYear1 = NormalizeValue(car1.Year, minYear, maxYear);
            double normYear2 = NormalizeValue(car2.Year, minYear, maxYear);
            
            double yearDistance = Math.Abs(normYear1 - normYear2);
            yearSimilarity = 1.0 - yearDistance;
        }

        // Brandstof vergelijken
        string fuel1 = car1.Fuel?.Trim().ToLower() ?? string.Empty;
        string fuel2 = car2.Fuel?.Trim().ToLower() ?? string.Empty;

        
        if (!string.IsNullOrEmpty(fuel1) && !string.IsNullOrEmpty(fuel2))
        {
            if (fuel1 == fuel2)
            {
                fuelSimilarity = 1.0; // Exact hetzelfde
            }
            else
            {
                // Gedeeltelijke match voor varianten (bijv. "hybrid" en "plug in hybrid")
                if ((fuel1.Contains("hybrid") && fuel2.Contains("hybrid")) ||
                    (fuel1.Contains("electric") && fuel2.Contains("electric")) ||
                    (fuel1.Contains("petrol") && fuel2.Contains("petrol")) ||
                    (fuel1.Contains("diesel") && fuel2.Contains("diesel")))
                {
                    fuelSimilarity = 0.5; // Soortgelijk
                }
                else
                {
                    fuelSimilarity = 0.0; // Totaal anders
                }
            }
        }

        // Combineer alle scores met gewichten
        double totalSimilarity = (powerSimilarity * weightPower) +
                                 (budgetSimilarity * weightBudget) +
                                 (yearSimilarity * weightYear) +
                                 (fuelSimilarity * weightFuel);

        var finalScore = Math.Max(0.0, Math.Min(1.0, totalSimilarity)); // Zorg dat score tussen 0 en 1 is
        return finalScore;
    }

    /// <summary>
    /// Zet een waarde om naar 0-1 bereik (min-max normalisatie).
    /// </summary>
    public double NormalizeValue(double value, double min, double max)
    {
        if (max <= min)
            return 0.0; // Voorkom delen door nul fout

        // Min-max normalisatie: (value - min) / (max - min)
        var normalized = (value - min) / (max - min);
        return normalized;
    }
}


