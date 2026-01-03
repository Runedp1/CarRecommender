namespace CarRecommender.Web.Models;

/// <summary>
/// Model voor ML evaluatie resultaten.
/// Komt overeen met CarRecommender.MlEvaluationResult uit de backend.
/// </summary>
public class MlEvaluationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public int TrainingSetSize { get; set; }
    public int TestSetSize { get; set; }
    public double PrecisionAtK { get; set; }
    public double RecallAtK { get; set; }
    public double MeanAbsoluteError { get; set; }
    public double RootMeanSquaredError { get; set; }
    public HyperparameterConfiguration? BestHyperparameters { get; set; }
    public List<HyperparameterResult> HyperparameterResults { get; set; } = new();
    public ForecastingResult? ForecastingResults { get; set; }
    public DateTime EvaluationTimestamp { get; set; }
}

public class HyperparameterConfiguration
{
    public double PowerWeight { get; set; }
    public double BudgetWeight { get; set; }
    public double YearWeight { get; set; }
    public double FuelWeight { get; set; }
}

public class HyperparameterResult
{
    public HyperparameterConfiguration Configuration { get; set; } = null!;
    public double Score { get; set; }
}

public class ForecastingResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public List<PeriodTrend> PeriodTrends { get; set; } = new();
    public GeneralTrends? GeneralTrends { get; set; }
    public DateTime AnalysisTimestamp { get; set; }
}

public class PeriodTrend
{
    public string PeriodName { get; set; } = string.Empty;
    public int MinYear { get; set; }
    public int MaxYear { get; set; }
    public double AveragePrice { get; set; }
    public double AveragePower { get; set; }
    public int CarCount { get; set; }
    public List<FuelDistribution> FuelDistribution { get; set; } = new();
}

public class FuelDistribution
{
    public string FuelType { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
}

public class GeneralTrends
{
    public string PriceTrend { get; set; } = string.Empty;
    public string PowerTrend { get; set; } = string.Empty;
    public string FuelTrend { get; set; } = string.Empty;
}







