# CarRecommender Project - Volledige Structuur

**Laatste update**: 2025-01-XX  
**Versie**: 2.0 (met Manual Filters & Dynamic Routing)

---

## 📁 Project Structuur

```
Recommendation_System_New/
│
├── backend/                                    # Backend API (ASP.NET Core Web API)
│   ├── CarRecommender.Api/
│   │   ├── Controllers/                        # API Controllers
│   │   │   ├── CarsController.cs              # GET /api/cars, GET /api/cars/{id}
│   │   │   ├── RecommendationsController.cs  # Recommendations endpoints
│   │   │   │                                   #   - GET /api/recommendations/{id}
│   │   │   │                                   #   - POST /api/recommendations/text
│   │   │   │                                   #   - POST /api/recommendations/hybrid/manual
│   │   │   └── HealthController.cs            # GET /api/health
│   │   ├── Program.cs                          # Dependency injection, middleware config
│   │   ├── appsettings.json                   # Configuratie (Development)
│   │   ├── appsettings.Production.json        # Configuratie (Production)
│   │   └── CarRecommender.Api.csproj          # Project file
│   │
│   ├── data/                                   # CSV Datasets
│   │   ├── Cleaned_Car_Data_For_App_Fully_Enriched.csv  # Hoofd dataset
│   │   └── [andere CSV bestanden]
│   │
│   └── images/                                 # Auto afbeeldingen (49k+ bestanden)
│       └── [Kaggle dataset images]
│
├── frontend/                                   # Frontend Web App (ASP.NET Core Razor Pages)
│   └── CarRecommender.Web/
│       ├── Pages/                              # Razor Pages
│       │   ├── Index.cshtml                    # Homepage - Tekst modus recommendations
│       │   ├── Index.cshtml.cs                 # Page model voor tekst input
│       │   ├── AdvancedFilters.cshtml          # Geavanceerde Filters - Manuele modus
│       │   ├── AdvancedFilters.cshtml.cs       # Page model voor manuele filters
│       │   ├── CarDetail.cshtml                # Dynamische detailpagina /car/{id}
│       │   ├── CarDetail.cshtml.cs             # Page model voor auto details
│       │   ├── Cars.cshtml                     # Auto's overzicht (bladeren)
│       │   ├── Cars.cshtml.cs                  # Page model voor auto's lijst
│       │   ├── Shared/
│       │   │   └── _Layout.cshtml              # Master layout met navigatie
│       │   └── [andere pages]
│       │
│       ├── Models/                              # Data Models
│       │   ├── Car.cs                          # Car model voor frontend
│       │   ├── RecommendationResult.cs         # Recommendation result model
│       │   ├── RecommendationTextRequest.cs     # Request model voor tekst modus
│       │   └── ManualFilterRequest.cs           # Request model voor manuele filters
│       │
│       ├── Services/                            # Services
│       │   └── CarApiClient.cs                 # HTTP client voor API communicatie
│       │
│       ├── Program.cs                           # Dependency injection, routing
│       ├── appsettings.json                     # API URL configuratie
│       └── CarRecommender.Web.csproj           # Project file
│
├── src/                                        # Core Business Logic Library (.NET 9.0)
│   ├── Car.cs                                  # Domain models:
│   │                                           #   - Car
│   │                                           #   - RecommendationResult
│   │                                           #   - UserPreferences
│   │                                           #   - ManualFilterRequest
│   │
│   ├── ICarRepository.cs                      # Repository interface
│   ├── CarRepository.cs                       # CSV-based repository implementatie
│   │
│   ├── IRecommendationService.cs             # Recommendation service interface
│   ├── RecommendationService.cs              # Main recommendation service
│   │                                           #   - RecommendSimilarCars()
│   │                                           #   - RecommendFromText()
│   │                                           #   - RecommendFromManualFilters()
│   │
│   ├── RecommendationEngine.cs              # Content-based similarity engine
│   ├── TextParserService.cs                  # NLP text parsing (Nederlands)
│   ├── RuleBasedFilter.cs                    # Harde filters (Old AI)
│   ├── CarFeatureVector.cs                   # Feature vector model
│   ├── CarFeatureVectorFactory.cs            # Feature vector factory
│   ├── SimilarityService.cs                 # Similarity berekeningen
│   ├── AdvancedScoringService.cs            # Geavanceerde scoring met transparantie
│   ├── RankingService.cs                     # Ranking met controlled randomness
│   └── ExplanationBuilder.cs                 # Uitleg generatie
│
├── docs/                                       # Documentatie
│   ├── PROJECT_STRUCTURE.md                   # Dit bestand
│   ├── PROJECT_OVERVIEW.md                    # Project overzicht
│   ├── MANUAL_FILTERS_DOCUMENTATION.md        # Manual filters documentatie
│   ├── AI_ENGINE_OVERVIEW.md                  # AI engine uitleg
│   └── [andere documentatie bestanden]
│
├── tools/                                      # Python scripts & notebooks
│   ├── notebooks/
│   │   └── recommender.ipynb                 # Jupyter notebook
│   └── scripts/                               # Data processing scripts
│       └── [Python scripts voor data cleaning/merging]
│
├── CarRecommender.sln                         # Visual Studio Solution
└── README.md                                  # Hoofd README

```

