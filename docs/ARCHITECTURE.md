# Architectuur Documentatie - Car Recommendation System

## Overzicht

Dit document beschrijft de architectuur van het Car Recommendation System en legt uit welke laag waarvoor dient.

## Laag Scheiding

Het project volgt een **3-laags architectuur** met duidelijke scheiding van verantwoordelijkheden:

```
┌─────────────────────────────────────────┐
│   PRESENTATIE LAAG (Program.cs)         │  ← Gebruikersinteractie
├─────────────────────────────────────────┤
│   BUSINESS LOGICA LAAG                  │
│   - RecommendationService.cs            │  ← Service coordinatie
│   - RecommendationEngine.cs             │  ← Core algoritmes
├─────────────────────────────────────────┤
│   DATA LAAG (CarRepository.cs)          │  ← Data toegang
├─────────────────────────────────────────┤
│   DATA MODEL LAAG (Car.cs)              │  ← Domain entities
└─────────────────────────────────────────┘
```

## 1. DATA MODEL LAAG

**Bestand**: `src/Car.cs`

**Verantwoordelijkheden:**
- Definieert domain entities (Car, RecommendationResult)
- Bevat alleen data structuren, geen logica
- Wordt gebruikt door alle andere lagen

**Klassen:**
- `Car`: Representeert een auto met alle eigenschappen
- `RecommendationResult`: Representeert een recommendation met similarity score

**Gebruikt door:**
- Alle andere lagen (data laag, business logica, presentatie)

## 2. DATA LAAG

**Bestand**: `src/CarRepository.cs`

**Verantwoordelijkheden:**
- **CSV Data Toegang**: Leest CSV bestanden en converteert naar Car objecten
- **Data Transformatie**: Parse en valideert CSV data
- **Image Path Toewijzing**: Genereert image paths voor Car objecten
- **Foutafhandeling**: Handelt parsing errors af zonder applicatie te crashen

**Methoden:**
- `LoadCarsFromCsv()`: Laadt auto's uit CSV
- `AssignImagePaths()`: Wijs image paths toe
- `FindCsvFile()`: Zoekt CSV bestand in data directory
- Helper methoden: `ParseCsvLine()`, `FindColumnIndex()`, `SanitizeFileName()`

**Kenmerken:**
- ❌ Geen business logica
- ❌ Geen presentatie (geen Console.WriteLine behalve error logging)
- ✅ Alleen data toegang en transformatie
- ✅ Implementeert Repository Pattern

**Werkt met:**
- `data/Cleaned_Car_Data_For_App_Fully_Enriched.csv`

## 3. BUSINESS LOGICA LAAG

### 3a. RecommendationEngine (`src/RecommendationEngine.cs`)

**Verantwoordelijkheden:**
- **Core Algoritmes**: Implementeert similarity berekening algoritmes
- **Feature Normalisatie**: Normaliseert numerieke waarden voor vergelijking
- **Pure Business Logica**: Geen data toegang, geen side effects

**Methoden:**
- `CalculateSimilarity()`: Berekent similarity tussen twee auto's
- `NormalizeValue()`: Normaliseert waarden naar 0-1 bereik

**Algoritme:**
- Gebruikt gewogen gemiddelde van 4 features:
  - Power (25%)
  - Budget (30%)
  - Year (20%)
  - Fuel (25%)
- Min-max normalisatie voor numerieke features
- Categorische matching voor Fuel type

**Kenmerken:**
- ❌ Geen data toegang
- ❌ Geen presentatie
- ✅ Pure functies (testbaar)
- ✅ Geen side effects

### 3b. RecommendationService (`src/RecommendationService.cs`)

**Verantwoordelijkheden:**
- **Service Coordinatie**: Coördineert het recommendation proces
- **Data Aggregatie**: Berekent dataset statistieken (min/max voor normalisatie)
- **Result Filtering**: Filtert en sorteert recommendations
- **Orchestratie**: Combineert verschillende stappen

