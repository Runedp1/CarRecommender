using System.Globalization;

namespace CarRecommender;

/// <summary>
/// Implementatie van ICarRepository - leest CSV bestanden en zet ze om naar Car objecten.
/// Handelt alle data toegang af, later kunnen we dit makkelijk aanpassen voor Azure SQL.
/// 
/// Voor Azure deployment:
/// - Maak een nieuwe SqlCarRepository die ICarRepository implementeert
/// - Registreer SqlCarRepository in Program.cs in plaats van deze klasse
/// - De rest van de applicatie blijft werken zonder wijzigingen
/// </summary>
public class CarRepository : ICarRepository
{
    private List<Car> _cars = new List<Car>();
    private readonly string _csvFileName;
    private readonly string _dataDirectory;

    /// <summary>
    /// Constructor - laadt auto's uit CSV bij initialisatie.
    /// Gebruikt configuratie voor CSV bestandsnaam en data directory.
    /// </summary>
    public CarRepository(string csvFileName = "Cleaned_Car_Data_For_App_Fully_Enriched.csv", string dataDirectory = "data")
    {
        _csvFileName = csvFileName;
        _dataDirectory = dataDirectory;
        LoadCarsFromCsv();
    }

    /// <summary>
    /// Laadt auto's uit CSV bestand bij initialisatie.
    /// Gebruikt geconfigureerde bestandsnaam en directory.
    /// </summary>
    private void LoadCarsFromCsv()
    {
        string csvPath = FindCsvFile(_csvFileName, _dataDirectory);
        
        if (string.IsNullOrEmpty(csvPath) || !File.Exists(csvPath))
        {
            Console.WriteLine($"Waarschuwing: Cleaned_Car_Data_For_App_Fully_Enriched.csv niet gevonden in data directory.");
            Console.WriteLine($"Gezocht in: {Path.Combine(Directory.GetCurrentDirectory(), "data")}");
            _cars = new List<Car>();
            return;
        }

        Console.WriteLine($"CSV-bestand gevonden: {csvPath}");
        Console.WriteLine("Laden van auto's...");

        // Parse CSV naar Car objecten
        _cars = LoadCarsFromCsv(csvPath);

        // Genereer image paths
        AssignImagePaths(_cars);

        Console.WriteLine($"Aantal ingelezen auto's: {_cars.Count}");
    }

    /// <summary>
    /// Haalt alle auto's op uit de data bron.
    /// </summary>
    public List<Car> GetAllCars()
    {
        return _cars;
    }