---

## 🎯 Belangrijkste Componenten

### Backend API (`backend/CarRecommender.Api/`)

**Type**: ASP.NET Core Web API (.NET 9.0)  
**Functie**: RESTful API voor auto recommendations

#### Endpoints:

1. **GET /api/health**
   - Health check voor monitoring
   - Response: `{ "status": "OK" }`

2. **GET /api/cars**
   - Haalt alle auto's op met paginatie
   - Query params: `page`, `pageSize`
   - Response: `PagedResult<Car>`

3. **GET /api/cars/{id}**
   - Haalt één specifieke auto op
   - Path param: `id` (int)
   - Response: `Car`

4. **GET /api/recommendations/{id}**
   - Recommendations voor een specifieke auto
   - Path param: `id` (int)
   - Query param: `top` (default: 5, max: 20)
   - Response: `List<RecommendationResult>`

5. **POST /api/recommendations/text**
   - Tekst-gebaseerde recommendations (NLP)
   - Request: `{ "text": "...", "top": 5 }`
   - Response: `List<RecommendationResult>`

6. **POST /api/recommendations/hybrid/manual**
   - Manuele filters (zonder tekst parsing)
   - Request: `ManualFilterRequest` (zie documentatie)
   - Response: `List<RecommendationResult>`

---

### Frontend Web App (`frontend/CarRecommender.Web/`)

**Type**: ASP.NET Core Razor Pages (.NET 9.0)  
**Functie**: Web interface voor gebruikers

#### Pagina's:

1. **Index.cshtml** (`/` of `/Index`)
   - **Modus**: Tekst-gebaseerde recommendations
   - **Functie**: 
     - Tekst input formulier
     - Toont recommendations met cards
     - Elke kaart linkt naar `/car/{id}`

2. **AdvancedFilters.cshtml** (`/advanced-filters`)
   - **Modus**: Manuele filters (zonder tekst parsing)
   - **Functie**:
     - Formulier met dropdowns en input velden
     - Merk, model, brandstof, transmissie, carrosserie
     - Min/max prijs, bouwjaar, vermogen
     - Elke kaart linkt naar `/car/{id}`

3. **CarDetail.cshtml** (`/car/{id}`)
   - **Modus**: Dynamische detailpagina
   - **Functie**:
     - Toont auto details op basis van route parameter `{id}`
     - Image carousel
     - Alle auto eigenschappen
     - Links naar recommendations

4. **Cars.cshtml** (`/Cars`)
   - **Modus**: Auto's overzicht
   - **Functie**:
     - Toont alle auto's
     - Elke kaart linkt naar `/car/{id}`
     - Filtering mogelijk (basis)

