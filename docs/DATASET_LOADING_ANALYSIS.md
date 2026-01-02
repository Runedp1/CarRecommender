# Dataset Loading Analysis & Fix

## 🔍 Root Cause Analysis

### Waar de dataset wordt geladen:

1. **Program.cs (line 92)**: `ICarRepository` wordt geregistreerd als **Singleton**
   ```csharp
   builder.Services.AddSingleton<ICarRepository>(sp => new CarRepository(csvFileName, dataDirectory));
   ```

2. **CarRepository.cs (line 26-30)**: Constructor laadt data bij initialisatie
   ```csharp
   public CarRepository(string csvFileName = "df_master_v8_def.csv", string dataDirectory = "data")
   {
       _csvFileName = csvFileName;
       _dataDirectory = dataDirectory;
       LoadCarsFromCsv(); // ⚠️ Wordt ALLEEN bij constructor aangeroepen
   }
   ```

3. **CarRepository.cs (line 17)**: Data wordt opgeslagen in private field
   ```csharp
   private List<Car> _cars = new List<Car>(); // ⚠️ In-memory cache
   ```

### Het Probleem:

- **Singleton Service**: `ICarRepository` is een singleton → wordt **één keer** aangemaakt bij applicatie startup
- **In-Memory Cache**: `_cars` list wordt **één keer** gevuld in constructor
- **Geen Refresh**: Als CSV bestand wordt vervangen, blijft oude data in geheugen
- **Oplossing**: Applicatie **moet worden herstart** na CSV wijziging

### Waar Vermogen wordt geparsed:

- **CarRepository.cs (lines 277-315)**: Vermogen wordt direct uit CSV kolom "vermogen" gelezen
- Geen transformatie of override - waarde komt direct uit CSV
- Parsing: `Regex.Replace(vermogenStr, @"[^\d]", "")` haalt alleen cijfers eruit

## ✅ Fix Implementatie

### 1. Uitgebreide Logging Toegevoegd

**CarRepository.cs - Na CSV laden:**
```csharp
Console.WriteLine($"[DATA_LOAD] ✅ LoadCarsFromCsv voltooid - {allCars.Count} auto's geladen");
// Toont eerste 5 records met ID, Merk, Model, Bouwjaar, Vermogen, Prijs
```

**CarRepository.cs - Voor returnen:**
```csharp
Console.WriteLine($"[DATA_RETURN] 📤 GetAllCars() aangeroepen - retourneert {_cars.Count} auto's");
// Toont eerste 5 records vlak voor returnen
```

**Program.cs - Bij DI registratie:**
```csharp
Console.WriteLine($"[DI] ✅ CarRepository singleton geïnitialiseerd met {carRepository.GetAllCars().Count} auto's");
```

**CarsController.cs - In API endpoint:**
```csharp
_logger.LogInformation("[API] GetAllCars() - Totaal: {TotalCount} auto's. Voorbeelden: {Samples}", ...);
```

### 2. Verificatie van Geladen Bestand

**CarRepository.cs (lines 65-77)**: Controleert of juiste bestand wordt geladen
- Toont bestandsnaam, grootte, laatste wijzigingsdatum
- Waarschuwing als verkeerd bestand wordt geladen

### 3. Oude Dataset Verwijderd

- `Cleaned_Car_Data_For_App_Fully_Enriched.csv` → `.OLD` (hernoemd)
- Voorkomt dat oude dataset per ongeluk wordt gebruikt

## 📋 Wat te Controleren bij Runtime

### Console Output bij Startup:

```
[CONFIG] CSV File: df_master_v8_def.csv
[CONFIG] Full Path: C:\...\backend\data\df_master_v8_def.csv
[CONFIG] File Size: 3.670.977 bytes
[CONFIG] File Last Modified: 12/30/2025 22:00:47
[VERIFY] Geladen bestand: df_master_v8_def.csv
[DATA_LOAD] ✅ LoadCarsFromCsv voltooid - 65128 auto's geladen
[DATA_LOAD] 📊 Voorbeeldrecords (eerste 5) direct na inladen:
[DATA_LOAD]   ID=1, Merk=chevrolet, Model=equinox, Bouwjaar=2011, Vermogen=182 KW, Prijs=€16.621
...
[DI] ✅ CarRepository singleton geïnitialiseerd met 65128 auto's
```

### Bij API Call:

```
[DATA_RETURN] 📤 GetAllCars() aangeroepen - retourneert 65128 auto's
[DATA_RETURN] 📊 Voorbeeldrecords (eerste 5) vlak voor returnen:
[DATA_RETURN]   ID=1, Merk=chevrolet, Model=equinox, Bouwjaar=2011, Vermogen=182 KW, Prijs=€16.621
[API] GetAllCars() - Totaal: 65128 auto's. Voorbeelden: ...
```

## 🔧 Concrete Fix

### Belangrijkste Wijzigingen:

1. **Program.cs**: Singleton wordt expliciet aangemaakt met logging
2. **CarRepository.cs**: Uitgebreide logging bij laden en returnen
3. **CarsController.cs**: Logging in API endpoint
4. **Oude dataset**: Hernoemd naar `.OLD` om verwarring te voorkomen

### Om Nieuwe Dataset te Gebruiken:

1. **Vervang CSV bestand**: `backend/data/df_master_v8_def.csv`
2. **Herstart applicatie**: Stop en start backend opnieuw
3. **Controleer console**: Zie `[DATA_LOAD]` berichten voor bevestiging
4. **Test API**: Check `/api/health/info` voor aantal geladen auto's

## ⚠️ Belangrijke Notitie

**Singleton Pattern betekent:**
- Data wordt **één keer** geladen bij applicatie startup
- Als CSV wordt vervangen **zonder herstart**, blijft oude data in geheugen
- **Oplossing**: Altijd applicatie herstarten na CSV wijziging

**Alternatief (niet geïmplementeerd):**
- File watcher toevoegen om automatisch te reloaden
- Of singleton vervangen door scoped/transient (maar dit is minder efficiënt)

