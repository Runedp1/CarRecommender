namespace CarRecommender.Web.Models;

/// <summary>
/// Car model voor de frontend - komt overeen met de API response.
/// </summary>
public class Car
{
    public int Id { get; set; }
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Power { get; set; }
    public string Fuel { get; set; } = string.Empty;
    public decimal Budget { get; set; }
    public int Year { get; set; }
    public string? Transmission { get; set; }  // transmissie (automatic/manual)
    public string? BodyType { get; set; }  // carrosserie (suv/sedan/hatchback/etc.)
    public string? DriveType { get; set; }  // aandrijving (FWD/RWD/AWD)
    public string ImagePath { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
}