---

### Core Library (`src/`)

**Type**: .NET 9.0 Class Library  
**Functie**: Business logic, algoritmes, data modellen

#### Belangrijkste Classes:

- **CarRepository**: CSV data access, filtering
- **RecommendationService**: Coördineert recommendation proces
- **TextParserService**: NLP parsing (Nederlands)
- **RuleBasedFilter**: Harde filters (Old AI)
- **AdvancedScoringService**: Geavanceerde scoring
- **ExplanationBuilder**: Uitleg generatie

---

## 🔄 Data Flow

### Tekst Modus Flow:

```
1. User input (tekst) 
   ↓
2. Index.cshtml → POST formulier
   ↓
3. CarApiClient.GetRecommendationsFromTextAsync()
   ↓
4. POST /api/recommendations/text
   ↓
5. RecommendationsController.GetRecommendationsFromText()
   ↓
6. RecommendationService.RecommendFromText()
   ↓
7. TextParserService.ParsePreferencesFromText() → UserPreferences
   ↓
8. RuleBasedFilter.FilterCars() → Candidate set
   ↓
9. AdvancedScoringService.CalculateScores() → Rankings
   ↓
10. Response: List<RecommendationResult>
    ↓
11. Index.cshtml toont cards met links naar /car/{id}
```

### Manuele Filters Flow:

```
1. User vult formulier in (AdvancedFilters.cshtml)
   ↓
2. POST formulier → AdvancedFiltersModel.OnPostAsync()
   ↓
3. CarApiClient.GetRecommendationsFromManualFiltersAsync()
   ↓
4. POST /api/recommendations/hybrid/manual
   ↓
5. RecommendationsController.GetRecommendationsFromManualFilters()
   ↓
6. RecommendationService.RecommendFromManualFilters()
   ↓
7. ManualFilterRequest → FilterCriteria (directe mapping)
   ↓
8. RuleBasedFilter.FilterCars() → Candidate set
   ↓
9. AdvancedScoringService.CalculateScores() → Rankings
   ↓
10. Response: List<RecommendationResult>
    ↓
11. AdvancedFilters.cshtml toont cards met links naar /car/{id}
```

### Detailpagina Flow:

```
1. User klikt op kaart → href="/car/@carId"
   ↓
2. Route: /car/{id} matcht CarDetail.cshtml
   ↓
3. Razor Pages mapt {id} → int id parameter
   ↓
4. CarDetailModel.OnGetAsync(int id)
   ↓
5. CarApiClient.GetCarByIdAsync(id)
   ↓
6. GET /api/cars/{id}
   ↓
7. CarsController.GetCar(id)
   ↓
8. CarRepository.GetCarById(id)
   ↓
9. Response: Car object
   ↓
10. CarDetail.cshtml toont auto details
```

---

## 🔌 API Integratie

### Frontend → Backend

**Service**: `CarApiClient` (`frontend/CarRecommender.Web/Services/CarApiClient.cs`)

**Methodes**:
- `GetAllCarsAsync()` → GET /api/cars
- `GetCarByIdAsync(int id)` → GET /api/cars/{id}
- `GetRecommendationsAsync(int carId, int top)` → GET /api/recommendations/{id}
- `GetRecommendationsFromTextAsync(string text, int top)` → POST /api/recommendations/text
- `GetRecommendationsFromManualFiltersAsync(ManualFilterRequest)` → POST /api/recommendations/hybrid/manual
- `GetCarImagesAsync(int id)` → GET /api/cars/{id}/images

**Configuratie**: `appsettings.json` → `ApiSettings:BaseUrl`

---

## 📊 Data Modellen

### Car
```csharp
public class Car
{
    public int Id { get; set; }
    public string Brand { get; set; }
    public string Model { get; set; }
    public int Power { get; set; }          // KW
    public string Fuel { get; set; }        // petrol/diesel/hybrid/electric
    public decimal Budget { get; set; }     // Prijs in euro's
    public int Year { get; set; }           // Bouwjaar
    public string? Transmission { get; set; }
    public string? BodyType { get; set; }
    public string ImageUrl { get; set; }
}
```

