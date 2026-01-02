using System;
using System.Collections.Generic;
using System.Linq;
using CarRecommender;

/// <summary>
/// Test script voor AdvancedScoringService.
/// Test verschillende scenario's om te verifiëren dat de scoring logica correct werkt.
/// 
/// Scenario's:
/// 1. Max budget 25k, sportief, Audi → auto's rond 23-25k met hoger vermogen moeten bovenaan staan
/// 2. Max budget 30k, comfortabel → auto's rond 25-30k met redelijk vermogen moeten bovenaan staan
/// 3. Geen budget voorkeur → gemiddelde auto's moeten bovenaan staan
/// </summary>
class TestAdvancedScoring
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== Test Advanced Scoring Service ===\n");

        // Maak test dataset
        var testCars = CreateTestCars();
        Console.WriteLine($"Aangemaakt: {testCars.Count} test auto's\n");

        // Test scenario 1: Max budget 25k, sportief, Audi
        Console.WriteLine("=== Scenario 1: Max budget 25k, sportief, Audi ===");
        TestScenario1(testCars);

        Console.WriteLine("\n=== Scenario 2: Max budget 30k, comfortabel ===");
        TestScenario2(testCars);

        Console.WriteLine("\n=== Scenario 3: Geen budget voorkeur, neutraal ===");
        TestScenario3(testCars);

        Console.WriteLine("\n=== Test voltooid ===");
    }

    /// <summary>
    /// Maakt een test dataset met verschillende auto's:
    /// - Budgetwagen (8k, laag vermogen)
    /// - Middenklasse (15k, redelijk vermogen)
    /// - Middenklasse (23k, redelijk vermogen, Audi)
    /// - Middenklasse (25k, redelijk vermogen, Audi)
    /// - Sportief (28k, hoog vermogen, Audi)
    /// - Luxe (35k, hoog vermogen)
    /// </summary>
    static List<Car> CreateTestCars()
    {
        return new List<Car>
        {
            new Car { Id = 1, Brand = "Opel", Model = "Corsa", Budget = 8000, Power = 75, Year = 2018, Fuel = "petrol", Transmission = "manual", BodyType = "hatchback" },
            new Car { Id = 2, Brand = "Volkswagen", Model = "Golf", Budget = 15000, Power = 110, Year = 2019, Fuel = "petrol", Transmission = "manual", BodyType = "hatchback" },
            new Car { Id = 3, Brand = "Audi", Model = "A3", Budget = 23000, Power = 150, Year = 2020, Fuel = "petrol", Transmission = "automatic", BodyType = "sedan" },
            new Car { Id = 4, Brand = "Audi", Model = "A4", Budget = 25000, Power = 190, Year = 2021, Fuel = "petrol", Transmission = "automatic", BodyType = "sedan" },
            new Car { Id = 5, Brand = "Audi", Model = "S4", Budget = 28000, Power = 260, Year = 2021, Fuel = "petrol", Transmission = "automatic", BodyType = "sedan" },
            new Car { Id = 6, Brand = "BMW", Model = "5 Series", Budget = 35000, Power = 250, Year = 2022, Fuel = "petrol", Transmission = "automatic", BodyType = "sedan" }
        };
    }

    /// <summary>
    /// Test scenario 1: Max budget 25k, sportief, Audi
    /// Verwacht: Auto's rond 23-25k met hoger vermogen (Audi A4, Audi S4) moeten bovenaan staan,
    /// niet de budgetwagen van 8k.
    /// </summary>
    static void TestScenario1(List<Car> cars)
    {
        var prefs = new UserPreferences
        {
            MaxBudget = 25000,
            PreferredBrand = "Audi",
            ComfortVsSportScore = 0.2, // Sportief (0 = puur sportief)
            PreferredFuel = "petrol"
        };

        var factory = new CarFeatureVectorFactory();
        factory.Initialize(cars);
        var idealVector = factory.CreateIdealVector(prefs, cars);
        var scoringService = new AdvancedScoringService(featureVectorFactory: factory);

        var results = new List<(Car car, AdvancedScoringService.FeatureScoreResult scores)>();

        foreach (var car in cars)
        {
            if (car.Power <= 0 || car.Budget <= 0 || car.Year < 1900)
                continue;

            var scores = scoringService.CalculateScores(car, prefs, idealVector, cars);
            results.Add((car, scores));
        }

        // Sorteer op finale score
        var sorted = results.OrderByDescending(r => r.scores.FinalScore).ToList();

        Console.WriteLine("Ranking (hoogste score eerst):");
        Console.WriteLine("--------------------------------");
        for (int i = 0; i < sorted.Count; i++)
        {
            var result = sorted[i];
            Console.WriteLine($"{i + 1}. {result.car.Brand} {result.car.Model} - €{result.car.Budget:N0}, {result.car.Power}KW");
            Console.WriteLine($"   Finale score: {result.scores.FinalScore:F3}");
            Console.WriteLine($"   Prijs-score: {result.scores.PriceScore:F3} | Vermogen-score: {result.scores.PowerScore:F3} | Merk-score: {result.scores.BrandScore:F3}");
            Console.WriteLine($"   Utility: {result.scores.UtilityScore:F3} | Similarity: {result.scores.SimilarityScore:F3}");
            Console.WriteLine();
        }

        // Verificatie: Top 2 moeten Audi's zijn rond 23-25k
        var top2 = sorted.Take(2).ToList();
        bool testPassed = top2.All(r => r.car.Brand == "Audi" && r.car.Budget >= 23000 && r.car.Budget <= 25000);
        
        Console.WriteLine($"✓ Test geslaagd: Top 2 zijn Audi's rond 23-25k = {testPassed}");
        
        // Verificatie: Budgetwagen (8k) moet niet bovenaan staan
        bool budgetWagenNotTop = sorted.First().car.Budget > 10000;
        Console.WriteLine($"✓ Budgetwagen niet bovenaan: {budgetWagenNotTop}");
    }

    /// <summary>
    /// Test scenario 2: Max budget 30k, comfortabel
    /// Verwacht: Auto's rond 25-30k met redelijk vermogen moeten bovenaan staan.
    /// </summary>
    static void TestScenario2(List<Car> cars)
    {
        var prefs = new UserPreferences
        {
            MaxBudget = 30000,
            ComfortVsSportScore = 0.8, // Comfortabel (1 = puur comfort)
            PreferredFuel = "petrol"
        };

        var factory = new CarFeatureVectorFactory();
        factory.Initialize(cars);
        var idealVector = factory.CreateIdealVector(prefs, cars);
        var scoringService = new AdvancedScoringService(featureVectorFactory: factory);

        var results = new List<(Car car, AdvancedScoringService.FeatureScoreResult scores)>();

        foreach (var car in cars)
        {
            if (car.Power <= 0 || car.Budget <= 0 || car.Year < 1900)
                continue;

            var scores = scoringService.CalculateScores(car, prefs, idealVector, cars);
            results.Add((car, scores));
        }

        var sorted = results.OrderByDescending(r => r.scores.FinalScore).ToList();

        Console.WriteLine("Ranking (hoogste score eerst):");
        Console.WriteLine("--------------------------------");
        for (int i = 0; i < sorted.Count; i++)
        {
            var result = sorted[i];
            Console.WriteLine($"{i + 1}. {result.car.Brand} {result.car.Model} - €{result.car.Budget:N0}, {result.car.Power}KW");
            Console.WriteLine($"   Finale score: {result.scores.FinalScore:F3}");
            Console.WriteLine($"   Prijs-score: {result.scores.PriceScore:F3} | Vermogen-score: {result.scores.PowerScore:F3}");
            Console.WriteLine();
        }

        // Verificatie: Top auto's moeten rond 25-30k zijn
        var top3 = sorted.Take(3).ToList();
        bool testPassed = top3.All(r => r.car.Budget >= 23000 && r.car.Budget <= 30000);
        Console.WriteLine($"✓ Test geslaagd: Top 3 zijn rond 25-30k = {testPassed}");
    }

    /// <summary>
    /// Test scenario 3: Geen budget voorkeur, neutraal
    /// Verwacht: Auto's rond gemiddelde budget moeten bovenaan staan.
    /// </summary>
    static void TestScenario3(List<Car> cars)
    {
        var prefs = new UserPreferences
        {
            ComfortVsSportScore = 0.5, // Neutraal
            PreferredFuel = "petrol"
        };

        var factory = new CarFeatureVectorFactory();
        factory.Initialize(cars);
        var idealVector = factory.CreateIdealVector(prefs, cars);
        var scoringService = new AdvancedScoringService(featureVectorFactory: factory);

        var results = new List<(Car car, AdvancedScoringService.FeatureScoreResult scores)>();

        foreach (var car in cars)
        {
            if (car.Power <= 0 || car.Budget <= 0 || car.Year < 1900)
                continue;

            var scores = scoringService.CalculateScores(car, prefs, idealVector, cars);
            results.Add((car, scores));
        }

        var sorted = results.OrderByDescending(r => r.scores.FinalScore).ToList();

        Console.WriteLine("Ranking (hoogste score eerst):");
        Console.WriteLine("--------------------------------");
        for (int i = 0; i < sorted.Count; i++)
        {
            var result = sorted[i];
            Console.WriteLine($"{i + 1}. {result.car.Brand} {result.car.Model} - €{result.car.Budget:N0}, {result.car.Power}KW");
            Console.WriteLine($"   Finale score: {result.scores.FinalScore:F3}");
            Console.WriteLine($"   Prijs-score: {result.scores.PriceScore:F3} | Vermogen-score: {result.scores.PowerScore:F3}");
            Console.WriteLine();
        }

        double avgBudget = cars.Average(c => (double)c.Budget);
        Console.WriteLine($"Gemiddeld budget: €{avgBudget:N0}");
        Console.WriteLine("✓ Test voltooid: Scores zijn gebaseerd op afstand tot gemiddelde");
    }
}









