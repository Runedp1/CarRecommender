namespace CarRecommender;

/// <summary>
/// Berekent hoe vergelijkbaar auto's zijn (similarity scores).
/// Gebruikt geen ML libraries, alleen simpele wiskunde.
/// </summary>
public class RecommendationEngine
{
    private static string GetLogPath()
    {
        var possiblePaths = new[]
        {
            @".cursor\debug.log",
            Path.Combine(Directory.GetCurrentDirectory(), ".cursor", "debug.log"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".cursor", "debug.log"),
            @"c:\Users\runed\OneDrive - Thomas More\Recommendation_System_New\.cursor\debug.log"
        };
        foreach (var path in possiblePaths)
        {
            try
            {
                var dir = Path.GetDirectoryName(path);
                if (dir != null && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                return Path.GetFullPath(path);
            }
            catch { }
        }
        return possiblePaths[0];
    }
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
        // #region agent log
        var logPath = GetLogPath();
        try { System.IO.File.AppendAllText(logPath, System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "B", location = "RecommendationEngine.cs:50", message = "CalculateSimilarity Entry", data = new { car1Id = car1?.Id, car2Id = car2?.Id, minPower, maxPower, minBudget = (double)minBudget, maxBudget = (double)maxBudget, minYear, maxYear, weightPower, weightBudget, weightYear, weightFuel }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
        // #endregion
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
        // #region agent log
        try { System.IO.File.AppendAllText(logPath, System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "A", location = "RecommendationEngine.cs:104", message = "Before Fuel Trim", data = new { car1FuelNull = car1.Fuel == null, car2FuelNull = car2.Fuel == null, car1FuelValue = car1.Fuel ?? "(null)", car2FuelValue = car2.Fuel ?? "(null)" }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
        // #endregion
        string fuel1 = car1.Fuel?.Trim().ToLower() ?? string.Empty;
        string fuel2 = car2.Fuel?.Trim().ToLower() ?? string.Empty;
        // #region agent log
        try { System.IO.File.AppendAllText(logPath, System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "A", location = "RecommendationEngine.cs:108", message = "After Fuel Trim", data = new { fuel1Empty = string.IsNullOrEmpty(fuel1), fuel2Empty = string.IsNullOrEmpty(fuel2), fuel1Value = fuel1, fuel2Value = fuel2 }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
        // #endregion
        
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
        // #region agent log
        try { System.IO.File.AppendAllText(logPath, System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "C", location = "RecommendationEngine.cs:124", message = "Before Final Score", data = new { powerSimilarity, budgetSimilarity, yearSimilarity, fuelSimilarity, weightPower, weightBudget, weightYear, weightFuel, totalSimilarity, isNaN = double.IsNaN(totalSimilarity), isInfinity = double.IsInfinity(totalSimilarity) }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
        // #endregion
        var finalScore = Math.Max(0.0, Math.Min(1.0, totalSimilarity)); // Zorg dat score tussen 0 en 1 is
        // #region agent log
        try { System.IO.File.AppendAllText(logPath, System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "C", location = "RecommendationEngine.cs:128", message = "CalculateSimilarity Exit", data = new { finalScore, car1Id = car1?.Id, car2Id = car2?.Id }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
        // #endregion
        return finalScore;
    }

    /// <summary>
    /// Zet een waarde om naar 0-1 bereik (min-max normalisatie).
    /// </summary>
    public double NormalizeValue(double value, double min, double max)
    {
        // #region agent log
        var logPathNorm = GetLogPath();
        try { System.IO.File.AppendAllText(logPathNorm, System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "D", location = "RecommendationEngine.cs:166", message = "NormalizeValue", data = new { value, min, max, maxLessEqualMin = max <= min }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
        // #endregion
        if (max <= min)
            return 0.0; // Voorkom delen door nul fout

        // Min-max normalisatie: (value - min) / (max - min)
        var normalized = (value - min) / (max - min);
        // #region agent log
        try { System.IO.File.AppendAllText(logPathNorm, System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "D", location = "RecommendationEngine.cs:172", message = "NormalizeValue Result", data = new { normalized, isNaN = double.IsNaN(normalized), isInfinity = double.IsInfinity(normalized) }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
        // #endregion
        return normalized;
    }
}


