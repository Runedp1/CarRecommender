namespace CarRecommender;

/// <summary>
/// Forecasting service - analyseert trends en patronen op basis van bouwjaar voor forecasting-achtige aanbevelingen.
/// 
/// FORECASTING COMPONENT voor ML & Forecasting vak:
/// 
/// Deze service analyseert trends in de auto dataset op basis van bouwjaar.
/// Hoewel we geen echte time-series data hebben, kunnen we bouwjaar gebruiken als een proxy voor tijd.
/// 
/// Trends die geanalyseerd worden:
/// 1. Prijs trends: Hoe verandert de gemiddelde prijs over de jaren?
/// 2. Vermogen trends: Hoe verandert het gemiddelde vermogen over de jaren?
/// 3. Brandstof trends: Hoe verandert de distributie van brandstoftypes over de jaren?
/// 
/// Deze trends kunnen geïnterpreteerd worden als:
/// - Seizoensanalyse: Verschillende trends per periode (oudere vs nieuwere auto's)
/// - Trendanalyse: Algemene trends in de markt (bijv. elektrische auto's worden populairder)
/// - Forecasting: Op basis van historische trends kunnen we voorspellingen doen voor toekomstige trends
/// 
/// Relevant voor ML & Forecasting vak:
/// - Demonstreert trendanalyse technieken
/// - Toont hoe tijdgerelateerde data geanalyseerd kan worden
/// - Biedt basis voor forecasting-achtige aanbevelingen (bijv. "elektrische auto's worden populairder")
/// </summary>
public class ForecastingService
{
    /// <summary>
    /// Analyseert trends in de auto dataset op basis van bouwjaar.
    /// 
    /// Forecasting Stappen:
    /// 1. Data aggregatie: Groepeer auto's per bouwjaar range
    /// 2. Trend berekening: Bereken gemiddelde prijs, vermogen per periode
    /// 3. Brandstof distributie: Analyseer hoe brandstoftypes veranderen over de jaren
    /// 4. Trend interpretatie: Identificeer patronen die als trends geïnterpreteerd kunnen worden
    /// </summary>
    public ForecastingResult AnalyzeTrends(List<Car> cars)
    {
        var validCars = cars
            .Where(c => c.Power > 0 && c.Budget > 0 && c.Year >= 1990 && c.Year <= DateTime.Now.Year)
            .ToList();
        
        if (validCars.Count == 0)
        {
            return new ForecastingResult
            {
                IsValid = false,
                ErrorMessage = "Onvoldoende data voor trendanalyse"
            };
        }
        
        // Definieer jaar ranges voor trendanalyse (seizoensanalyse per periode)
        var yearRanges = new[]
        {
            new { Name = "1990-2000", MinYear = 1990, MaxYear = 2000 },
            new { Name = "2001-2010", MinYear = 2001, MaxYear = 2010 },
            new { Name = "2011-2015", MinYear = 2011, MaxYear = 2015 },
            new { Name = "2016-2020", MinYear = 2016, MaxYear = 2020 },
            new { Name = "2021-2025", MinYear = 2021, MaxYear = 2025 }
        };
        
        var periodTrends = new List<PeriodTrend>();
        
        foreach (var range in yearRanges)
        {
            var carsInRange = validCars
                .Where(c => c.Year >= range.MinYear && c.Year <= range.MaxYear)
                .ToList();
            
            if (carsInRange.Count == 0)
                continue;
            
            // Bereken gemiddelde prijs en vermogen voor deze periode
            double avgPrice = (double)carsInRange.Average(c => c.Budget);
            double avgPower = carsInRange.Average(c => c.Power);
            
            // Analyseer brandstof distributie voor deze periode
            var fuelDistribution = carsInRange
                .GroupBy(c => c.Fuel?.ToLower().Trim() ?? "unknown")
                .Select(g => new FuelDistribution
                {
                    FuelType = g.Key,
                    Count = g.Count(),
                    Percentage = (double)g.Count() / carsInRange.Count * 100.0
                })
                .OrderByDescending(f => f.Count)
                .ToList();
            
            periodTrends.Add(new PeriodTrend
            {
                PeriodName = range.Name,
                MinYear = range.MinYear,
                MaxYear = range.MaxYear,
                AveragePrice = avgPrice,
                AveragePower = avgPower,
                CarCount = carsInRange.Count,
                FuelDistribution = fuelDistribution
            });
        }
        
        // Bereken algemene trends (trendanalyse)
        var generalTrends = CalculateGeneralTrends(periodTrends);
        
        return new ForecastingResult
        {
            IsValid = true,
            PeriodTrends = periodTrends,
            GeneralTrends = generalTrends,
            AnalysisTimestamp = DateTime.UtcNow
        };
    }
    
