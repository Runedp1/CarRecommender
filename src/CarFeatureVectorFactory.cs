namespace CarRecommender;

/// <summary>
/// Factory voor het maken van CarFeatureVector objecten uit Car objecten.
/// Handelt normalisatie en one-hot encoding af.
/// </summary>
public class CarFeatureVectorFactory
{
    private readonly Dictionary<string, int> _brandIndexMap = new Dictionary<string, int>();
    private readonly Dictionary<string, int> _fuelIndexMap = new Dictionary<string, int>();
    private readonly Dictionary<string, int> _transmissionIndexMap = new Dictionary<string, int>();
    private readonly Dictionary<string, int> _bodyTypeIndexMap = new Dictionary<string, int>();

    private double _minPrice = double.MaxValue;
    private double _maxPrice = double.MinValue;
    private int _minYear = int.MaxValue;
    private int _maxYear = int.MinValue;
    private int _minPower = int.MaxValue;
    private int _maxPower = int.MinValue;

    private bool _isInitialized = false;

    /// <summary>
    /// Initialiseert de factory met alle auto's om normalisatie ranges en encoding dictionaries te bepalen.
    /// Moet worden aangeroepen voordat CreateVector wordt gebruikt.
    /// </summary>
    public void Initialize(List<Car> cars)
    {
        if (_isInitialized)
            return;

        // Verzamel alle unieke waarden voor categorische features
        var brands = new HashSet<string>();
        var fuels = new HashSet<string>();
        var transmissions = new HashSet<string>();
        var bodyTypes = new HashSet<string>();

        foreach (var car in cars)
        {
            // Verzamel categorische waarden
            if (!string.IsNullOrWhiteSpace(car.Brand))
                brands.Add(car.Brand.ToLower().Trim());
            
            if (!string.IsNullOrWhiteSpace(car.Fuel))
                fuels.Add(car.Fuel.ToLower().Trim());
            
            if (!string.IsNullOrWhiteSpace(car.Transmission))
                transmissions.Add(car.Transmission.ToLower().Trim());
            
            if (!string.IsNullOrWhiteSpace(car.BodyType))
                bodyTypes.Add(car.BodyType.ToLower().Trim());

            // Verzamel numerieke waarden voor normalisatie
            if (car.Budget > 0)
            {
                _minPrice = Math.Min(_minPrice, (double)car.Budget);
                _maxPrice = Math.Max(_maxPrice, (double)car.Budget);
            }

            if (car.Year > 1900)
            {
                _minYear = Math.Min(_minYear, car.Year);
                _maxYear = Math.Max(_maxYear, car.Year);
            }

            if (car.Power > 0)
            {
                _minPower = Math.Min(_minPower, car.Power);
                _maxPower = Math.Max(_maxPower, car.Power);
            }
        }

        // Maak index maps voor one-hot encoding
        int index = 0;
        foreach (var brand in brands.OrderBy(b => b))
        {
            _brandIndexMap[brand] = index++;
        }

        index = 0;
        foreach (var fuel in fuels.OrderBy(f => f))
        {
            _fuelIndexMap[fuel] = index++;
        }

        index = 0;
        foreach (var transmission in transmissions.OrderBy(t => t))
        {
            _transmissionIndexMap[transmission] = index++;
        }

        index = 0;
        foreach (var bodyType in bodyTypes.OrderBy(b => b))
        {
            _bodyTypeIndexMap[bodyType] = index++;
        }

        // Zorg voor geldige ranges (voorkom delen door nul)
        if (_maxPrice <= _minPrice) _maxPrice = _minPrice + 1;
        if (_maxYear <= _minYear) _maxYear = _minYear + 1;
        if (_maxPower <= _minPower) _maxPower = _minPower + 1;

        _isInitialized = true;
    }

    /// <summary>
    /// Maakt een feature vector van een Car object.
    /// </summary>
    public CarFeatureVector CreateVector(Car car)
    {
        if (!_isInitialized)
            throw new InvalidOperationException("Factory moet eerst worden geïnitialiseerd met Initialize()");

        var vector = new CarFeatureVector();

        // Normaliseer numerieke waarden (min-max normalisatie naar 0-1)
        vector.NormalizedPrice = NormalizeValue((double)car.Budget, _minPrice, _maxPrice);
        vector.NormalizedYear = NormalizeValue(car.Year, _minYear, _maxYear);
        vector.NormalizedPower = NormalizeValue(car.Power, _minPower, _maxPower);

        // One-hot encoding voor categorische features
        InitializeOneHotEncoding(vector.BrandEncoding, car.Brand ?? "", _brandIndexMap);
        InitializeOneHotEncoding(vector.FuelEncoding, car.Fuel ?? "", _fuelIndexMap);
        InitializeOneHotEncoding(vector.TransmissionEncoding, car.Transmission ?? "", _transmissionIndexMap);
        InitializeOneHotEncoding(vector.BodyTypeEncoding, car.BodyType ?? "", _bodyTypeIndexMap);

        return vector;
    }

