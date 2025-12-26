using System.Globalization;
using System.Text.Json;

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
    
    // PERFORMANCE FIX: Index van foto's voor snelle lookup
    // Dictionary key: "brand_model" (genormaliseerd), value: lijst van bestandsnamen
    // Dit voorkomt dat we voor elke auto door alle 49,341 foto's moeten loopen
    private Dictionary<string, List<string>>? _imageIndex = null;
    private string? _imagesDirectory = null;

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

        // PERFORMANCE FIX: Laad permanente image mapping (als die bestaat)
        // Dit voorkomt dat we bij elke start opnieuw moeten matchen
        string mappingPath = GetImageMappingPath();
        Dictionary<int, string>? imageMapping = LoadImageMapping(mappingPath);
        
        if (imageMapping != null && imageMapping.Count > 0)
        {
            // Mapping bestaat - gebruik deze direct
            Console.WriteLine($"[LoadCarsFromCsv] Permanente image mapping geladen: {imageMapping.Count} auto's hebben ImageUrl");
            ApplyImageMapping(_cars, imageMapping);
        }
        else
        {
            // Geen mapping gevonden - genereer nieuwe mapping en sla op
            Console.WriteLine($"[LoadCarsFromCsv] Geen permanente mapping gevonden - genereer nieuwe mapping...");
            AssignImagePaths(_cars);
            SaveImageMapping(_cars, mappingPath);
        }
        
        // Statistieken loggen
        int carsWithImages = _cars.Count(c => !string.IsNullOrEmpty(c.ImageUrl));
        Console.WriteLine($"Image matching resultaten:");
        Console.WriteLine($"  Totaal auto's: {_cars.Count}");
        Console.WriteLine($"  Auto's met foto: {carsWithImages} ({(carsWithImages * 100.0 / _cars.Count):F1}%)");

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
                    // FIX: Als ID kolom bestaat maar waarde is 0 of leeg, gebruik rijnummer als fallback
                    // Dit voorkomt dat alle auto's ID 0 krijgen (wat routing problemen veroorzaakt)
                    if (idIndex >= 0 && idIndex < columns.Length)
                    {
                        string? idValue = columns[idIndex]?.Trim();
                        if (!string.IsNullOrWhiteSpace(idValue) && 
                            int.TryParse(idValue, out int id) && id > 0)
                        {
                            car.Id = id;
                        }
                        else
                        {
                            // ID kolom bestaat maar waarde is leeg/0/invalid - gebruik rijnummer
                            car.Id = i;
                        }
                    }
                    else
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
                            
                            // Als Image_Path een bestandsnaam bevat met $$ separator (bijv. "Lexus$$RX 350$$2007$$Black$$48_24$$6$$image_0.jpg"),
                            // parse dit om merk en model te extraheren
                            if (imagePath.Contains("$$"))
                            {
                                string[] parts = imagePath.Split(new[] { "$$" }, StringSplitOptions.RemoveEmptyEntries);
                                if (parts.Length >= 2)
                                {
                                    // Gebruik merk en model uit Image_name voor betere image URL
                                    // Maar gebruik alleen als Brand/Model nog niet gezet zijn
                                    if (string.IsNullOrWhiteSpace(car.Brand) && parts.Length > 0)
                                        car.Brand = parts[0].Trim();
                                    if (string.IsNullOrWhiteSpace(car.Model) && parts.Length > 1)
                                        car.Model = parts[1].Trim();
                                    
                                    // Sla de Image_Path op zodat FindKaggleImage deze kan gebruiken
                                    // Format: "Lexus$$RX 350$$2007$$..." -> zoek naar "Lexus_RX_350" of "Lexus_RX350" in image bestanden
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
        int matched = 0;
        int skipped = 0;
        
        // PERFORMANCE FIX: Indexeer alle foto's ÉÉN KEER aan het begin
        // Dit voorkomt dat we voor elke auto door alle 49,341 foto's moeten loopen
        Console.WriteLine($"[AssignImagePaths] Start - {cars.Count} auto's");
        Console.WriteLine($"[AssignImagePaths] Indexeren van foto's...");
        
        BuildImageIndex();
        
        if (_imageIndex == null || _imageIndex.Count == 0)
        {
            Console.WriteLine($"[AssignImagePaths] WAARSCHUWING: Geen foto's geïndexeerd! Alle auto's krijgen lege ImageUrl.");
            foreach (Car car in cars)
            {
                car.ImageUrl = string.Empty;
            }
            return;
        }
        
        Console.WriteLine($"[AssignImagePaths] Foto index gebouwd: {_imageIndex.Count} unieke brand_model combinaties gevonden");
        
        foreach (Car car in cars)
        {
            // Skip als geen merk of model
            if (string.IsNullOrWhiteSpace(car.Brand) || string.IsNullOrWhiteSpace(car.Model))
            {
                skipped++;
                car.ImageUrl = string.Empty;
                continue;
            }
            
            // Stap 1: Als ImagePath uit CSV bestaat, probeer deze eerst te gebruiken
            if (!string.IsNullOrWhiteSpace(car.ImagePath) && 
                !car.ImagePath.Equals("None", StringComparison.OrdinalIgnoreCase))
            {
                string kaggleImagePath = FindKaggleImageByPath(car);
                if (!string.IsNullOrEmpty(kaggleImagePath))
                {
                    car.ImageUrl = kaggleImagePath;
                    matched++;
                    continue;
                }
            }
            
            // PERFORMANCE FIX: Stap 2 - Gebruik index voor snelle lookup
            // In plaats van door alle 49,341 foto's te loopen, gebruiken we de index
            string kaggleImagePathByMatch = FindKaggleImageUsingIndex(car);
            if (!string.IsNullOrEmpty(kaggleImagePathByMatch))
            {
                car.ImageUrl = kaggleImagePathByMatch;
                matched++;
            }
            else
            {
                // Geen image gevonden - lege string (frontend gebruikt SVG fallback)
                car.ImageUrl = string.Empty;
            }
        }
        
        Console.WriteLine($"Image matching resultaten:");
        Console.WriteLine($"  Totaal auto's: {cars.Count}");
        Console.WriteLine($"  Auto's met foto: {matched} ({(matched * 100.0 / cars.Count):F1}%)");
        Console.WriteLine($"  Auto's zonder foto: {cars.Count - matched} ({(cars.Count - matched) * 100.0 / cars.Count:F1}%)");
    }

    /// <summary>
    /// Genereert een ImageUrl voor een auto op basis van merk en model.
    /// GEEN fallback naar externe services - retourneert lege string als geen image gevonden.
    /// De frontend gebruikt dan een professionele SVG icon fallback.
    /// </summary>
    private string GenerateImageUrl(Car car)
    {
        // GEEN willekeurige foto's meer - retourneer lege string
        // De frontend zal dan de SVG icon fallback gebruiken
        return string.Empty;
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
    /// PERFORMANCE FIX: Bouwt een index van alle foto's voor snelle lookup.
    /// Indexeert foto's op basis van genormaliseerde brand_model combinaties.
    /// Dit voorkomt dat we voor elke auto door alle 49,341 foto's moeten loopen.
    /// </summary>
    private void BuildImageIndex()
    {
        if (_imageIndex != null)
        {
            // Index al gebouwd, skip
            return;
        }
        
        _imageIndex = new Dictionary<string, List<string>>();
        
        // Zoek images directory (gebruik zelfde logica als FindKaggleImage)
        string? imagesDir = FindImagesDirectory();
        
        if (string.IsNullOrEmpty(imagesDir))
        {
            Console.WriteLine($"[BuildImageIndex] GEEN images directory gevonden!");
            return;
        }
        
        _imagesDirectory = imagesDir;
        
        Console.WriteLine($"[BuildImageIndex] Indexeren van foto's in: {imagesDir}");
        
        // Haal alle foto's op
        var allImages = Directory.GetFiles(imagesDir, "*.jpg", SearchOption.TopDirectoryOnly);
        Console.WriteLine($"[BuildImageIndex] {allImages.Length} foto's gevonden, indexeren...");
        
        int indexedCount = 0;
        
        foreach (var imagePath in allImages)
        {
            string fileName = Path.GetFileNameWithoutExtension(imagePath);
            string fileNameNormalized = fileName
                .ToLowerInvariant()
                .Replace("_", "")
                .Replace("-", "")
                .Replace(" ", "");
            
            // Extract brand en model uit bestandsnaam
            // Bestandsnamen zijn zoals "Acura_MDX_2011..." -> "Acura" en "MDX"
            string[] parts = fileName.Split('_');
            if (parts.Length >= 2)
            {
                string brand = parts[0].Trim();
                string model = parts[1].Trim();
                
                // Normaliseer brand en model voor index key
                string brandNormalized = brand
                    .ToLowerInvariant()
                    .Replace(" ", "")
                    .Replace("-", "")
                    .Replace("_", "");
                
                string modelNormalized = model
                    .ToLowerInvariant()
                    .Replace(" ", "")
                    .Replace("-", "")
                    .Replace("_", "");
                
                if (!string.IsNullOrEmpty(brandNormalized) && !string.IsNullOrEmpty(modelNormalized))
                {
                    // Maak index key: "brand_model"
                    string indexKey = $"{brandNormalized}_{modelNormalized}";
                    
                    // Voeg toe aan index
                    if (!_imageIndex.ContainsKey(indexKey))
                    {
                        _imageIndex[indexKey] = new List<string>();
                    }
                    
                    _imageIndex[indexKey].Add(Path.GetFileName(imagePath));
                    indexedCount++;
                }
            }
        }
        
        Console.WriteLine($"[BuildImageIndex] Index gebouwd: {_imageIndex.Count} unieke brand_model combinaties, {indexedCount} foto's geïndexeerd");
    }
    
    /// <summary>
    /// PERFORMANCE FIX: Zoekt foto's met behulp van de index in plaats van door alle foto's te loopen.
    /// Dit is VEEL sneller: O(1) lookup in plaats van O(n) voor elke auto.
    /// </summary>
    private string FindKaggleImageUsingIndex(Car car)
    {
        if (_imageIndex == null || _imageIndex.Count == 0)
        {
            return string.Empty;
        }
        
        if (string.IsNullOrWhiteSpace(car.Brand) || string.IsNullOrWhiteSpace(car.Model))
        {
            return string.Empty;
        }
        
        // Normaliseer brand en model op dezelfde manier als in BuildImageIndex
        string brandNormalized = (car.Brand ?? "")
            .ToLowerInvariant()
            .Replace(" ", "")
            .Replace("-", "")
            .Replace("_", "")
            .Trim();
        
        string modelNormalized = (car.Model ?? "")
            .ToLowerInvariant()
            .Replace(" ", "")
            .Replace("-", "")
            .Replace("_", "")
            .Trim();
        
        if (string.IsNullOrEmpty(brandNormalized) || string.IsNullOrEmpty(modelNormalized))
        {
            return string.Empty;
        }
        
        // Zoek in index: probeer exacte match eerst
        string indexKey = $"{brandNormalized}_{modelNormalized}";
        
        if (_imageIndex.ContainsKey(indexKey) && _imageIndex[indexKey].Count > 0)
        {
            // MATCH GEVONDEN! Gebruik eerste foto uit de lijst
            string fileName = _imageIndex[indexKey][0];
            return $"/images/{fileName}";
        }
        
        // Als exacte match niet werkt, probeer partial matching
        // Bijvoorbeeld: "bmwx5" vs "bmw_x5" -> beide normaliseren naar "bmwx5"
        // Maar dit zou al moeten werken met de huidige normalisatie...
        
        // Probeer alternatieve keys (bijv. zonder underscore)
        string altKey = $"{brandNormalized}{modelNormalized}";
        foreach (var kvp in _imageIndex)
        {
            if (kvp.Key.Contains(brandNormalized) && kvp.Key.Contains(modelNormalized))
            {
                if (kvp.Value.Count > 0)
                {
                    string fileName = kvp.Value[0];
                    return $"/images/{fileName}";
                }
            }
        }
        
        return string.Empty;
    }
    
    /// <summary>
    /// Helper methode om images directory te vinden (gebruikt door BuildImageIndex en FindKaggleImage).
    /// </summary>
    private string? FindImagesDirectory()
    {
        // Bepaal project root door te zoeken naar bekende directories (data, backend, etc.)
        string? projectRoot = null;
        string currentDir = Directory.GetCurrentDirectory();
        
        // Zoek project root door omhoog te gaan tot we data/ of backend/ directory vinden
        DirectoryInfo? dir = new DirectoryInfo(currentDir);
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "data")) || 
                Directory.Exists(Path.Combine(dir.FullName, "backend")))
            {
                projectRoot = dir.FullName;
                break;
            }
            dir = dir.Parent;
        }
        
        // Probeer verschillende mogelijke image directories
        List<string> possibleDirs = new List<string>();
        
        // Van project root (als gevonden)
        if (!string.IsNullOrEmpty(projectRoot))
        {
            possibleDirs.Add(Path.Combine(projectRoot, "backend", "images"));
            possibleDirs.Add(Path.Combine(projectRoot, "images"));
        }
        
        // Van current directory
        possibleDirs.Add(Path.Combine(currentDir, "backend", "images"));
        possibleDirs.Add(Path.Combine(currentDir, "images"));
        possibleDirs.Add(Path.Combine(currentDir, "..", "backend", "images"));
        possibleDirs.Add(Path.Combine(currentDir, "..", "..", "backend", "images"));
        
        // Van AppDomain BaseDirectory (runtime directory)
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        possibleDirs.Add(Path.Combine(baseDir, "backend", "images"));
        possibleDirs.Add(Path.Combine(baseDir, "images"));
        possibleDirs.Add(Path.Combine(baseDir, "..", "backend", "images"));
        possibleDirs.Add(Path.Combine(baseDir, "..", "..", "backend", "images"));
        
        // Absolute pad als fallback (voor Azure deployment)
        possibleDirs.Add(@"C:\Users\runed\OneDrive - Thomas More\Recommendation_System_New\backend\images");

        foreach (var dirPath in possibleDirs)
        {
            try
            {
                string normalizedPath = Path.GetFullPath(dirPath);
                
                if (Directory.Exists(normalizedPath))
                {
                    var jpgFiles = Directory.GetFiles(normalizedPath, "*.jpg", SearchOption.TopDirectoryOnly);
                    var jpgFilesUpper = Directory.GetFiles(normalizedPath, "*.JPG", SearchOption.TopDirectoryOnly);
                    var jpgFilesMixed = Directory.GetFiles(normalizedPath, "*.Jpg", SearchOption.TopDirectoryOnly);
                    
                    int totalJpgFiles = jpgFiles.Length + jpgFilesUpper.Length + jpgFilesMixed.Length;
                    
                    if (totalJpgFiles > 0)
                    {
                        return normalizedPath;
                    }
                }
            }
            catch
            {
                continue;
            }
        }
        
        return null;
    }

    /// <summary>
    /// Zoekt een Kaggle image op basis van merk en model (OUDE METHODE - langzaam).
    /// Kaggle images hebben naamgeving: Brand_Model_Year_... (bijv. Acura_ILX_2013_...)
    /// GEBRUIK FindKaggleImageUsingIndex() in plaats van deze methode voor betere performance!
    /// </summary>
    private string FindKaggleImage(Car car)
    {
        if (string.IsNullOrWhiteSpace(car.Brand) || string.IsNullOrWhiteSpace(car.Model))
            return string.Empty;

        try
        {
            // BUG FIX: Probeer verschillende mogelijke image directories
            // Het probleem was dat Directory.GetCurrentDirectory() niet altijd naar de juiste directory wijst
            // We proberen nu meer locaties, inclusief relatieve paden vanaf verschillende startpunten
            
            // Bepaal project root door te zoeken naar bekende directories (data, backend, etc.)
            string? projectRoot = null;
            string currentDir = Directory.GetCurrentDirectory();
            
            // Zoek project root door omhoog te gaan tot we data/ of backend/ directory vinden
            DirectoryInfo? dir = new DirectoryInfo(currentDir);
            while (dir != null)
            {
                if (Directory.Exists(Path.Combine(dir.FullName, "data")) || 
                    Directory.Exists(Path.Combine(dir.FullName, "backend")))
                {
                    projectRoot = dir.FullName;
                    break;
                }
                dir = dir.Parent;
            }
            
            // Probeer verschillende mogelijke image directories
            List<string> possibleDirs = new List<string>();
            
            // Van project root (als gevonden)
            if (!string.IsNullOrEmpty(projectRoot))
            {
                possibleDirs.Add(Path.Combine(projectRoot, "backend", "images"));
                possibleDirs.Add(Path.Combine(projectRoot, "images"));
            }
            
            // Van current directory
            possibleDirs.Add(Path.Combine(currentDir, "backend", "images"));
            possibleDirs.Add(Path.Combine(currentDir, "images"));
            possibleDirs.Add(Path.Combine(currentDir, "..", "backend", "images"));
            possibleDirs.Add(Path.Combine(currentDir, "..", "..", "backend", "images"));
            
            // Van AppDomain BaseDirectory (runtime directory)
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            possibleDirs.Add(Path.Combine(baseDir, "backend", "images"));
            possibleDirs.Add(Path.Combine(baseDir, "images"));
            possibleDirs.Add(Path.Combine(baseDir, "..", "backend", "images"));
            possibleDirs.Add(Path.Combine(baseDir, "..", "..", "backend", "images"));
            
            // Absolute pad als fallback (voor Azure deployment)
            possibleDirs.Add(@"C:\Users\runed\OneDrive - Thomas More\Recommendation_System_New\backend\images");

            string? imagesDir = null;
            foreach (var dirPath in possibleDirs)
            {
                try
                {
                    string normalizedPath = Path.GetFullPath(dirPath); // Normaliseer pad
                    
                    // BUG FIX: Case-insensitive directory check - probeer ook met verschillende case variaties
                    if (Directory.Exists(normalizedPath))
                    {
                        // BUG FIX: Check of er daadwerkelijk .jpg bestanden in zitten (case-insensitive)
                        // Directory.GetFiles is standaard case-insensitive op Windows, maar expliciet maken
                        var jpgFiles = Directory.GetFiles(normalizedPath, "*.jpg", SearchOption.TopDirectoryOnly);
                        var jpgFilesUpper = Directory.GetFiles(normalizedPath, "*.JPG", SearchOption.TopDirectoryOnly);
                        var jpgFilesMixed = Directory.GetFiles(normalizedPath, "*.Jpg", SearchOption.TopDirectoryOnly);
                        
                        int totalJpgFiles = jpgFiles.Length + jpgFilesUpper.Length + jpgFilesMixed.Length;
                        
                        if (totalJpgFiles > 0)
                        {
                            imagesDir = normalizedPath;
                            Console.WriteLine($"[FindKaggleImage] Images directory gevonden: {imagesDir} ({totalJpgFiles} .jpg bestanden gevonden)");
                            break;
                        }
                    }
                }
                catch
                {
                    // Skip invalid paths
                    continue;
                }
            }

            if (string.IsNullOrEmpty(imagesDir))
            {
                Console.WriteLine($"[FindKaggleImage] GEEN images directory gevonden! Gezocht in:");
                foreach (var dirPath in possibleDirs.Take(10)) // Log eerste 10 voor debugging
                {
                    Console.WriteLine($"  - {dirPath}");
                }
                return string.Empty;
            }

            // BUG FIX: Case-insensitive matching met robuuste normalisatie
            // Bestandsnamen zijn zoals "Acura_MDX_2011..." (met hoofdletters en underscores)
            // We normaliseren alles naar lowercase en verwijderen speciale tekens voor matching
            // Bijv: "BMW X5" -> "bmwx5", "Acura_MDX_2011..." -> "acuramdx2011..."
            
            // Normaliseer brand: lowercase, verwijder spaties, streepjes, underscores
            string brandNormalized = (car.Brand ?? "")
                .ToLowerInvariant()  // BUG FIX: ToLowerInvariant voor betere performance en consistentie
                .Replace(" ", "")
                .Replace("-", "")
                .Replace("_", "")
                .Trim();
            
            // Normaliseer model: lowercase, verwijder spaties, streepjes, underscores
            string modelNormalized = (car.Model ?? "")
                .ToLowerInvariant()  // BUG FIX: ToLowerInvariant voor betere performance
                .Replace(" ", "")
                .Replace("-", "")
                .Replace("_", "")
                .Trim();
            
            if (string.IsNullOrEmpty(brandNormalized) || string.IsNullOrEmpty(modelNormalized))
            {
                Console.WriteLine($"[FindKaggleImage] Lege brand of model: Brand='{car.Brand}', Model='{car.Model}'");
                return string.Empty;
            }
            
            // BUG FIX: Case-insensitive file search - gebruik SearchOption.AllDirectories NIET nodig, maar zorg dat we alle .jpg vinden
            // Directory.GetFiles is standaard case-insensitive op Windows, maar expliciet maken voor duidelijkheid
            var allImages = Directory.GetFiles(imagesDir, "*.jpg", SearchOption.TopDirectoryOnly);
            Console.WriteLine($"[FindKaggleImage] Zoeken: '{brandNormalized}' + '{modelNormalized}' in {allImages.Length} foto's (directory: {imagesDir})");
            
            // BUG FIX: Robuuste matching - normaliseer bestandsnaam op dezelfde manier als brand/model
            int checkedCount = 0;
            int matchCount = 0;
            
            foreach (var imagePath in allImages)
            {
                checkedCount++;
                
                // BUG FIX: Normaliseer bestandsnaam op EXACT dezelfde manier als brand/model
                // Bestandsnaam: "Acura_MDX_2011..." -> "acuramdx2011..."
                string fileName = Path.GetFileNameWithoutExtension(imagePath)
                    .ToLowerInvariant()  // BUG FIX: ToLowerInvariant voor consistentie
                    .Replace("_", "")   // Verwijder underscores
                    .Replace("-", "")   // Verwijder streepjes
                    .Replace(" ", "");   // Verwijder spaties
                
                // BUG FIX: Case-insensitive Contains check (ToLowerInvariant zorgt hiervoor)
                // Check of merk EN model beide voorkomen in genormaliseerde bestandsnaam
                // Bijv: "acuramdx2011..." bevat "acura" en "mdx"
                bool brandFound = fileName.Contains(brandNormalized, StringComparison.OrdinalIgnoreCase);
                bool modelFound = fileName.Contains(modelNormalized, StringComparison.OrdinalIgnoreCase);
                
                if (brandFound && modelFound)
                {
                    // MATCH GEVONDEN! Gebruik deze foto
                    string fileNameOnly = Path.GetFileName(imagePath);
                    Console.WriteLine($"[FindKaggleImage] MATCH #{++matchCount}: {car.Brand} {car.Model} -> {fileNameOnly}");
                    Console.WriteLine($"[FindKaggleImage]   Normalized: brand='{brandNormalized}', model='{modelNormalized}', filename='{fileName}'");
                    
                    // Gebruik eerste match
                    return $"/images/{fileNameOnly}";
                }
                
                // Log eerste paar checks voor debugging
                if (checkedCount <= 5)
                {
                    string originalFileName = Path.GetFileName(imagePath);
                    Console.WriteLine($"[FindKaggleImage] Check {checkedCount}: '{originalFileName}' -> normalized: '{fileName}' | brand: {brandFound} ('{brandNormalized}'), model: {modelFound} ('{modelNormalized}')");
                }
            }
            
            Console.WriteLine($"[FindKaggleImage] GEEN match voor {car.Brand} {car.Model} (gecheckt {checkedCount} foto's, brand='{brandNormalized}', model='{modelNormalized}')");
            return string.Empty;
        }
        catch (Exception ex)
        {
            // Log exception voor debugging
            Console.WriteLine($"[FindKaggleImage] Exception: {ex.Message}");
        }

        return string.Empty;
    }

    /// <summary>
    /// Zoekt een image op basis van ImagePath uit CSV.
    /// ImagePath formaat: "Lexus$$RX 350$$2007$$Black$$48_24$$6$$image_0.jpg"
    /// Zoekt naar bestanden die beginnen met "Lexus_RX350" of "Lexus_RX_350" etc.
    /// </summary>
    private string FindKaggleImageByPath(Car car)
    {
        if (string.IsNullOrWhiteSpace(car.ImagePath))
            return string.Empty;
            
        try
        {
            // Parse ImagePath: "Lexus$$RX 350$$2007$$..." -> merk="Lexus", model="RX 350"
            string[] parts = car.ImagePath.Split(new[] { "$$" }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
                return string.Empty;
                
            string brandFromPath = parts[0].Trim();
            string modelFromPath = parts[1].Trim();
            
            // Normaliseer voor matching
            string brandNormalized = NormalizeForImageSearch(brandFromPath);
            string modelNormalized = NormalizeForImageSearch(modelFromPath);
            
            // Zoek image directories
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
                
            // Zoek naar bestanden die beginnen met brand_model
            var allImages = Directory.GetFiles(imagesDir, "*.jpg", SearchOption.TopDirectoryOnly);
            
            foreach (var imagePath in allImages)
            {
                string fileName = Path.GetFileNameWithoutExtension(imagePath);
                string[] fileNameParts = fileName.Split('_');
                
                if (fileNameParts.Length < 2)
                    continue;
                    
                string imageBrand = NormalizeForImageSearch(fileNameParts[0]);
                string imageModel = NormalizeForImageSearch(fileNameParts[1]);
                
                // Exact match op brand + model
                if (imageBrand == brandNormalized && 
                    (imageModel == modelNormalized || 
                     imageModel.Contains(modelNormalized) || 
                     modelNormalized.Contains(imageModel)))
                {
                    // Match gevonden!
                    string fileNameOnly = Path.GetFileName(imagePath);
                    return $"/images/{fileNameOnly}";
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FindKaggleImageByPath] Exception: {ex.Message}");
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
    /// PERFORMANCE FIX: Bepaalt het pad naar het permanente image mapping bestand.
    /// Mapping wordt opgeslagen in data/car_image_mapping.json
    /// </summary>
    private string GetImageMappingPath()
    {
        // Zoek data directory (zelfde logica als FindCsvFile)
        string? dataDir = null;
        
        // Probeer verschillende locaties
        string[] possibleDirs = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), _dataDirectory),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _dataDirectory),
            Path.Combine(Directory.GetCurrentDirectory(), "..", _dataDirectory),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", _dataDirectory),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", _dataDirectory),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", _dataDirectory)
        };
        
        foreach (var dir in possibleDirs)
        {
            if (Directory.Exists(dir))
            {
                dataDir = dir;
                break;
            }
        }
        
        // Als data directory niet gevonden, gebruik current directory
        if (string.IsNullOrEmpty(dataDir))
        {
            dataDir = Directory.GetCurrentDirectory();
        }
        
        return Path.Combine(dataDir, "car_image_mapping.json");
    }
    
    /// <summary>
    /// PERFORMANCE FIX: Laadt permanente image mapping uit JSON bestand.
    /// Retourneert null als bestand niet bestaat.
    /// </summary>
    private Dictionary<int, string>? LoadImageMapping(string mappingPath)
    {
        try
        {
            if (!File.Exists(mappingPath))
            {
                return null;
            }
            
            Console.WriteLine($"[LoadImageMapping] Laden van mapping: {mappingPath}");
            string jsonContent = File.ReadAllText(mappingPath);
            
            // Deserialize JSON naar Dictionary<int, string>
            var mapping = JsonSerializer.Deserialize<Dictionary<int, string>>(jsonContent);
            
            if (mapping != null)
            {
                Console.WriteLine($"[LoadImageMapping] Mapping geladen: {mapping.Count} auto's");
            }
            
            return mapping;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LoadImageMapping] Fout bij laden van mapping: {ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// PERFORMANCE FIX: Slaat permanente image mapping op naar JSON bestand.
    /// Dit voorkomt dat we bij elke start opnieuw moeten matchen.
    /// </summary>
    private void SaveImageMapping(List<Car> cars, string mappingPath)
    {
        try
        {
            // Maak dictionary: carId -> ImageUrl
            Dictionary<int, string> mapping = new Dictionary<int, string>();
            
            foreach (var car in cars)
            {
                if (!string.IsNullOrEmpty(car.ImageUrl))
                {
                    mapping[car.Id] = car.ImageUrl;
                }
            }
            
            // Serialize naar JSON
            var options = new JsonSerializerOptions
            {
                WriteIndented = true, // Leesbaar formaat
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            
            string jsonContent = JsonSerializer.Serialize(mapping, options);
            
            // Zorg dat directory bestaat
            string? directory = Path.GetDirectoryName(mappingPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            // Schrijf naar bestand
            File.WriteAllText(mappingPath, jsonContent);
            
            Console.WriteLine($"[SaveImageMapping] Mapping opgeslagen: {mapping.Count} auto's -> {mappingPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SaveImageMapping] Fout bij opslaan van mapping: {ex.Message}");
        }
    }
    
    /// <summary>
    /// PERFORMANCE FIX: Past permanente image mapping toe op auto's.
    /// </summary>
    private void ApplyImageMapping(List<Car> cars, Dictionary<int, string> mapping)
    {
        int applied = 0;
        
        foreach (var car in cars)
        {
            if (mapping.TryGetValue(car.Id, out string? imageUrl))
            {
                car.ImageUrl = imageUrl ?? string.Empty;
                if (!string.IsNullOrEmpty(imageUrl))
                {
                    applied++;
                }
            }
            else
            {
                // Geen mapping voor deze auto - lege ImageUrl
                car.ImageUrl = string.Empty;
            }
        }
        
        Console.WriteLine($"[ApplyImageMapping] Mapping toegepast: {applied} auto's hebben ImageUrl");
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