**Methoden:**
- `RecommendSimilarCars()`: Hoofdmethode die recommendations genereert

**Kenmerken:**
- ❌ Geen data toegang (gebruikt CarRepository)
- ❌ Geen presentatie
- ✅ Business logica coördinatie
- ✅ Gebruikt RecommendationEngine voor berekeningen

**Gebruikt:**
- RecommendationEngine voor similarity berekeningen
- CarRepository voor data (indirect via parameters)

## 4. PRESENTATIE LAAG

**Bestand**: `src/Program.cs`

**Verantwoordelijkheden:**
- **Entry Point**: Main() methode start de applicatie
- **Gebruikersinteractie**: Console output, input
- **Coördinatie**: Coordineert tussen verschillende lagen
- **Foutafhandeling**: Toont gebruikersvriendelijke error messages

**Methoden:**
- `Main()`: Entry point
- `DisplayCars()`: Toont lijst van auto's
- `DisplayRecommendations()`: Toont recommendations

**Kenmerken:**
- ❌ Geen business logica
- ❌ Geen data toegang (gebruikt CarRepository)
- ✅ Alleen presentatie en coördinatie
- ✅ Gebruikersvriendelijke interface

**Gebruikt:**
- CarRepository voor data
- RecommendationService voor business logica

## Data Flow

```
[CSV Bestand] 
    ↓
[CarRepository.LoadCarsFromCsv()]
    ↓
[List<Car>]
    ↓
[CarRepository.AssignImagePaths()]
    ↓
[List<Car> met ImagePath]
    ↓
[RecommendationService.RecommendSimilarCars()]
    ↓
    ├─→ [RecommendationEngine.CalculateSimilarity()] (voor elke auto)
    ↓
[List<RecommendationResult>]
    ↓
[Program.DisplayRecommendations()]
    ↓
[Console Output]
```

## Bestandsstructuur

```
src/
├── Car.cs                    # DATA MODEL LAAG
├── CarRepository.cs          # DATA LAAG
├── RecommendationEngine.cs   # BUSINESS LOGICA (Core algoritmes)
├── RecommendationService.cs  # BUSINESS LOGICA (Service coordinatie)
└── Program.cs                # PRESENTATIE LAAG
```

## Voordelen van deze Architectuur

1. **Scheiding van Verantwoordelijkheden**: Elke laag heeft één duidelijke taak
2. **Testbaarheid**: Business logica is pure functies, makkelijk te testen
3. **Onderhoudbaarheid**: Wijzigingen in één laag beïnvloeden andere lagen niet
4. **Herbruikbaarheid**: Business logica kan gebruikt worden door andere UI's (web, desktop)
5. **Uitbreidbaarheid**: Nieuwe features kunnen toegevoegd worden zonder bestaande code te breken

## Uitbreidingsmogelijkheden

### Nieuwe Data Bron
- Wijzig alleen `CarRepository.cs` om nieuwe bronnen te ondersteunen (bijv. database, API)

### Nieuwe Recommendation Algoritme
- Voeg nieuwe methode toe aan `RecommendationEngine.cs` of maak nieuwe engine klasse

### Nieuwe UI (Web, Desktop)
- Maak nieuwe presentatie laag die `RecommendationService` gebruikt
- `Program.cs` blijft voor console, nieuwe klasse voor web/desktop

### Performance Optimalisatie
- Voeg caching toe in `CarRepository` of `RecommendationService`
- Wijzigingen blijven geïsoleerd binnen één laag

## Voor Docenten

Deze architectuur demonstreert:
- ✅ Clean Code principes
- ✅ SOLID principes (Single Responsibility)
- ✅ Repository Pattern
- ✅ Service Layer Pattern
- ✅ Separation of Concerns
- ✅ Testbaarheid en maintainability
- ✅ Professionele code organisatie

Alle code bevat uitgebreide Nederlandse comments die uitleggen welke laag waarvoor dient.



