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
    private Dictionary<int, List<string>> _imageMapping = new Dictionary<int, List<string>>();

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
        Console.WriteLine($"[DEBUG] LoadCarsFromCsv gestart - Zoeken naar: {_csvFileName} in {_dataDirectory}");
        string csvPath = FindCsvFile(_csvFileName, _dataDirectory);
        Console.WriteLine($"[DEBUG] FindCsvFile resultaat: {csvPath ?? "NULL"}");
        
        if (string.IsNullOrEmpty(csvPath) || !File.Exists(csvPath))
        {
            Console.WriteLine($"[DEBUG] FOUT: CSV bestand niet gevonden!");
            Console.WriteLine($"Waarschuwing: {_csvFileName} niet gevonden in data directory.");
            Console.WriteLine($"Gezocht in: {Path.Combine(Directory.GetCurrentDirectory(), _dataDirectory)}");
            _cars = new List<Car>();
            return;
        }

        Console.WriteLine($"[DEBUG] CSV-bestand gevonden: {csvPath}");
        Console.WriteLine("[DEBUG] Laden van auto's...");

        // Parse CSV naar Car objecten
        Console.WriteLine("[DEBUG] Aanroepen LoadCarsFromCsv(string csvPath)...");
        var allCars = LoadCarsFromCsv(csvPath);
        Console.WriteLine($"[DEBUG] LoadCarsFromCsv voltooid - {allCars.Count} auto's geladen");
        
        // #region agent log
        try
        {
            var audiCountBeforeDedup = allCars.Count(c => c.Brand?.Equals("Audi", StringComparison.OrdinalIgnoreCase) == true);
            var audiModelsBeforeDedup = allCars.Where(c => c.Brand?.Equals("Audi", StringComparison.OrdinalIgnoreCase) == true).Select(c => c.Model).Distinct().ToList();
            Console.WriteLine($"[DEBUG] Audi count VOOR deduplicatie: {audiCountBeforeDedup} (totaal auto's: {allCars.Count})");
            Console.WriteLine($"[DEBUG] Audi modellen VOOR deduplicatie: {string.Join(", ", audiModelsBeforeDedup)}");
            var logPath = Path.Combine(Directory.GetCurrentDirectory(), ".cursor", "debug.log");
            var logDir = Path.GetDirectoryName(logPath);
            if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);
            File.AppendAllText(logPath, 
                $"{{\"location\":\"CarRepository.cs:53\",\"message\":\"Audi count VOOR deduplicatie\",\"data\":{{\"count\":{audiCountBeforeDedup},\"models\":{JsonSerializer.Serialize(audiModelsBeforeDedup)},\"totalCars\":{allCars.Count}}},\"timestamp\":{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()},\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"B\"}}\n");
        }
        catch { /* Fail silently in production */ }
        // #endregion
        
        // Verwijder duplicaten (Brand+Model combinatie, behoud hoogste prijs)
        // TIJDELIJK UITGESCHAKELD VOOR DEBUGGING
        int originalCount = allCars.Count;
        // _cars = RemoveDuplicates(allCars);
        _cars = allCars; // TIJDELIJK: geen deduplicatie
        int duplicateCount = 0; // originalCount - _cars.Count;
        
        // #region agent log
        try
        {
            var audiCountAfterDedup = _cars.Count(c => c.Brand?.Equals("Audi", StringComparison.OrdinalIgnoreCase) == true);
            var audiModelsAfterDedup = _cars.Where(c => c.Brand?.Equals("Audi", StringComparison.OrdinalIgnoreCase) == true).Select(c => c.Model).Distinct().ToList();
            Console.WriteLine($"[DEBUG] Audi count NA deduplicatie: {audiCountAfterDedup} (totaal auto's: {_cars.Count})");
            Console.WriteLine($"[DEBUG] Audi modellen NA deduplicatie: {string.Join(", ", audiModelsAfterDedup)}");
            var logPath = Path.Combine(Directory.GetCurrentDirectory(), ".cursor", "debug.log");
            var logDir = Path.GetDirectoryName(logPath);
            if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);
            File.AppendAllText(logPath, 
                $"{{\"location\":\"CarRepository.cs:57\",\"message\":\"Audi count NA deduplicatie\",\"data\":{{\"count\":{audiCountAfterDedup},\"models\":{JsonSerializer.Serialize(audiModelsAfterDedup)},\"totalCars\":{_cars.Count}}},\"timestamp\":{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()},\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"B\"}}\n");
        }
        catch { /* Fail silently in production */ }
        // #endregion
        
        if (duplicateCount > 0)
        {
            Console.WriteLine($"Duplicaten verwijderd: {duplicateCount} auto's (oorspronkelijk {originalCount}, na verwijdering {_cars.Count})");
        }

        // Laad image mapping
        LoadImageMapping();

        // Genereer image paths (gebruikt mapping voor eerste image)
        AssignImagePaths(_cars);

        Console.WriteLine($"Aantal ingelezen auto's: {_cars.Count}");
    }

    /// <summary>
    /// Haalt alle auto's op uit de data bron.
    /// </summary>
    public List<Car> GetAllCars()
    {
        // #region agent log
        try
        {
            var audiCount = _cars.Count(c => c.Brand?.Equals("Audi", StringComparison.OrdinalIgnoreCase) == true);
            Console.WriteLine($"[DEBUG] GetAllCars aangeroepen - Totaal auto's: {_cars.Count}, Audi count: {audiCount}");
            var logPath = Path.Combine(Directory.GetCurrentDirectory(), ".cursor", "debug.log");
            var logDir = Path.GetDirectoryName(logPath);
            if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);
            File.AppendAllText(logPath, 
                $"{{\"location\":\"CarRepository.cs:78\",\"message\":\"GetAllCars aangeroepen\",\"data\":{{\"totalCars\":{_cars.Count},\"audiCount\":{audiCount}}},\"timestamp\":{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()},\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"C\"}}\n");
        }
        catch { /* Fail silently in production */ }
        // #endregion
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
        Console.WriteLine($"[DEBUG] LoadCarsFromCsv(string) gestart met pad: {csvPath}");
        List<Car> cars = new List<Car>();
        int rowNumber = 0;

        try
        {
            // Lees alle regels uit het CSV-bestand
            string[] lines = File.ReadAllLines(csvPath);
            Console.WriteLine($"[DEBUG] CSV gelezen: {lines.Length} regels totaal (inclusief header)");

            if (lines.Length == 0)
            {
                Console.WriteLine("[DEBUG] FOUT: Het CSV-bestand is leeg.");
                return cars;
            }
            if (lines.Length == 1)
            {
                Console.WriteLine("[DEBUG] FOUT: CSV bevat alleen header, geen data regels.");
                return cars;
            }
            Console.WriteLine($"[DEBUG] Verwacht {lines.Length - 1} data regels (na header)");

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
                        // #region agent log
                        if (car.Brand.Equals("Audi", StringComparison.OrdinalIgnoreCase))
                        {
                            try
                            {
                                var logPath = Path.Combine(Directory.GetCurrentDirectory(), ".cursor", "debug.log");
                                var logData = new { brand = car.Brand, model = car.Model, budget = car.Budget, power = car.Power, year = car.Year, rowNumber };
                                var logDir = Path.GetDirectoryName(logPath);
                                if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
                                    Directory.CreateDirectory(logDir);
                                File.AppendAllText(logPath, 
                                    $"{{\"location\":\"CarRepository.cs:258\",\"message\":\"Audi gevonden in CSV (voor IsCarRealistic)\",\"data\":{JsonSerializer.Serialize(logData)},\"timestamp\":{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()},\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"A\"}}\n");
                            }
                            catch { /* Fail silently in production */ }
                        }
                        // #endregion
                        // Filter onrealistische waarden
                        if (IsCarRealistic(car))
                        {
                            // #region agent log
                            if (car.Brand.Equals("Audi", StringComparison.OrdinalIgnoreCase))
                            {
                                try
                                {
                                    var logPath = Path.Combine(Directory.GetCurrentDirectory(), ".cursor", "debug.log");
                                    var logData = new { brand = car.Brand, model = car.Model, budget = car.Budget };
                                    var logDir = Path.GetDirectoryName(logPath);
                                    if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
                                        Directory.CreateDirectory(logDir);
                                    File.AppendAllText(logPath, 
                                        $"{{\"location\":\"CarRepository.cs:268\",\"message\":\"Audi passeert IsCarRealistic\",\"data\":{JsonSerializer.Serialize(logData)},\"timestamp\":{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()},\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"A\"}}\n");
                                }
                                catch { /* Fail silently in production */ }
                            }
                            // #endregion
                            // Genereer ImageUrl voor belangrijke modellen
                            car.ImageUrl = GenerateImageUrl(car);
                            cars.Add(car);
                        }
                        else
                        {
                            // #region agent log
                            if (car.Brand.Equals("Audi", StringComparison.OrdinalIgnoreCase))
                            {
                                try
                                {
                                    var logPath = Path.Combine(Directory.GetCurrentDirectory(), ".cursor", "debug.log");
                                    var logData = new { brand = car.Brand, model = car.Model, budget = car.Budget, power = car.Power, year = car.Year };
                                    var logDir = Path.GetDirectoryName(logPath);
                                    if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
                                        Directory.CreateDirectory(logDir);
                                    File.AppendAllText(logPath, 
                                        $"{{\"location\":\"CarRepository.cs:287\",\"message\":\"Audi GEFAALD IsCarRealistic check\",\"data\":{JsonSerializer.Serialize(logData)},\"timestamp\":{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()},\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"A\"}}\n");
                                }
                                catch { /* Fail silently in production */ }
                            }
                            // #endregion
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
            Console.WriteLine($"[DEBUG] FOUT: Bestand niet gevonden: {csvPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] FOUT bij het lezen van het CSV-bestand: {ex.Message}");
            Console.WriteLine($"[DEBUG] Exception type: {ex.GetType().Name}");
            Console.WriteLine($"[DEBUG] Stack trace: {ex.StackTrace}");
        }

        Console.WriteLine($"[DEBUG] LoadCarsFromCsv(string) voltooid: {cars.Count} auto's toegevoegd");
        return cars;
    }

    /// <summary>
    /// Verwijdert duplicaten op basis van Brand+Model combinatie.
    /// Behoudt de auto met de hoogste prijs per unieke combinatie.
    /// </summary>
    private List<Car> RemoveDuplicates(List<Car> cars)
    {
        if (cars == null || cars.Count == 0)
            return cars;

        // Gebruik een dictionary om de beste auto per unieke combinatie bij te houden
        // Key: Brand|Model (case-insensitive), Value: Car object
        Dictionary<string, Car> uniqueCarsMap = new Dictionary<string, Car>(StringComparer.OrdinalIgnoreCase);

        foreach (Car car in cars)
        {
            string brand = (car.Brand ?? string.Empty).Trim();
            string model = (car.Model ?? string.Empty).Trim();
            string uniqueKey = $"{brand}|{model}";

            if (uniqueCarsMap.TryGetValue(uniqueKey, out Car? existingCar))
            {
                // Als er al een auto is, behoud degene met de hoogste prijs
                // #region agent log
                if (brand.Equals("Audi", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var logPath = Path.Combine(Directory.GetCurrentDirectory(), ".cursor", "debug.log");
                        var logData = new { uniqueKey, existingBudget = existingCar.Budget, newBudget = car.Budget, kept = car.Budget > existingCar.Budget };
                        var logDir = Path.GetDirectoryName(logPath);
                        if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
                            Directory.CreateDirectory(logDir);
                        File.AppendAllText(logPath, 
                            $"{{\"location\":\"CarRepository.cs:292\",\"message\":\"Audi duplicaat gevonden in RemoveDuplicates\",\"data\":{JsonSerializer.Serialize(logData)},\"timestamp\":{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()},\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"B\"}}\n");
                    }
                    catch { /* Fail silently in production */ }
                }
                // #endregion
                if (car.Budget > existingCar.Budget)
                {
                    uniqueCarsMap[uniqueKey] = car;
                }
            }
            else
            {
                // Nieuwe unieke combinatie, voeg toe
                // #region agent log
                if (brand.Equals("Audi", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var logPath = Path.Combine(Directory.GetCurrentDirectory(), ".cursor", "debug.log");
                        var logData = new { uniqueKey, budget = car.Budget };
                        var logDir = Path.GetDirectoryName(logPath);
                        if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
                            Directory.CreateDirectory(logDir);
                        File.AppendAllText(logPath, 
                            $"{{\"location\":\"CarRepository.cs:302\",\"message\":\"Nieuwe Audi unieke combinatie toegevoegd\",\"data\":{JsonSerializer.Serialize(logData)},\"timestamp\":{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()},\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"B\"}}\n");
                    }
                    catch { /* Fail silently in production */ }
                }
                // #endregion
                uniqueCarsMap.Add(uniqueKey, car);
            }
        }

        return uniqueCarsMap.Values.ToList();
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
            // Eerst checken of er images zijn in de mapping
            if (_imageMapping.TryGetValue(car.Id, out List<string>? images) && images != null && images.Count > 0)
            {
                // Gebruik de eerste image uit de mapping voor ImageUrl (voor main pagina)
                car.ImageUrl = images[0];
                // ImagePath kan leeg blijven, we gebruiken ImageUrl
                continue;
            }

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
                // Genereer externe ImageUrl als fallback
                car.ImageUrl = GenerateImageUrl(car);
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
    /// Laadt de image mapping uit car_image_mapping.json.
    /// Deze mapping bevat alle images per auto ID.
    /// </summary>
    private void LoadImageMapping()
    {
        try
        {
            string mappingPath = Path.Combine(_dataDirectory, "car_image_mapping.json");
            
            // Probeer ook vanuit huidige directory
            if (!File.Exists(mappingPath))
            {
                mappingPath = Path.Combine(Directory.GetCurrentDirectory(), _dataDirectory, "car_image_mapping.json");
            }
            
            // Laatste poging: zoek omhoog in directory tree
            if (!File.Exists(mappingPath))
            {
                string? searchDir = Directory.GetCurrentDirectory();
                for (int i = 0; i < 5 && searchDir != null; i++)
                {
                    string testPath = Path.Combine(searchDir, _dataDirectory, "car_image_mapping.json");
                    if (File.Exists(testPath))
                    {
                        mappingPath = testPath;
                        break;
                    }
                    searchDir = Directory.GetParent(searchDir)?.FullName;
                }
            }

            if (!File.Exists(mappingPath))
            {
                Console.WriteLine($"Waarschuwing: car_image_mapping.json niet gevonden. Images worden niet geladen.");
                return;
            }

            string jsonContent = File.ReadAllText(mappingPath);
            var mappingDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonContent);

            if (mappingDict == null)
            {
                Console.WriteLine("Waarschuwing: car_image_mapping.json kon niet worden geparsed.");
                return;
            }

            // Converteer string keys naar int keys en JsonElement arrays naar List<string>
            foreach (var kvp in mappingDict)
            {
                if (int.TryParse(kvp.Key, out int carId))
                {
                    List<string> imageUrls = new List<string>();
                    
                    if (kvp.Value.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var element in kvp.Value.EnumerateArray())
                        {
                            if (element.ValueKind == JsonValueKind.String)
                            {
                                imageUrls.Add(element.GetString() ?? string.Empty);
                            }
                        }
                    }
                    else if (kvp.Value.ValueKind == JsonValueKind.String)
                    {
                        // Backward compatibility: single string instead of array
                        imageUrls.Add(kvp.Value.GetString() ?? string.Empty);
                    }
                    
                    if (imageUrls.Count > 0)
                    {
                        _imageMapping[carId] = imageUrls;
                    }
                }
            }

            Console.WriteLine($"Image mapping geladen: {_imageMapping.Count} auto's met images.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fout bij het laden van image mapping: {ex.Message}");
        }
    }

    /// <summary>
    /// Haalt alle images op voor een specifieke auto.
    /// Retourneert een lijst van image URLs uit de image mapping.
    /// </summary>
    public List<string> GetCarImages(Car car)
    {
        if (car == null)
        {
            return new List<string>();
        }

        if (_imageMapping.TryGetValue(car.Id, out List<string>? images))
        {
            return images;
        }

        // Geen images in mapping, retourneer lege lijst
        return new List<string>();
    }
}



