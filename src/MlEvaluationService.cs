using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace CarRecommender;

/// <summary>
/// Service voor ML evaluatie met cross-validation en algoritme vergelijking
/// </summary>
public class MlEvaluationService : IMlEvaluationService
{
    private readonly ICarRepository _carRepository;
    private readonly MlRecommendationService _mlRecommendationService;
    private readonly AdvancedScoringService _advancedScoringService;
    private readonly KnnRecommendationService _knnRecommendationService;
    private readonly CarFeatureVectorFactory _featureVectorFactory;

    public MlEvaluationService(
        ICarRepository carRepository,
        MlRecommendationService mlRecommendationService,
        AdvancedScoringService advancedScoringService,
        KnnRecommendationService knnRecommendationService,
        CarFeatureVectorFactory featureVectorFactory)
    {
        _carRepository = carRepository;
        _mlRecommendationService = mlRecommendationService;
        _advancedScoringService = advancedScoringService;
        _knnRecommendationService = knnRecommendationService;
        _featureVectorFactory = featureVectorFactory;
    }

    // ============================================================================
    // OUDE METHODES (backwards compatibility - simpele implementaties)
    // ============================================================================
    
    public MlEvaluationResult EvaluateModel()
    {
        try
        {
            // Gebruik nieuwe cross-validation voor ML.NET
            var cvResult = PerformCrossValidation("mlnet", kFolds: 5);
            
            return new MlEvaluationResult
            {
                IsValid = true,
                Precision = cvResult.PrecisionAt10,
                Recall = cvResult.RecallAt10,
                PrecisionAtK = cvResult.PrecisionAt10,
                RecallAtK = cvResult.RecallAt10,
                MAE = 0, // Niet van toepassing voor recommendation task
                RMSE = 0,
                TrainingSetSize = cvResult.TotalTestCases * 4, // 80% train in 5-fold
                TestSetSize = cvResult.TotalTestCases,
                Message = $"Cross-validation completed with {cvResult.TotalTestCases} test cases. " +
                        $"Average response time: {cvResult.AverageResponseTimeMs:F2}ms"
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MlEval] Error in EvaluateModel: {ex.Message}");
            return new MlEvaluationResult
            {
                IsValid = false,
                ErrorMessage = ex.Message,
                Message = "Evaluation failed"
            };
        }
    }
    
    public double CalculatePrecisionAtK(List<RecommendationResult> recommendations, List<Car> relevantCars, int k)
    {
        var topK = recommendations.Take(k).ToList();
        var relevantIds = relevantCars.Select(c => c.Id).ToHashSet();
        var recommendedIds = topK.Select(r => r.Car.Id).ToHashSet();
        
        var truePositives = recommendedIds.Intersect(relevantIds).Count();
        return k > 0 ? (double)truePositives / k : 0;
    }
    
    public double CalculateRecallAtK(List<RecommendationResult> recommendations, List<Car> relevantCars, int k)
    {
        var topK = recommendations.Take(k).ToList();
        var relevantIds = relevantCars.Select(c => c.Id).ToHashSet();
        var recommendedIds = topK.Select(r => r.Car.Id).ToHashSet();
        
        var truePositives = recommendedIds.Intersect(relevantIds).Count();
        return relevantIds.Count > 0 ? (double)truePositives / relevantIds.Count : 0;
    }
    
    public double CalculateMeanAbsoluteError(List<Car> predicted, List<Car> actual)
    {
        if (predicted.Count != actual.Count) return 0;
        
        // Gebruik Budget in plaats van Price
        var errors = predicted.Zip(actual, (p, a) => Math.Abs((double)(p.Budget - a.Budget))).ToList();
        return errors.Any() ? errors.Average() : 0;
    }

    public double CalculateRootMeanSquaredError(List<Car> predicted, List<Car> actual)
    {
        if (predicted.Count != actual.Count) return 0;
        
        // Gebruik Budget in plaats van Price
        var squaredErrors = predicted.Zip(actual, (p, a) => Math.Pow((double)(p.Budget - a.Budget), 2)).ToList();
        return squaredErrors.Any() ? Math.Sqrt(squaredErrors.Average()) : 0;
    }

    // ============================================================================
    // NIEUWE CROSS-VALIDATION METHODES
    // ============================================================================
    