### RecommendationResult
```csharp
public class RecommendationResult
{
    public Car Car { get; set; }
    public double SimilarityScore { get; set; }  // 0-1
    public string Explanation { get; set; }
    public FeatureScoreResult? FeatureScores { get; set; }
}
```

### ManualFilterRequest
```csharp
public class ManualFilterRequest
{
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public string? Fuel { get; set; }
    public bool? Transmission { get; set; }
    public string? BodyType { get; set; }
    public int? MinYear { get; set; }
    public int? MaxYear { get; set; }
    public int? MinPower { get; set; }
    public int? Top { get; set; }
    // NOTE: Geen km-stand (zoals gevraagd)
}
```

---

## 🛣️ Routing

### Frontend Routes:

- `/` → Index.cshtml (Tekst modus)
- `/Index` → Index.cshtml
- `/advanced-filters` → AdvancedFilters.cshtml (Manuele modus)
- `/car/{id}` → CarDetail.cshtml (Dynamische detailpagina)
- `/Cars` → Cars.cshtml (Auto's overzicht)

### Backend Routes:

- `/api/health` → HealthController
- `/api/cars` → CarsController
- `/api/cars/{id}` → CarsController
- `/api/recommendations/{id}` → RecommendationsController
- `/api/recommendations/text` → RecommendationsController
- `/api/recommendations/hybrid/manual` → RecommendationsController

---

## 🔧 Configuratie

### Backend (`backend/CarRecommender.Api/appsettings.json`)

```json
{
  "DataSettings": {
    "CsvFileName": "Cleaned_Car_Data_For_App_Fully_Enriched.csv",
    "DataDirectory": "data"
  },
  "Logging": { ... }
}
```

### Frontend (`frontend/CarRecommender.Web/appsettings.json`)

```json
{
  "ApiSettings": {
    "BaseUrl": "https://app-carrecommender-dev-xxx.azurewebsites.net"
  }
}
```

---

## 📦 Dependencies

### Backend:
- .NET 9.0
- ASP.NET Core Web API
- CarRecommender library (src/)

### Frontend:
- .NET 9.0
- ASP.NET Core Razor Pages
- Bootstrap 5
- jQuery

### Core Library:
- .NET 9.0
- System.Text.Json

---

## 🚀 Deployment

### Backend:
- **Platform**: Azure App Service
- **Runtime**: .NET 9.0
- **Status**: ✅ Gedeployed

### Frontend:
- **Platform**: Azure App Service (of lokaal)
- **Runtime**: .NET 9.0
- **Status**: ⚠️ Configuratie aanwezig

---

## 📝 Belangrijke Features

### ✅ Geïmplementeerd:

- [x] Tekst-gebaseerde recommendations (NLP)
- [x] Manuele filters (zonder tekst parsing)
- [x] Dynamische detailpagina routing
- [x] Content-based similarity engine
- [x] Advanced scoring met transparantie
- [x] Explanation generation
- [x] Image handling met fallbacks
- [x] Responsive UI met Bootstrap

### 🔄 In ontwikkeling:

- [ ] Ratings/feedback systeem
- [ ] Azure SQL Database migratie
- [ ] Performance optimalisaties
- [ ] Unit tests

---

## 📚 Documentatie

- **PROJECT_STRUCTURE.md** (dit bestand): Volledige project structuur
- **PROJECT_OVERVIEW.md**: Project overzicht en features
- **MANUAL_FILTERS_DOCUMENTATION.md**: Manual filters uitleg
- **AI_ENGINE_OVERVIEW.md**: AI engine documentatie

---

**Laatste wijzigingen**:
- Manual filters endpoint toegevoegd
- AdvancedFilters pagina toegevoegd
- Dynamische detailpagina routing geïmplementeerd
- Comments toegevoegd aan alle kaart componenten