    /// <summary>
    /// Haalt een specifieke auto op op basis van ID.
    /// </summary>
    public Car? GetCarById(int id)
    {
        return _cars.FirstOrDefault(c => c.Id == id);
    }
    /// <summary>
    /// Leest CSV bestand en maakt Car objecten. Slaat foutieve rijen over.
    /// Interne methode voor het laden van data.
    /// </summary>
    private List<Car> LoadCarsFromCsv(string csvPath)
    {
        List<Car> cars = new List<Car>();
        int rowNumber = 0;

        try
        {
            // Lees alle regels uit het CSV-bestand
            string[] lines = File.ReadAllLines(csvPath);

            if (lines.Length == 0)
            {
                Console.WriteLine("Waarschuwing: Het CSV-bestand is leeg.");
                return cars;
            }

            // Zoek welke kolom waar staat (flexibel, werkt met verschillende CSV formaten)
            string header = lines[0].ToLower();
            string[] headerColumns = ParseCsvLine(header);
            
            int idIndex = FindColumnIndex(headerColumns, new[] { "id" });
            int merkIndex = FindColumnIndex(headerColumns, new[] { "merk", "brand", "company names" });
            int modelIndex = FindColumnIndex(headerColumns, new[] { "model", "cars names" });
            int vermogenIndex = FindColumnIndex(headerColumns, new[] { "vermogen", "power", "horsepower", "engines" });
            int brandstofIndex = FindColumnIndex(headerColumns, new[] { "brandstof", "fuel", "fuel types" });
            int budgetIndex = FindColumnIndex(headerColumns, new[] { "budget", "prijs", "price", "cars prices" });
            int bouwjaarIndex = FindColumnIndex(headerColumns, new[] { "bouwjaar", "year", "jaar" });
            int transmissionIndex = FindColumnIndex(headerColumns, new[] { "transmission", "transmissie" });
            int bodyTypeIndex = FindColumnIndex(headerColumns, new[] { "type_auto", "bodytype", "body type", "carrosserie", "type" });
            int imagePathIndex = FindColumnIndex(headerColumns, new[] { "image_path", "Image_Path", "imagepath", "image name", "genmodel_id", "Image_ID" });

            // Verwerk alle data regels (skip header)
            for (int i = 1; i < lines.Length; i++)
            {
                rowNumber = i + 1; // Voor foutmeldingen (1-gebaseerd)

                try
                {
                    string line = lines[i].Trim();
                    
                    // Sla lege regels over
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    // Parse CSV regel (handelt komma's in strings correct af)
                    string[] columns = ParseCsvLine(line);

                    Car car = new Car();

                    // Parse en valideer Id
                    if (idIndex >= 0 && idIndex < columns.Length && 
                        int.TryParse(columns[idIndex]?.Trim(), out int id))
                    {
                        car.Id = id;
                    }
                    else if (idIndex < 0)
                    {
                        // Geen id kolom gevonden, gebruik rijnummer als ID
                        car.Id = i;
                    }

                    // Parse Merk (Brand)
                    if (merkIndex >= 0 && merkIndex < columns.Length)
                    {
                        car.Brand = columns[merkIndex]?.Trim() ?? string.Empty;
                    }

                    // Parse Model
                    if (modelIndex >= 0 && modelIndex < columns.Length)
                    {
                        car.Model = columns[modelIndex]?.Trim() ?? string.Empty;
                    }

                    // Parse Vermogen (Power)
                    if (vermogenIndex >= 0 && vermogenIndex < columns.Length)
                    {
                        string vermogenStr = columns[vermogenIndex]?.Trim() ?? string.Empty;
                        // Haal alleen cijfers eruit (bijv. "963 hp" -> 963)
                        vermogenStr = System.Text.RegularExpressions.Regex.Replace(vermogenStr, @"[^\d]", "");
                        if (int.TryParse(vermogenStr, out int power))
                        {
                            car.Power = power;
                        }
                    }

                    // Parse Brandstof (Fuel)
                    if (brandstofIndex >= 0 && brandstofIndex < columns.Length)
                    {
                        car.Fuel = columns[brandstofIndex]?.Trim() ?? string.Empty;
                    }

                    // Parse Budget (Budget/Price)
                    if (budgetIndex >= 0 && budgetIndex < columns.Length)
                    {
                        string budgetStr = columns[budgetIndex]?.Trim() ?? string.Empty;
                        // Maak prijs schoon (verwijder euro tekens etc.)
                        budgetStr = System.Text.RegularExpressions.Regex.Replace(budgetStr, @"[^\d.,]", "");
                        budgetStr = budgetStr.Replace(",", ".");
                        if (decimal.TryParse(budgetStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal budget))
                        {
                            car.Budget = budget;
                        }
                    }

                    // Parse Bouwjaar (Year)
                    if (bouwjaarIndex >= 0 && bouwjaarIndex < columns.Length)
                    {
                        string jaarStr = columns[bouwjaarIndex]?.Trim() ?? string.Empty;
                        // Pak alleen het jaar eruit (4 cijfers)
                        var yearMatch = System.Text.RegularExpressions.Regex.Match(jaarStr, @"\b(19|20)\d{2}\b");
                        if (yearMatch.Success && int.TryParse(yearMatch.Value, out int year) && year >= 1900 && year <= DateTime.Now.Year + 1)
                        {
                            car.Year = year;
                        }
                    }

                    // Parse Transmissie (Transmission)
                    if (transmissionIndex >= 0 && transmissionIndex < columns.Length)
                    {
                        car.Transmission = columns[transmissionIndex]?.Trim();
                    }

                    // Parse Carrosserie (Body Type)
                    if (bodyTypeIndex >= 0 && bodyTypeIndex < columns.Length)
                    {
                        car.BodyType = columns[bodyTypeIndex]?.Trim();
                    }

                    // Image path uit CSV (optioneel, anders wordt het later gegenereerd)
                    // Zoek naar Image_Path kolom (let op hoofdletters)
                    int imagePathColIndex = FindColumnIndex(headerColumns, new[] { "Image_Path", "image_path", "imagepath" });
                    if (imagePathColIndex >= 0 && imagePathColIndex < columns.Length)
                    {
                        string imagePath = columns[imagePathColIndex]?.Trim() ?? string.Empty;
                        if (!string.IsNullOrWhiteSpace(imagePath) && imagePath.ToLower() != "none")
                        {
                            car.ImagePath = imagePath;
                            // Als Image_Path een bestandsnaam bevat (bijv. "Lexus$$RX 350$$2007$$Black$$48_24$$6$$image_0.jpg"),
                            // kunnen we dit gebruiken om een URL te genereren
                            if (imagePath.Contains("$$") || imagePath.Contains(".jpg") || imagePath.Contains(".png"))
                            {
                                // Parse Image_name om merk en model te extraheren voor betere image URL
                                string[] parts = imagePath.Split(new[] { "$$" }, StringSplitOptions.RemoveEmptyEntries);
                                if (parts.Length >= 2)
                                {
                                    // Gebruik merk en model uit Image_name voor betere image URL
                                    // Maar gebruik alleen als Brand/Model nog niet gezet zijn
                                    if (string.IsNullOrWhiteSpace(car.Brand) && parts.Length > 0)
                                        car.Brand = parts[0].Trim();
                                    if (string.IsNullOrWhiteSpace(car.Model) && parts.Length > 1)
                                        car.Model = parts[1].Trim();
                                }
                            }
                        }
                    }

                    // Voeg alleen toe als merk en model bestaan EN waarden realistisch zijn
                    if (!string.IsNullOrWhiteSpace(car.Brand) && !string.IsNullOrWhiteSpace(car.Model))
                    {
                        // Filter onrealistische waarden
                        if (IsCarRealistic(car))
                        {
                            // Genereer ImageUrl voor belangrijke modellen
                            car.ImageUrl = GenerateImageUrl(car);
                            cars.Add(car);
                        }
                        else
                        {
                            // Stil overgeslagen - te veel log output anders
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Rij {rowNumber} overgeslagen: ontbrekende merk of model.");
                    }
                }
                catch (Exception ex)
                {
                    // Fout opgetreden, skip deze rij
                    Console.WriteLine($"Rij {rowNumber} overgeslagen: {ex.Message}");
                }
            }
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine($"Fout: Bestand niet gevonden: {csvPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fout bij het lezen van het CSV-bestand: {ex.Message}");
        }

        return cars;
    }

    /// <summary>
    /// Genereert image paths voor alle auto's.
    /// 
    /// Waarom geen web scraping:
    /// - Copyright issues (afbeeldingen zijn beschermd)
    /// - Terms of Service schending
    /// - Juridische risico's
    /// 
    /// Gebruik in plaats daarvan legale bronnen:
    /// - Eigen foto's, stock foto's met licentie (Unsplash, Pexels)
    /// - Officiële media kits van fabrikanten
    /// 
    /// Images worden handmatig geplaatst in images/{brand}/{model}/ mapstructuur.
    /// </summary>
    public void AssignImagePaths(List<Car> cars)
    {
        foreach (Car car in cars)
        {
            // Als er al een path is, skip deze auto
            if (!string.IsNullOrWhiteSpace(car.ImagePath) && 
                !car.ImagePath.Equals("None", StringComparison.OrdinalIgnoreCase))
            {
                // Zorg dat ImageUrl ook gezet is, zelfs als ImagePath al bestaat
                if (string.IsNullOrWhiteSpace(car.ImageUrl))
                {
                    car.ImageUrl = GenerateImageUrl(car);
                }
                continue; // Al een path, skip rest
            }

            // Maak veilige bestandsnamen (verwijder illegale tekens)
            string cleanBrand = SanitizeFileName(car.Brand);
            string cleanModel = SanitizeFileName(car.Model);
            
            // Genereer pad: images/brand/model/id.jpg
            car.ImagePath = $"images/{cleanBrand}/{cleanModel}/{car.Id}.jpg";
            
            // Check of lokale afbeelding bestaat (van Kaggle dataset)
            string localImagePath = Path.Combine(Directory.GetCurrentDirectory(), car.ImagePath);
            if (File.Exists(localImagePath))
            {
                // Gebruik lokale afbeelding via relatieve URL (voor web server)
                car.ImageUrl = $"/{car.ImagePath.Replace('\\', '/')}";
            }
            else
            {
                // Probeer Kaggle image te vinden (andere naamgeving)
                string kaggleImagePath = FindKaggleImage(car);
                if (!string.IsNullOrEmpty(kaggleImagePath))
                {
                    car.ImageUrl = kaggleImagePath;
                }
                else
                {
                    // Genereer externe ImageUrl als fallback
                    car.ImageUrl = GenerateImageUrl(car);
                }
            }
        }
    }

    /// <summary>
    /// Genereert een ImageUrl voor een auto op basis van merk en model.
    /// Gebruikt Auto-Data.net voor echte auto-afbeeldingen waar mogelijk.
    /// </summary>
    private string GenerateImageUrl(Car car)
    {
        if (string.IsNullOrWhiteSpace(car.Brand) || string.IsNullOrWhiteSpace(car.Model))
        {
            return string.Empty;
        }

        // Maak een veilige search query voor auto-afbeeldingen
        string safeBrand = car.Brand.Trim().Replace(" ", "-").Replace("&", "and").ToLower();
        string safeModel = car.Model.Trim().Replace(" ", "-").Replace("&", "and").ToLower();
        
        // Gebruik een deterministische seed op basis van merk+model voor consistente fallback afbeeldingen
        int seed = Math.Abs((car.Brand + car.Model).GetHashCode());
        
        // Optie 1: Probeer Auto-Data.net image URL structuur
        // Auto-Data.net heeft een image database, maar de exacte URL structuur is niet publiekelijk bekend
        // We kunnen proberen een URL te construeren op basis van merk en model
        // Format: https://www.auto-data.net/images/{brand}/{model}/...
        // Dit is een gok, maar het kan werken voor veel modellen
        
        // Probeer eerst een directe Auto-Data.net image URL
        string autoDataUrl = $"https://www.auto-data.net/images/{safeBrand}/{safeModel}.jpg";
        
        // Als alternatief kunnen we ook een search URL gebruiken die naar de auto pagina linkt
        // Dit geeft de gebruiker toegang tot de auto-data.net pagina met afbeeldingen
        // Maar voor nu gebruiken we een fallback naar Picsum Photos met seed voor consistente afbeeldingen
        
        // Fallback: Gebruik Lorem Picsum met seed voor consistente afbeeldingen
        // Dit geeft altijd een werkende afbeelding (maar niet specifiek auto's)
        // De frontend kan dit gebruiken als fallback als Auto-Data.net URL niet werkt
        return $"https://picsum.photos/seed/{seed}/400/300";
        
        // TOEKOMSTIGE VERBETERING:
        // 1. Gebruik Auto-Data.net API (vereist API key/offerte) voor echte auto foto's
        // 2. Of gebruik Pexels API (gratis) voor auto foto's
        // 3. Cache de resultaten om API calls te beperken
    }

    /// <summary>
    /// Controleert of een auto realistische waarden heeft.
    /// Filtert onrealistische prijzen, vermogens en bouwjaren.
    /// </summary>
    private bool IsCarRealistic(Car car)
    {
        // Realistische grenzen
        // Prijs: €300 minimum (kleine tweedehands auto's kunnen zo laag zijn), 
        //        maar filter duidelijk conversiefouten (< €10)
        const decimal MIN_REALISTIC_PRICE = 300;
        const decimal MAX_REALISTIC_PRICE = 500000;
        
        // Vermogen: 20 KW minimum (kleine elektrische auto's), 
        //           800 KW maximum (extreem hoge waarden zijn parsing fouten)
        const int MIN_REALISTIC_POWER = 20;  // KW - zeer lage maar nog realistische stadsauto's/elektrische
        const int MAX_REALISTIC_POWER = 800;  // KW - zeer hoge maar nog realistische sportwagens
        
        const int MIN_REALISTIC_YEAR = 1990;
        const int MAX_REALISTIC_YEAR = 2025;

        // Budget check
        if (car.Budget < MIN_REALISTIC_PRICE || car.Budget > MAX_REALISTIC_PRICE)
        {
            return false;
        }

        // Vermogen check (0 betekent meestal missing data)
        if (car.Power < MIN_REALISTIC_POWER || car.Power > MAX_REALISTIC_POWER)
        {
            return false;
        }

        // Bouwjaar check
        if (car.Year < MIN_REALISTIC_YEAR || car.Year > MAX_REALISTIC_YEAR)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Zoekt een Kaggle image op basis van merk en model.
    /// Kaggle images hebben naamgeving: Brand_Model_Year_... (bijv. Acura_ILX_2013_...)
    /// </summary>
    private string FindKaggleImage(Car car)
    {
        if (string.IsNullOrWhiteSpace(car.Brand) || string.IsNullOrWhiteSpace(car.Model))
            return string.Empty;

        try
        {
            // Probeer verschillende mogelijke image directories
            string[] possibleDirs = new[]
            {
                Path.Combine(Directory.GetCurrentDirectory(), "backend", "images"),
                Path.Combine(Directory.GetCurrentDirectory(), "images"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "backend", "images"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "images")
            };

            string? imagesDir = null;
            foreach (var dir in possibleDirs)
            {
                if (Directory.Exists(dir))
                {
                    imagesDir = dir;
                    break;
                }
            }

            if (string.IsNullOrEmpty(imagesDir))
                return string.Empty;

            // Normaliseer merk en model voor matching
            string brandNormalized = NormalizeForImageSearch(car.Brand);
            string modelNormalized = NormalizeForImageSearch(car.Model);

            // Get all image files
            var allImages = Directory.GetFiles(imagesDir, "*.jpg", SearchOption.TopDirectoryOnly);
            
            // Zoek naar best match
            string bestMatch = string.Empty;
            int bestScore = 0;

            foreach (var imagePath in allImages)
            {
                string fileName = Path.GetFileNameWithoutExtension(imagePath);
                string[] parts = fileName.Split('_');
                
                if (parts.Length < 2)
                    continue;

                string imageBrand = NormalizeForImageSearch(parts[0]);
                string imageModel = NormalizeForImageSearch(parts[1]);

                int score = 0;
                
                // Exact brand match
                if (imageBrand == brandNormalized)
                    score += 10;
                else if (imageBrand.Contains(brandNormalized) || brandNormalized.Contains(imageBrand))
                    score += 5;

                // Exact model match
                if (imageModel == modelNormalized)
                    score += 10;
                else if (imageModel.Contains(modelNormalized) || modelNormalized.Contains(imageModel))
                    score += 5;

                // Year match (bonus)
                if (parts.Length > 2 && int.TryParse(parts[2], out int imageYear))
                {
                    if (car.Year > 0 && Math.Abs(imageYear - car.Year) <= 2)
                        score += 2;
                }

                if (score > bestScore && score >= 10) // Minimaal brand + model match nodig
                {
                    bestScore = score;
                    bestMatch = imagePath;
                }
            }

            if (!string.IsNullOrEmpty(bestMatch))
            {
                // Converteer naar relatieve URL voor web server
                // Images moeten beschikbaar zijn via /images/...
                string fileName = Path.GetFileName(bestMatch);
                
                // Als image in backend/images staat, gebruik /images/...
                if (bestMatch.Contains("backend" + Path.DirectorySeparatorChar + "images"))
                {
                    return $"/images/{fileName}";
                }
                else
                {
                    // Probeer relatieve path
                    string relativePath = Path.GetRelativePath(Directory.GetCurrentDirectory(), bestMatch);
                    return $"/{relativePath.Replace('\\', '/')}";
                }
            }
        }
        catch (Exception)
        {
            // Stil falen, gebruik fallback
        }

        return string.Empty;
    }

    /// <summary>
    /// Normaliseert merk/model naam voor image matching.
    /// </summary>
    private string NormalizeForImageSearch(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        // Lowercase, trim, verwijder speciale tekens
        string normalized = name.ToLower().Trim();
        
        // Vervang spaties en streepjes met underscores
        normalized = normalized.Replace(" ", "_").Replace("-", "_");
        
        // Verwijder speciale tekens behalve underscores
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"[^a-z0-9_]", "");
        
        return normalized;
    }

    /// <summary>
    /// Maakt een veilige bestandsnaam (verwijdert illegale tekens).
    /// </summary>
    private string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return "unknown";

        // Converteer naar lowercase voor consistentie
        string sanitized = fileName.ToLower().Trim();
        
        // Vervang illegale tekens door underscore
        char[] illegalChars = Path.GetInvalidFileNameChars();
        foreach (char c in illegalChars)
        {
            sanitized = sanitized.Replace(c, '_');
        }
        
        // Spaties -> underscores
        sanitized = sanitized.Replace(' ', '_');
        
        // Vervang meerdere underscores door één
        while (sanitized.Contains("__"))
        {
            sanitized = sanitized.Replace("__", "_");
        }
        
        // Verwijder leading/trailing underscores
        sanitized = sanitized.Trim('_');
        
        // Fallback als naam leeg is
        if (string.IsNullOrWhiteSpace(sanitized))
            sanitized = "unknown";
        
        return sanitized;
    }