    public CrossValidationResult PerformCrossValidation(string algorithmName, int kFolds = 5, int topK = 10)
    {
        Console.WriteLine($"[MlEval] Starting cross-validation for {algorithmName}...");
        
        var allCars = _carRepository.GetAllCars();
        var folds = CreateFolds(allCars, kFolds);
        var foldResults = new List<FoldMetrics>();
        var responseTimes = new List<double>();

        for (int i = 0; i < kFolds; i++)
        {
            Console.WriteLine($"[MlEval] Processing fold {i + 1}/{kFolds}...");
            
            var trainSet = folds.Where((_, idx) => idx != i).SelectMany(f => f).ToList();
            var testSet = folds[i];

            var (metrics, avgTime) = EvaluateFold(algorithmName, trainSet, testSet, topK, i + 1);
            
            foldResults.Add(metrics);
            responseTimes.Add(avgTime);
        }

        return new CrossValidationResult
        {
            AlgorithmName = algorithmName,
            PrecisionAt10 = foldResults.Average(f => f.Precision),
            RecallAt10 = foldResults.Average(f => f.Recall),
            F1Score = foldResults.Average(f => f.F1Score),
            AverageResponseTimeMs = responseTimes.Average(),
            TotalTestCases = foldResults.Sum(f => f.TestSize),
            FoldResults = foldResults
        };
    }

    public AlgorithmComparison CompareAllAlgorithms(int kFolds = 5)
    {
        var algorithms = new[] { "mlnet", "knn" };
        var results = new Dictionary<string, CrossValidationResult>();

        foreach (var algo in algorithms)
        {
            Console.WriteLine($"\n[MlEval] === Evaluating {algo.ToUpper()} ===");
            try
            {
                results[algo] = PerformCrossValidation(algo, kFolds);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MlEval] Error evaluating {algo}: {ex.Message}");
            }
        }

