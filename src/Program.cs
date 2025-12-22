namespace CarRecommender;

/// <summary>
/// Entry point - laadt data, toont auto's en genereert recommendations.
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("CAR RECOMMENDATION SYSTEM");
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine();

        // Laad auto's uit CSV via repository (data wordt automatisch geladen bij initialisatie)
        ICarRepository repository = new CarRepository();
        List<Car> cars = repository.GetAllCars();

        Console.WriteLine($"\nAantal ingelezen auto's: {cars.Count}");

        if (cars.Count == 0)
        {
            Console.WriteLine("Geen auto's gevonden om weer te geven.");
            Console.WriteLine("\nDruk op een toets om af te sluiten...");
            Console.ReadKey();
            return;
        }

        // DEMO 1: Filter op budget en vermogen
        Console.WriteLine("\n" + new string('=', 100));
        Console.WriteLine("DEMO 1: FILTERING - Auto's tussen €15.000 en €30.000 met min. 100 KW");
        Console.WriteLine(new string('=', 100));

        List<Car> filteredCars = repository.FilterCars(
            minBudget: 15000, 
            maxBudget: 30000, 
            minPower: 100
        );

        Console.WriteLine($"\nGevonden: {filteredCars.Count} auto's");
        DisplayCars(filteredCars.Take(10).ToList(), "Voorbeelden (eerste 10):");

        // DEMO 2: Filter op merk
        Console.WriteLine("\n" + new string('=', 100));
        Console.WriteLine("DEMO 2: FILTERING - BMW auto's vanaf 2020");
        Console.WriteLine(new string('=', 100));

        List<Car> bmwCars = repository.FilterCars(brand: "BMW", minYear: 2020);
        Console.WriteLine($"\nGevonden: {bmwCars.Count} BMW auto's vanaf 2020");
        DisplayCars(bmwCars.Take(10).ToList(), "Voorbeelden (eerste 10):");

        // DEMO 3: Filter op brandstof
        Console.WriteLine("\n" + new string('=', 100));
        Console.WriteLine("DEMO 3: FILTERING - Elektrische auto's onder €40.000");
        Console.WriteLine(new string('=', 100));

        List<Car> electricCars = repository.FilterCars(maxBudget: 40000, fuel: "electric");
        Console.WriteLine($"\nGevonden: {electricCars.Count} elektrische auto's onder €40.000");
        DisplayCars(electricCars.Take(10).ToList(), "Voorbeelden (eerste 10):");

        // DEMO 4: Recommendations met gefilterde dataset
        Console.WriteLine("\n" + new string('=', 100));
        Console.WriteLine("DEMO 4: RECOMMENDATIONS met gefilterde auto's");
        Console.WriteLine(new string('=', 100));

        // Filter: auto's tussen €20.000 en €35.000
        List<Car> demoCars = repository.FilterCars(minBudget: 20000, maxBudget: 35000);
        
        ICarRepository carRepo = new CarRepository();
        IRecommendationService recommendationService = new RecommendationService(carRepo);

        if (demoCars.Count > 0)
        {
            Car targetCar = demoCars[0];
            Console.WriteLine($"\nTarget auto: {targetCar.Brand} {targetCar.Model} ({targetCar.Year})");
            Console.WriteLine($"  Vermogen: {targetCar.Power} KW | Brandstof: {targetCar.Fuel} | Budget: {targetCar.Budget:C2}");

            List<RecommendationResult> recommendations = recommendationService.RecommendSimilarCars(targetCar, 5);

            DisplayRecommendations(recommendations, $"Top 5 Aanbevelingen (uit {demoCars.Count} gefilterde auto's):");
        }

        // DEMO 5: Tekst-gebaseerde recommendations - Uitgebreide test met 10 verschillende input teksten
        Console.WriteLine("\n" + new string('=', 100));
        Console.WriteLine("DEMO 5: TEKST-GEBASEERDE RECOMMENDATIONS (NLP) - GEWICHTEN & INFORMele TAAL");
        Console.WriteLine(new string('=', 100));
        Console.WriteLine("Test cases tonen verschillende gradaties van belangrijkheid:");
        Console.WriteLine("  - Gewicht 1.5 (cruciaal/must-have): 'moet', 'cruciaal', 'absoluut'");
        Console.WriteLine("  - Gewicht 1.0 (belangrijk): 'belangrijk', 'wilt', 'nodig'");
        Console.WriteLine("  - Gewicht 0.6 (liever): 'liever', 'bij voorkeur', 'zou leuk zijn'");
        Console.WriteLine("  - Gewicht 0.3 (optioneel): contextueel geïmpliceerd");

        TextParserService textParser = new TextParserService();

        // Test cases met verschillende gradaties van belangrijkheid
        string[] testCases = new[]
        {
            // Voorbeelden uit de opdracht
            "Ik zou liever een automaat hebben met veel vermogen, max 25k euro",
            "Cruciaal dat hij zuinig is en elektrisch, budget tot 30k",
            "Comfortabele SUV voor familie, automaat belangrijk, rond de 20 mille",
            
            // Extra edge cases
            "Geen idee, alles kan",
            "Iets comfortabels voor lange ritten, max 35000",
            "Sportieve wagen, veel vermogen belangrijk, max 50000 euro",
            "Klein stadsautootje, zuinig, goedkoop, max 15000 euro",
            "Heb een SUV nodig met diesel motor, automaat cruciaal, budget 40000",
            "Wil een stationwagen, benzine, comfortabel voor gezin, max 25000 euro",
            "Geen automaat, liever benzine, budget tot 20000"
        };

        for (int i = 0; i < testCases.Length; i++)
        {
            Console.WriteLine($"\n\n{new string('=', 100)}");
            Console.WriteLine($"TEST CASE {i + 1}/10");
            Console.WriteLine(new string('=', 100));

            string testText = testCases[i];
            Console.WriteLine($"\nInput tekst: \"{testText}\"");

            // Parse preferences om te zien wat er wordt uitgehaald
            UserPreferences parsedPrefs = textParser.ParsePreferencesFromText(testText);
            Console.WriteLine("\n--- Geëxtraheerde Preferences (met gewichten) ---");
            Console.WriteLine($"  MaxBudget: {(parsedPrefs.MaxBudget.HasValue ? $"€{parsedPrefs.MaxBudget.Value:N0}" : "geen")} " +
                $"{(parsedPrefs.PreferenceWeights.ContainsKey("budget") ? $"[gewicht: {parsedPrefs.PreferenceWeights["budget"]:F1}]" : "")}");
            Console.WriteLine($"  PreferredFuel: {parsedPrefs.PreferredFuel ?? "geen"} " +
                $"{(parsedPrefs.PreferenceWeights.ContainsKey("fuel") ? $"[gewicht: {parsedPrefs.PreferenceWeights["fuel"]:F1}]" : "")}");
            Console.WriteLine($"  AutomaticTransmission: {(parsedPrefs.AutomaticTransmission.HasValue ? (parsedPrefs.AutomaticTransmission.Value ? "automaat" : "schakel") : "geen voorkeur")} " +
                $"{(parsedPrefs.PreferenceWeights.ContainsKey("transmission") ? $"[gewicht: {parsedPrefs.PreferenceWeights["transmission"]:F1}]" : "")}");
            Console.WriteLine($"  MinPower: {(parsedPrefs.MinPower.HasValue ? (parsedPrefs.MinPower.Value <= 1.0 ? $"{parsedPrefs.MinPower.Value:F2} (score)" : $"{parsedPrefs.MinPower.Value:F0} KW") : "geen")} " +
                $"{(parsedPrefs.PreferenceWeights.ContainsKey("power") ? $"[gewicht: {parsedPrefs.PreferenceWeights["power"]:F1}]" : "")}");
            Console.WriteLine($"  BodyTypePreference: {parsedPrefs.BodyTypePreference ?? "geen"} " +
                $"{(parsedPrefs.PreferenceWeights.ContainsKey("bodytype") ? $"[gewicht: {parsedPrefs.PreferenceWeights["bodytype"]:F1}]" : "")}");
            Console.WriteLine($"  ComfortVsSportScore: {parsedPrefs.ComfortVsSportScore:F2} (0=sportief, 1=comfort) " +
                $"{(parsedPrefs.PreferenceWeights.ContainsKey("comfort") ? $"[gewicht: {parsedPrefs.PreferenceWeights["comfort"]:F1}]" : "")}");

            // Genereer recommendations
            List<RecommendationResult> recommendations = recommendationService.RecommendFromText(testText, 3);

            Console.WriteLine($"\n--- Top 3 Recommendations ---");
            if (recommendations.Count == 0)
            {
                Console.WriteLine("  Geen recommendations gevonden!");
            }
            else
            {
                for (int j = 0; j < recommendations.Count; j++)
                {
                    var rec = recommendations[j];
                    Car car = rec.Car;
                    Console.WriteLine($"\n  {j + 1}. {car.Brand} {car.Model} ({car.Year})");
                    Console.WriteLine($"     €{car.Budget:N0} | {car.Power} KW | {car.Fuel} | Score: {rec.SimilarityScore:F3}");
                    Console.WriteLine($"     Uitleg: {rec.Explanation}");
                }
            }
        }

        Console.WriteLine("\n\nDruk op een toets om af te sluiten...");
        Console.ReadKey();
    }

    /// <summary>
    /// Print auto's in tabel formaat.
    /// </summary>
    private static void DisplayCars(List<Car> cars, string title)
    {
        Console.WriteLine($"\n{title}");
        Console.WriteLine(new string('-', 100));
        Console.WriteLine($"{"Merk",-15} {"Model",-20} {"Vermogen",-10} {"Brandstof",-12} {"Budget",-15} {"Bouwjaar",-10}");
        Console.WriteLine(new string('-', 100));

        foreach (Car car in cars)
        {
            Console.WriteLine($"{car.Brand,-15} {car.Model,-20} {car.Power,-10} {car.Fuel,-12} {car.Budget:C2,-15} {car.Year,-10}");
        }
    }

    /// <summary>
    /// Print recommendations met similarity scores.
    /// </summary>
    private static void DisplayRecommendations(List<RecommendationResult> recommendations, string title)
    {
        Console.WriteLine($"\n{title}");
        Console.WriteLine(new string('-', 150));
        Console.WriteLine($"{"Merk",-15} {"Model",-20} {"Vermogen",-10} {"Brandstof",-12} {"Budget",-15} {"Bouwjaar",-10} {"Similarity",-12} {"Image Path",-40}");
        Console.WriteLine(new string('-', 150));

        if (recommendations.Count == 0)
        {
            Console.WriteLine("Geen recommendations gevonden.");
            return;
        }

        foreach (var recommendation in recommendations)
        {
            Car car = recommendation.Car;
            Console.WriteLine($"{car.Brand,-15} {car.Model,-20} {car.Power,-10} {car.Fuel,-12} {car.Budget:C2,-15} {car.Year,-10} {recommendation.SimilarityScore:F4} {car.ImagePath,-40}");
        }
    }

    /// <summary>
    /// Print tekst-gebaseerde recommendations met explanations.
    /// </summary>
    private static void DisplayTextRecommendations(List<RecommendationResult> recommendations, string title)
    {
        Console.WriteLine($"\n{title}");
        Console.WriteLine(new string('=', 120));

        if (recommendations.Count == 0)
        {
            Console.WriteLine("Geen recommendations gevonden op basis van je input.");
            return;
        }

        for (int i = 0; i < recommendations.Count; i++)
        {
            var rec = recommendations[i];
            Car car = rec.Car;

            Console.WriteLine($"\n{i + 1}. {car.Brand} {car.Model} ({car.Year})");
            Console.WriteLine($"   Vermogen: {car.Power} KW | Brandstof: {car.Fuel} | Budget: {car.Budget:C0} | Similarity: {rec.SimilarityScore:F3}");
            Console.WriteLine($"   Uitleg: {rec.Explanation}");
            Console.WriteLine(new string('-', 120));
        }
    }
}



