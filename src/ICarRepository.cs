namespace CarRecommender;

/// <summary>
/// Interface voor data toegang laag (Repository pattern).
/// Deze interface definieert hoe we auto data kunnen ophalen, ongeacht of het uit CSV, SQL, of een andere bron komt.
/// 
/// Voor Azure deployment:
/// - Maak een nieuwe SqlCarRepository die deze interface implementeert
/// - Registreer SqlCarRepository in Program.cs in plaats van CarRepository
/// - De rest van de applicatie blijft werken zonder wijzigingen
/// </summary>
public interface ICarRepository
{
    /// <summary>
    /// Haalt alle auto's op uit de data bron.
    /// </summary>
    List<Car> GetAllCars();

    /// <summary>
    /// Haalt een specifieke auto op op basis van ID.
    /// </summary>
    Car? GetCarById(int id);

    /// <summary>
    /// Filter auto's op basis van verschillende criteria.
    /// </summary>
    List<Car> FilterCars(
        string? brand = null,
        int? minBudget = null, 
        int? maxBudget = null,
        int? minPower = null, 
        int? maxPower = null,
        int? minYear = null, 
        int? maxYear = null,
        string? fuel = null);
}