        return new AlgorithmComparison
        {
            Results = results,
            BestByPrecision = results.OrderByDescending(r => r.Value.PrecisionAt10).First().Key,
            BestByRecall = results.OrderByDescending(r => r.Value.RecallAt10).First().Key,
            BestBySpeed = results.OrderBy(r => r.Value.AverageResponseTimeMs).First().Key
        };
    }

    // ============================================================================
    // PRIVATE HELPER METHODES
    // ============================================================================
    
    private (FoldMetrics metrics, double avgResponseTime) EvaluateFold(
        string algorithmName,
        List<Car> trainSet,
        List<Car> testSet,
        int topK,
        int foldNumber)
    {
        var precisionScores = new List<double>();
        var recallScores = new List<double>();
        var responseTimes = new List<double>();

        // Limiteer test set voor snelheid (max 50 auto's per fold)
        var testSample = testSet.Take(50).ToList();

        foreach (var testCar in testSample)
        {
            var sw = Stopwatch.StartNew();
            var recommendations = GetRecommendations(algorithmName, testCar, topK, trainSet);
            sw.Stop();
            
            responseTimes.Add(sw.Elapsed.TotalMilliseconds);

            var relevantCars = GetGroundTruth(testCar, trainSet);
            var recommendedIds = recommendations.Select(r => r.Id).ToHashSet();
            var relevantIds = relevantCars.Select(c => c.Id).ToHashSet();

            var truePositives = recommendedIds.Intersect(relevantIds).Count();
            var precision = recommendedIds.Count > 0 ? (double)truePositives / recommendedIds.Count : 0;
            var recall = relevantIds.Count > 0 ? (double)truePositives / relevantIds.Count : 0;

            precisionScores.Add(precision);
            recallScores.Add(recall);
        }

        var avgPrecision = precisionScores.Average();
        var avgRecall = recallScores.Average();
        var f1 = avgPrecision + avgRecall > 0
            ? 2 * (avgPrecision * avgRecall) / (avgPrecision + avgRecall)
            : 0;

        return (new FoldMetrics
        {
            FoldNumber = foldNumber,
            Precision = avgPrecision,
            Recall = avgRecall,
            F1Score = f1,
            TestSize = testSample.Count
        }, responseTimes.Average());
    }

    private List<Car> GetRecommendations(string algorithmName, Car queryCar, int topK, List<Car> trainSet)
    {
        try
        {
            switch (algorithmName.ToLower())
            {
                case "mlnet":
                    // ML.NET: Gebruik PredictScore voor elke auto en sorteer
                    // OPTIMALISATIE: Beperk trainSet voor evaluatie (max 500 auto's voor snelheid)
                    // Dit versnelt evaluatie drastisch zonder training te verkleinen
                    var limitedTrainSet = trainSet.Take(500).ToList();
                    
                    var mlScores = limitedTrainSet
                        .Select(car => new 
                        { 
                            Car = car, 
                            Score = _mlRecommendationService.PredictScore(car, trainSet) 
                        })
                        .OrderByDescending(x => x.Score)
                        .Take(topK)
                        .Select(x => x.Car)
                        .ToList();
                    return mlScores;

                case "cosine":
                    // Cosine Similarity: Gebruik simpele feature matching score
                    var cosineScores = trainSet
                        .Select(car => new
                        {
                            Car = car,
                            Score = CalculateSimpleCosineSimilarity(queryCar, car)
                        })
                        .OrderByDescending(x => x.Score)
                        .Take(topK)
                        .Select(x => x.Car)
                        .ToList();
                    return cosineScores;

                case "knn":
                    // KNN: Gebruik FindNearestNeighbors
                    _featureVectorFactory.Initialize(trainSet);
                    var knnResults = _knnRecommendationService.FindNearestNeighbors(queryCar, trainSet);
                    return knnResults
                        .Take(topK)
                        .Select(r => r.Car)
                        .ToList();

                default:
                    throw new ArgumentException($"Unknown algorithm: {algorithmName}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MlEval] Error in GetRecommendations for {algorithmName}: {ex.Message}");
            return new List<Car>();
        }
    }

    private double CalculateSimpleCosineSimilarity(Car car1, Car car2)
    {
        double score = 0;
        int totalFeatures = 7;
        
        // Categorical features (0 or 1)
        if (car1.Brand == car2.Brand) score += 1;
        if (car1.Fuel == car2.Fuel) score += 1;
        if (car1.Transmission == car2.Transmission) score += 1;
        if (car1.BodyType == car2.BodyType) score += 1;
        
        // Numeric features (normalized similarity 0-1)
        // Budget similarity
        var budgetDiff = Math.Abs((double)(car1.Budget - car2.Budget));
        var budgetMax = (double)Math.Max(car1.Budget, car2.Budget);
        score += budgetMax > 0 ? (1 - Math.Min(budgetDiff / budgetMax, 1)) : 0;
        
        // Power similarity
        var powerDiff = Math.Abs(car1.Power - car2.Power);
        var powerMax = Math.Max(car1.Power, car2.Power);
        score += powerMax > 0 ? (1 - Math.Min((double)powerDiff / powerMax, 1)) : 0;
        
        // Year similarity
        var yearDiff = Math.Abs(car1.Year - car2.Year);
        score += 1 - Math.Min(yearDiff / 10.0, 1); // Max 10 jaar verschil
        
        return score / totalFeatures; // Normalize to 0-1
    }

    private List<Car> GetGroundTruth(Car queryCar, List<Car> candidateCars)
    {
        var relevant = new List<Car>();

        foreach (var car in candidateCars)
        {
            if (car.Id == queryCar.Id) continue;

            int matches = 0;
            
            // Tel exacte matches op categorische features
            if (car.Brand == queryCar.Brand) matches++;
            if (car.Fuel == queryCar.Fuel) matches++;
            if (car.Transmission == queryCar.Transmission) matches++;
            if (car.BodyType == queryCar.BodyType) matches++;

            // Numerieke proximity (gebruik Budget in plaats van Price!)
            var budgetSimilar = Math.Abs(car.Budget - queryCar.Budget) < queryCar.Budget * 0.2m; // ±20%
            var yearSimilar = Math.Abs(car.Year - queryCar.Year) <= 3; // ±3 jaar
            var powerSimilar = Math.Abs(car.Power - queryCar.Power) < queryCar.Power * 0.15; // ±15%

            // Auto is relevant als 3+ matches OF 2+ matches + alle numeriek
            if (matches >= 3 || (matches >= 2 && budgetSimilar && yearSimilar && powerSimilar))
            {
                relevant.Add(car);
            }
        }

        return relevant.Take(20).ToList(); // Max 20 relevante auto's
    }

    private List<List<Car>> CreateFolds(List<Car> cars, int k)
    {
        var shuffled = cars.OrderBy(_ => Guid.NewGuid()).ToList();
        var folds = new List<List<Car>>();
        var foldSize = cars.Count / k;

        for (int i = 0; i < k; i++)
        {
            var fold = shuffled
                .Skip(i * foldSize)
                .Take(i == k - 1 ? cars.Count - (i * foldSize) : foldSize)
                .ToList();
            folds.Add(fold);
        }

        return folds;
    }
}