    /// <summary>
    /// Maakt een feature vector voor een ideale auto op basis van user preferences.
    /// </summary>
    public CarFeatureVector CreateIdealVector(UserPreferences prefs, List<Car> availableCars)
    {
        if (!_isInitialized)
            throw new InvalidOperationException("Factory moet eerst worden geïnitialiseerd met Initialize()");

        var vector = new CarFeatureVector();

        // Bepaal ideale numerieke waarden
        double idealPrice = prefs.MaxBudget.HasValue 
            ? (double)prefs.MaxBudget.Value 
            : availableCars.Where(c => c.Budget > 0).Select(c => (double)c.Budget).DefaultIfEmpty(30000).Average();
        
        int idealYear = DateTime.Now.Year - 2; // Relatief nieuw
        double idealPower = prefs.MinPower.HasValue && prefs.MinPower.Value > 100
            ? prefs.MinPower.Value
            : prefs.MinPower.HasValue && prefs.MinPower.Value <= 1.0
                ? _minPower + (prefs.MinPower.Value * (_maxPower - _minPower))
                : availableCars.Where(c => c.Power > 0).Select(c => (double)c.Power).DefaultIfEmpty(120).Average();

        // Normaliseer
        vector.NormalizedPrice = NormalizeValue(idealPrice, _minPrice, _maxPrice);
        vector.NormalizedYear = NormalizeValue(idealYear, _minYear, _maxYear);
        vector.NormalizedPower = NormalizeValue(idealPower, _minPower, _maxPower);

        // One-hot encoding voor categorische features
        string preferredFuel = prefs.PreferredFuel?.ToLower().Trim() ?? "";
        string preferredBrand = prefs.PreferredBrand?.ToLower().Trim() ?? "";
        string preferredTransmission = prefs.AutomaticTransmission.HasValue 
            ? (prefs.AutomaticTransmission.Value ? "automatic" : "manual")
            : "";
        string preferredBodyType = prefs.BodyTypePreference?.ToLower().Trim() ?? "";

        InitializeOneHotEncoding(vector.BrandEncoding, preferredBrand ?? "", _brandIndexMap);
        InitializeOneHotEncoding(vector.FuelEncoding, preferredFuel ?? "", _fuelIndexMap);
        InitializeOneHotEncoding(vector.TransmissionEncoding, preferredTransmission ?? "", _transmissionIndexMap);
        InitializeOneHotEncoding(vector.BodyTypeEncoding, preferredBodyType ?? "", _bodyTypeIndexMap);

        return vector;
    }

    /// <summary>
    /// Normaliseert een waarde naar 0-1 bereik (min-max normalisatie).
    /// </summary>
    private double NormalizeValue(double value, double min, double max)
    {
        if (max <= min)
            return 0.5; // Default waarde als range ongeldig is

        double normalized = (value - min) / (max - min);
        return Math.Max(0.0, Math.Min(1.0, normalized)); // Clamp naar 0-1
    }

    /// <summary>
    /// Initialiseert one-hot encoding dictionary voor een categorische waarde.
    /// </summary>
    private void InitializeOneHotEncoding(Dictionary<string, double> encoding, string value, Dictionary<string, int> indexMap)
    {
        // Initialiseer alle mogelijke waarden met 0.0
        foreach (var key in indexMap.Keys)
        {
            encoding[key] = 0.0;
        }

        // Zet de aanwezige waarde op 1.0
        if (!string.IsNullOrWhiteSpace(value))
        {
            string normalizedValue = value.ToLower().Trim();
            
            // Zoek exacte match
            if (indexMap.ContainsKey(normalizedValue))
            {
                encoding[normalizedValue] = 1.0;
            }
            else
            {
                // Zoek gedeeltelijke match (bijv. "plug-in hybrid" matcht "hybrid")
                var match = indexMap.Keys.FirstOrDefault(k => 
                    normalizedValue.Contains(k) || k.Contains(normalizedValue));
                if (match != null)
                {
                    encoding[match] = 1.0;
                }
            }
        }
    }
}

