namespace CarRecommender;

/// <summary>
/// Old AI - Rule-based filtering.
/// Implementeert harde filters die de candidate set bepalen.
/// Niets mag deze regels omzeilen - dit is de eerste laag van filtering.
/// 
/// Filters op:
/// - Budget (min/max)
/// - Brandstof (exacte match of varianten)
/// - Merk (exacte match)
/// - Carrosserie (body type)
/// - Transmissie (automatic/manual)
/// - Bouwjaar (min/max)
/// </summary>
public class RuleBasedFilter
{
    /// <summary>
    /// Filter criteria voor rule-based filtering.
    /// </summary>
    public class FilterCriteria
    {
        public decimal? MinBudget { get; set; }
        public decimal? MaxBudget { get; set; }
        public string? PreferredFuel { get; set; }
        public string? PreferredBrand { get; set; }
        public string? PreferredBodyType { get; set; }
        public bool? AutomaticTransmission { get; set; }  // true = automaat, false = schakel, null = geen voorkeur
        public int? MinYear { get; set; }
        public int? MaxYear { get; set; }
    }

    /// <summary>
    /// Filtert auto's op basis van harde regels.
    /// Retourneert alleen auto's die aan ALLE criteria voldoen.
    /// </summary>
    public List<Car> FilterCars(List<Car> cars, FilterCriteria criteria)
    {
        var filtered = cars.AsEnumerable();

        // Budget filter
        if (criteria.MinBudget.HasValue)
        {
            filtered = filtered.Where(c => c.Budget >= criteria.MinBudget.Value);
        }
        if (criteria.MaxBudget.HasValue)
        {
            filtered = filtered.Where(c => c.Budget <= criteria.MaxBudget.Value);
        }

        // Brandstof filter (exacte match of varianten)
        if (!string.IsNullOrWhiteSpace(criteria.PreferredFuel))
        {
            string fuelLower = criteria.PreferredFuel.ToLower().Trim();
            filtered = filtered.Where(c => 
            {
                string carFuel = c.Fuel.ToLower().Trim();
                
                // Exacte match
                if (carFuel == fuelLower)
                    return true;
                
                // Varianten match (bijv. "hybrid" matcht "plug-in hybrid")
                if (fuelLower.Contains("hybrid") && carFuel.Contains("hybrid"))
                    return true;
                if (fuelLower.Contains("electric") && carFuel.Contains("electric"))
                    return true;
                if (fuelLower.Contains("petrol") && (carFuel.Contains("petrol") || carFuel.Contains("gasoline") || carFuel.Contains("benzine")))
                    return true;
                if (fuelLower.Contains("diesel") && carFuel.Contains("diesel"))
                    return true;
                
                return false;
            });
        }

        // Merk filter (exacte match, case-insensitive)
        if (!string.IsNullOrWhiteSpace(criteria.PreferredBrand))
        {
            filtered = filtered.Where(c => 
                c.Brand.Equals(criteria.PreferredBrand, StringComparison.OrdinalIgnoreCase));
        }

        // Carrosserie filter
        if (!string.IsNullOrWhiteSpace(criteria.PreferredBodyType))
        {
            string bodyTypeLower = criteria.PreferredBodyType.ToLower().Trim();
            filtered = filtered.Where(c => 
            {
                if (string.IsNullOrWhiteSpace(c.BodyType))
                    return false; // Geen body type = geen match
                
                string carBodyType = c.BodyType.ToLower().Trim();
                
                // Exacte match
                if (carBodyType == bodyTypeLower)
                    return true;
                
                // Varianten (bijv. "suv" matcht "jeep")
                if (bodyTypeLower == "suv" && (carBodyType.Contains("suv") || carBodyType.Contains("jeep") || carBodyType.Contains("4x4")))
                    return true;
                if (bodyTypeLower == "station" && (carBodyType.Contains("station") || carBodyType.Contains("break") || carBodyType.Contains("combi")))
                    return true;
                if (bodyTypeLower == "sedan" && (carBodyType.Contains("sedan") || carBodyType.Contains("berline") || carBodyType.Contains("limousine")))
                    return true;
                if (bodyTypeLower == "hatchback" && (carBodyType.Contains("hatchback") || carBodyType.Contains("compact")))
                    return true;
                
                return false;
            });
        }

        // Transmissie filter
        if (criteria.AutomaticTransmission.HasValue)
        {
            bool wantsAutomatic = criteria.AutomaticTransmission.Value;
            filtered = filtered.Where(c => 
            {
                if (string.IsNullOrWhiteSpace(c.Transmission))
                    return false; // Geen transmissie info = geen match
                
                string transmission = c.Transmission.ToLower().Trim();
                bool isAutomatic = transmission.Contains("automatic") || 
                                   transmission.Contains("automaat") || 
                                   transmission.Contains("cvt") ||
                                   transmission.Contains("dct");
                
                return isAutomatic == wantsAutomatic;
            });
        }

        // Bouwjaar filter
        if (criteria.MinYear.HasValue)
        {
            filtered = filtered.Where(c => c.Year >= criteria.MinYear.Value);
        }
        if (criteria.MaxYear.HasValue)
        {
            filtered = filtered.Where(c => c.Year <= criteria.MaxYear.Value);
        }

        return filtered.ToList();
    }

    /// <summary>
    /// Converteert UserPreferences naar FilterCriteria voor rule-based filtering.
    /// </summary>
    public FilterCriteria ConvertPreferencesToCriteria(UserPreferences prefs)
    {
        return new FilterCriteria
        {
            MaxBudget = prefs.MaxBudget.HasValue ? (decimal)prefs.MaxBudget.Value : null,
            PreferredFuel = prefs.PreferredFuel,
            PreferredBrand = prefs.PreferredBrand, // Merk voorkeur wordt nu ondersteund
            PreferredBodyType = prefs.BodyTypePreference,
            AutomaticTransmission = prefs.AutomaticTransmission,
            MinYear = null, // Bouwjaar min wordt niet uit preferences gehaald voor nu
            MaxYear = null  // Bouwjaar max wordt niet uit preferences gehaald voor nu
        };
    }
}