    /// <summary>
    /// Bereken algemene trends tussen periodes (bijv. stijgende prijzen, verschuiving naar elektrisch).
    /// 
    /// Trendanalyse: Vergelijk verschillende periodes om patronen te identificeren.
    /// </summary>
    private GeneralTrends CalculateGeneralTrends(List<PeriodTrend> periodTrends)
    {
        if (periodTrends.Count < 2)
        {
            return new GeneralTrends
            {
                PriceTrend = "Onvoldoende data",
                PowerTrend = "Onvoldoende data",
                FuelTrend = "Onvoldoende data"
            };
        }
        
        // Prijs trend: Vergelijk eerste en laatste periode
        var firstPeriod = periodTrends.First();
        var lastPeriod = periodTrends.Last();
        
        double priceChange = ((lastPeriod.AveragePrice - firstPeriod.AveragePrice) / firstPeriod.AveragePrice) * 100.0;
        string priceTrend = priceChange > 5 ? "Stijgend" : priceChange < -5 ? "Dalend" : "Stabiel";
        priceTrend += $" ({priceChange:F1}% verandering)";
        
        // Vermogen trend
        double powerChange = ((lastPeriod.AveragePower - firstPeriod.AveragePower) / firstPeriod.AveragePower) * 100.0;
        string powerTrend = powerChange > 5 ? "Stijgend" : powerChange < -5 ? "Dalend" : "Stabiel";
        powerTrend += $" ({powerChange:F1}% verandering)";
        
        // Brandstof trend: Analyseer verschuiving naar elektrisch/hybrid
        var firstPeriodFuel = firstPeriod.FuelDistribution.FirstOrDefault(f => 
            f.FuelType.Contains("electric") || f.FuelType.Contains("hybrid"));
        var lastPeriodFuel = lastPeriod.FuelDistribution.FirstOrDefault(f => 
            f.FuelType.Contains("electric") || f.FuelType.Contains("hybrid"));
        
        double firstElectricPercentage = firstPeriodFuel?.Percentage ?? 0.0;
        double lastElectricPercentage = lastPeriodFuel?.Percentage ?? 0.0;
        double electricChange = lastElectricPercentage - firstElectricPercentage;
        
        string fuelTrend = electricChange > 5 ? 
            $"Verschuiving naar elektrisch/hybrid (+{electricChange:F1}%)" :
            electricChange < -5 ?
            $"Verschuiving weg van elektrisch/hybrid ({electricChange:F1}%)" :
            "Stabiele brandstof distributie";
        
        return new GeneralTrends
        {
            PriceTrend = priceTrend,
            PowerTrend = powerTrend,
            FuelTrend = fuelTrend
        };
    }
}

/// <summary>
/// Resultaat model voor forecasting/trend analyse.
/// </summary>
public class ForecastingResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public List<PeriodTrend> PeriodTrends { get; set; } = new();
    public GeneralTrends? GeneralTrends { get; set; }
    public DateTime AnalysisTimestamp { get; set; }
}

/// <summary>
/// Trend analyse voor een specifieke periode (bijv. 2016-2020).
/// </summary>
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

/// <summary>
/// Brandstof distributie voor een periode.
/// </summary>
public class FuelDistribution
{
    public string FuelType { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
}

/// <summary>
/// Algemene trends tussen periodes.
/// </summary>
public class GeneralTrends
{
    public string PriceTrend { get; set; } = string.Empty;
    public string PowerTrend { get; set; } = string.Empty;
    public string FuelTrend { get; set; } = string.Empty;
}


