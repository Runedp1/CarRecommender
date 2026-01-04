using System.Security.Cryptography;

namespace CarRecommender;

/// <summary>
/// Ranking service die gecombineerde scores berekent en controlled randomness toepast.
/// 
/// De ranking logica combineert:
/// 1. Match met harde voorkeuren (sportief = hoger vermogen, lager gewicht indien beschikbaar)
/// 2. Similarity score uit feature vectors (cosine similarity)
/// 3. Controlled randomness voor variatie bij gelijke scores
/// </summary>
public class RankingService
{
    /// <summary>
    /// Configureerbare gewichten voor ranking.
    /// Deze kunnen worden aangepast via configuratie of experimenten.
    /// </summary>
    public class RankingWeights
    {
        /// <summary>
        /// Gewicht voor similarity score (cosine similarity uit feature vectors).
        /// Standaard: 0.7 (70% van de score komt van similarity).
        /// </summary>
        public double SimilarityWeight { get; set; } = 0.7;

        /// <summary>
        /// Gewicht voor preference matching (sportief = hoger vermogen, etc.).
        /// Standaard: 0.2 (20% van de score komt van preference matching).
        /// </summary>
        public double PreferenceMatchWeight { get; set; } = 0.2;

        /// <summary>
        /// Gewicht voor controlled randomness.
        /// Standaard: 0.1 (10% van de score komt van randomness).
        /// </summary>
        public double RandomnessWeight { get; set; } = 0.1;

        /// <summary>
        /// Drempelwaarde voor controlled randomness.
        /// Als het verschil tussen scores kleiner is dan deze waarde, wordt randomness toegepast.
        /// Standaard: 0.05 (5% verschil).
        /// </summary>
        public double RandomnessThreshold { get; set; } = 0.05;
    }

    private readonly RankingWeights _weights;
    private readonly Random _random;

    public RankingService(RankingWeights? weights = null)
    {
        _weights = weights ?? new RankingWeights();
        _random = new Random();
    }

    /// <summary>
    /// Berekent een gecombineerde score voor een auto op basis van:
    /// - Similarity score (cosine similarity)
    /// - Preference matching (sportief/comfort, etc.)
    /// - Controlled randomness (alleen bij gelijke scores)
    /// </summary>
    public double CalculateCombinedScore(
        Car car,
        double similarityScore,
        UserPreferences? preferences = null)
    {
        // 1. Similarity score (0.0 - 1.0)
        double similarityComponent = similarityScore * _weights.SimilarityWeight;

        // 2. Preference matching score (0.0 - 1.0)
        double preferenceMatchScore = CalculatePreferenceMatchScore(car, preferences);
        double preferenceComponent = preferenceMatchScore * _weights.PreferenceMatchWeight;

        // 3. Base score zonder randomness
        double baseScore = similarityComponent + preferenceComponent;

        // 4. Controlled randomness (alleen als base score hoog genoeg is)
        // Randomness wordt alleen toegepast als de score al goed is (> 0.5)
        // Dit voorkomt dat slechte matches naar boven komen door randomness
        double randomnessComponent = 0.0;
        if (baseScore > 0.5)
        {
            // Kleine random variatie voor variatie bij gelijke scores
            randomnessComponent = (_random.NextDouble() * _weights.RandomnessWeight);
        }

        double finalScore = baseScore + randomnessComponent;

        // Clamp naar 0-1 bereik
        return Math.Max(0.0, Math.Min(1.0, finalScore));
    }

    /// <summary>
    /// Berekent preference matching score op basis van user preferences.
    /// Bijvoorbeeld: sportief = hoger vermogen krijgt bonus, comfort = lager vermogen krijgt bonus.
    /// </summary>
    private double CalculatePreferenceMatchScore(Car car, UserPreferences? preferences)
    {
        if (preferences == null)
            return 0.5; // Neutrale score als geen preferences

        double score = 0.5; // Start met neutrale score
        double totalWeight = 0.0;

        // Sportief vs Comfort matching
        if (preferences.ComfortVsSportScore < 0.5) // Sportief
        {
            // Sportief = hoger vermogen is beter
            // Bonus voor auto's met hoog vermogen
            double powerScore = car.Power > 0 ? Math.Min(1.0, car.Power / 300.0) : 0.5; // Normaliseer naar 0-1 (300 KW = max)
            double weight = preferences.PreferenceWeights.GetValueOrDefault("comfort", 0.5);
            score += (powerScore - 0.5) * weight;
            totalWeight += weight;
        }
        else // Comfort
        {
            // Comfort = lager vermogen is beter (zuiniger)
            // Bonus voor auto's met redelijk vermogen (niet te laag, niet te hoog)
            double powerScore = car.Power > 0 
                ? 1.0 - Math.Abs(car.Power - 120.0) / 200.0 // 120 KW is ideaal voor comfort
                : 0.5;
            powerScore = Math.Max(0.0, Math.Min(1.0, powerScore));
            double weight = preferences.PreferenceWeights.GetValueOrDefault("comfort", 0.5);
            score += (powerScore - 0.5) * weight;
            totalWeight += weight;
        }

        // Normaliseer score
        if (totalWeight > 0)
        {
            score = 0.5 + (score - 0.5) / totalWeight;
        }

        return Math.Max(0.0, Math.Min(1.0, score));
    }

    /// <summary>
    /// Sorteert recommendation results met controlled randomness.
    /// Auto's met bijna gelijke scores krijgen een kleine random variatie.
    /// </summary>
    public List<RecommendationResult> RankWithControlledRandomness(
        List<RecommendationResult> results,
        double similarityThreshold = 0.05)
    {
        // Groepeer resultaten op basis van score ranges
        var grouped = results
            .Select((r, index) => new { Result = r, Index = index })
            .GroupBy(x => Math.Floor(x.Result.SimilarityScore / similarityThreshold))
            .OrderByDescending(g => g.Key)
            .ToList();

        var ranked = new List<RecommendationResult>();

        foreach (var group in grouped)
        {
            var groupResults = group.Select(x => x.Result).ToList();
            
            // Als meerdere auto's in dezelfde score range zitten, shuffle ze
            if (groupResults.Count > 1)
            {
                // Fisher-Yates shuffle voor controlled randomness
                for (int i = groupResults.Count - 1; i > 0; i--)
                {
                    int j = _random.Next(i + 1);
                    var temp = groupResults[i];
                    groupResults[i] = groupResults[j];
                    groupResults[j] = temp;
                }
            }

            ranked.AddRange(groupResults);
        }

        return ranked;
    }
}