    /// <summary>
    /// Parse CSV regel correct (handelt komma's binnen quotes af).
    /// </summary>
    private string[] ParseCsvLine(string line)
    {
        List<string> result = new List<string>();
        bool inQuotes = false;
        string currentField = "";

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                // Toggle tussen in/uit quotes
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                // Komma buiten quotes = nieuwe kolom
                result.Add(currentField);
                currentField = "";
            }
            else
            {
                currentField += c;
            }
        }

        // Laatste veld toevoegen
        result.Add(currentField);

        return result.ToArray();
    }

    /// <summary>
    /// Helper methode: Zoekt de index van een kolom op basis van mogelijke kolomnamen (case-insensitive)
    /// </summary>
    private int FindColumnIndex(string[] headers, string[] possibleNames)
    {
        for (int i = 0; i < headers.Length; i++)
        {
            string header = headers[i].Trim().ToLower();
            foreach (string name in possibleNames)
            {
                if (header.Contains(name.ToLower()))
                {
                    return i;
                }
            }
        }
        return -1;
    }

    /// <summary>
    /// Zoekt CSV bestand in data directory (werkt ook vanuit verschillende start directories).
    /// </summary>
    public string FindCsvFile(string fileName, string? dataDirectory = null)
    {
        // Standaard data directory: twee levels omhoog vanaf executable, dan data/
        if (string.IsNullOrEmpty(dataDirectory))
        {
            string? execDir = AppDomain.CurrentDomain.BaseDirectory;
            string? projectRoot = Directory.GetParent(execDir)?.Parent?.Parent?.FullName;
            if (projectRoot != null)
            {
                dataDirectory = Path.Combine(projectRoot, "data");
            }
            else
            {
                dataDirectory = "data";
            }
        }

        // Probeer direct pad
        string directPath = Path.Combine(dataDirectory, fileName);
        if (File.Exists(directPath))
        {
            return directPath;
        }

        // Probeer ook vanuit huidige directory
        string currentPath = Path.Combine(Directory.GetCurrentDirectory(), "data", fileName);
        if (File.Exists(currentPath))
        {
            return currentPath;
        }

        // Laatste poging: zoek omhoog in directory tree
        string? searchDir = Directory.GetCurrentDirectory();
        for (int i = 0; i < 5 && searchDir != null; i++)
        {
            string testPath = Path.Combine(searchDir, "data", fileName);
            if (File.Exists(testPath))
            {
                return testPath;
            }
            searchDir = Directory.GetParent(searchDir)?.FullName;
        }

        return string.Empty;
    }

    /// <summary>
    /// Filter auto's op basis van verschillende criteria.
    /// Implementeert ICarRepository.FilterCars - gebruikt GetAllCars() intern.
    /// </summary>
    public List<Car> FilterCars(
        string? brand = null,
        int? minBudget = null, int? maxBudget = null,
        int? minPower = null, int? maxPower = null,
        int? minYear = null, int? maxYear = null,
        string? fuel = null)
    {
        var filtered = GetAllCars().AsEnumerable();

        if (!string.IsNullOrWhiteSpace(brand))
        {
            filtered = filtered.Where(c => c.Brand.Equals(brand, StringComparison.OrdinalIgnoreCase));
        }

        if (minBudget.HasValue)
        {
            filtered = filtered.Where(c => c.Budget >= minBudget.Value);
        }

        if (maxBudget.HasValue)
        {
            filtered = filtered.Where(c => c.Budget <= maxBudget.Value);
        }

        if (minPower.HasValue)
        {
            filtered = filtered.Where(c => c.Power >= minPower.Value);
        }

        if (maxPower.HasValue)
        {
            filtered = filtered.Where(c => c.Power <= maxPower.Value);
        }

        if (minYear.HasValue)
        {
            filtered = filtered.Where(c => c.Year >= minYear.Value);
        }

        if (maxYear.HasValue)
        {
            filtered = filtered.Where(c => c.Year <= maxYear.Value);
        }

        if (!string.IsNullOrWhiteSpace(fuel))
        {
            filtered = filtered.Where(c => c.Fuel.Contains(fuel, StringComparison.OrdinalIgnoreCase));
        }

        return filtered.ToList();
    }

    /// <summary>
    /// Haalt alle images op voor een specifieke auto op basis van merk, model en jaar.
    /// Retourneert een lijst van image URLs die matchen met de auto.
    /// </summary>
    public List<string> GetCarImages(Car car)
    {
        if (string.IsNullOrWhiteSpace(car.Brand) || string.IsNullOrWhiteSpace(car.Model))
            return new List<string>();

        var imageUrls = new List<string>();

        try
        {
            // Probeer verschillende mogelijke image directories
            string[] possibleDirs = new[]
            {
                Path.Combine(Directory.GetCurrentDirectory(), "backend", "images"),
                Path.Combine(Directory.GetCurrentDirectory(), "images"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "backend", "images"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "images")
            };

            string? imagesDir = null;
            foreach (var dir in possibleDirs)
            {
                if (Directory.Exists(dir))
                {
                    imagesDir = dir;
                    break;
                }
            }

            if (string.IsNullOrEmpty(imagesDir))
                return imageUrls;

            // Normaliseer merk en model voor matching
            string brandNormalized = NormalizeForImageSearch(car.Brand);
            string modelNormalized = NormalizeForImageSearch(car.Model);

            // Get all image files
            var allImages = Directory.GetFiles(imagesDir, "*.jpg", SearchOption.TopDirectoryOnly);

            foreach (var imagePath in allImages)
            {
                string fileName = Path.GetFileNameWithoutExtension(imagePath);
                string[] parts = fileName.Split('_');

                if (parts.Length < 2)
                    continue;

                string imageBrand = NormalizeForImageSearch(parts[0]);
                string imageModel = NormalizeForImageSearch(parts[1]);

                bool brandMatches = imageBrand == brandNormalized || 
                                   imageBrand.Contains(brandNormalized) || 
                                   brandNormalized.Contains(imageBrand);
                
                bool modelMatches = imageModel == modelNormalized || 
                                   imageModel.Contains(modelNormalized) || 
                                   modelNormalized.Contains(imageModel);

                // Als brand en model matchen, voeg toe aan lijst
                if (brandMatches && modelMatches)
                {
                    string fileNameOnly = Path.GetFileName(imagePath);
                    string imageUrl = $"/images/{fileNameOnly}";
                    imageUrls.Add(imageUrl);
                }
            }

            // Sorteer op jaar match (images met jaar match eerst)
            imageUrls = imageUrls.OrderByDescending(url =>
            {
                string fileName = Path.GetFileNameWithoutExtension(url);
                string[] parts = fileName.Split('_');
                if (parts.Length > 2 && int.TryParse(parts[2], out int imageYear))
                {
                    if (car.Year > 0 && Math.Abs(imageYear - car.Year) <= 2)
                        return 1;
                }
                return 0;
            }).ToList();
        }
        catch (Exception)
        {
            // Stil falen, retourneer lege lijst
        }

        return imageUrls;
    }
}



