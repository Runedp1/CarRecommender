# CarRecommender Project - Overzicht

## Globale Architectuur

Dit project is een **hybride AI-aanbevelingssysteem** voor auto's met de volgende componenten:

### Backend (C#/.NET 9.0 API)
- **Locatie**: `backend/CarRecommender.Api/`
- **Type**: ASP.NET Core Web API
- **Functie**: RESTful API die recommendation endpoints aanbiedt
- **Data bron**: CSV-bestand (`Cleaned_Car_Data_For_App_Fully_Enriched.csv`)
- **Status**: ✅ Bouwt succesvol, geen compile errors

### Frontend (ASP.NET Core Razor Pages)
- **Locatie**: `frontend/CarRecommender.Web/`
- **Type**: ASP.NET Core Razor Pages web applicatie
- **Functie**: Web interface voor gebruikers om auto's te zoeken en recommendations te bekijken
- **API integratie**: Gebruikt `CarApiClient` service om met backend API te communiceren
- **Status**: ✅ Bouwt succesvol, geen compile errors

### Core Library (Business Logic)
- **Locatie**: `src/`
- **Type**: .NET 9.0 class library
- **Functie**: Bevat alle recommendation algoritmes, data modellen en business logica
- **Gedeeld door**: Backend API en (potentieel) andere clients

---

## Belangrijkste Endpoints

### Backend API Endpoints

#### 1. **GET /api/health**
- **Beschrijving**: Health check endpoint voor monitoring
- **Response**: `{ "status": "OK" }`
- **Gebruik**: Azure App Service health monitoring

#### 2. **GET /api/cars**
- **Beschrijving**: Haalt alle auto's op met paginatie
- **Query parameters**:
  - `page` (optioneel, default: 1): Pagina nummer
  - `pageSize` (optioneel, default: 20, max: 100): Items per pagina
- **Response**: `PagedResult<Car>` met `Items`, `Page`, `PageSize`, `TotalCount`, `TotalPages`
- **Status codes**: 200 OK, 400 Bad Request, 500 Internal Server Error

#### 3. **GET /api/cars/{id}**
- **Beschrijving**: Haalt details op van één specifieke auto
- **Path parameter**: `id` (int): Auto ID
- **Response**: `Car` object
- **Status codes**: 200 OK, 404 Not Found, 500 Internal Server Error

#### 4. **GET /api/recommendations/{id}**
- **Beschrijving**: Haalt top-N recommendations op voor een specifieke auto
- **Path parameter**: `id` (int): Auto ID
- **Query parameter**: `top` (optioneel, default: 5, max: 20): Aantal recommendations
- **Response**: `List<RecommendationResult>` met auto's gesorteerd op similarity score
- **Status codes**: 200 OK, 400 Bad Request, 404 Not Found, 500 Internal Server Error

#### 5. **POST /api/recommendations/text**
- **Beschrijving**: Genereert recommendations op basis van tekst input (NLP)
- **Request body**: 
  ```json
  {
    "text": "Ik zou liever een automaat hebben met veel vermogen, max 25k euro",
    "top": 5
  }
  ```
- **Response**: `List<RecommendationResult>` met auto's en explanations
- **Status codes**: 200 OK, 400 Bad Request, 500 Internal Server Error

#### 6. **POST /api/recommendations/hybrid/manual**
- **Beschrijving**: Genereert recommendations op basis van manuele filters (zonder tekst parsing)
- **Request body**: 
  ```json
  {
    "minPrice": 10000,
    "maxPrice": 30000,
    "brand": "bmw",
    "model": "x5",
    "fuel": "diesel",
    "transmission": true,
    "bodyType": "suv",
    "minYear": 2015,
    "maxYear": 2023,
    "minPower": 150,
    "top": 5
  }
  ```
- **Response**: `List<RecommendationResult>` met auto's en explanations
- **Status codes**: 200 OK, 400 Bad Request, 500 Internal Server Error
- **Verschil met tekst modus**: Geen NLP parsing, directe formulier velden

---

## Recommendation Engine - Componenten

### 1. **Rule-Based Filtering**
- **Locatie**: `src/RecommendationService.cs` → `FilterCarsByPreferences()`
- **Functie**: Filtert auto's op basis van user preferences:
  - Budget (max budget)
  - Brandstof (petrol/diesel/hybrid/electric)
  - Vermogen (min power in KW of score 0-1)
  - Realistische grenzen (prijs: €300-€500k, vermogen: 20-800 KW)
- **Status**: ✅ Geïmplementeerd

### 2. **Content-Based Similarity (Feature Vectors)**
- **Locatie**: `src/RecommendationEngine.cs` → `CalculateSimilarity()`
- **Functie**: Berekent similarity scores tussen auto's op basis van:
  - **Vermogen** (Power): Genormaliseerde afstand (0-1)
  - **Budget** (Prijs): Genormaliseerde afstand (0-1)
  - **Bouwjaar** (Year): Genormaliseerde afstand (0-1)
  - **Brandstof** (Fuel): Exact match (1.0), gedeeltelijke match (0.5), geen match (0.0)
- **Gewichten**: Standaard 25% power, 30% budget, 20% year, 25% fuel
- **Custom gewichten**: Ondersteund voor tekst-gebaseerde recommendations
- **Status**: ✅ Geïmplementeerd

### 3. **Text Parsing (NLP)**
- **Locatie**: `src/TextParserService.cs`
- **Functie**: Parse Nederlandse tekst input naar `UserPreferences`:
  - **Budget**: Herkent "max 25k", "rond de 30.000", "budget tot 20k"
  - **Brandstof**: Herkent "benzine", "diesel", "hybride", "elektrisch"
  - **Transmissie**: Herkent "automaat", "schakel", "handbak"
  - **Vermogen**: Herkent "veel vermogen" (0.8), "voldoende" (0.5), of exacte waarden "200 KW"
  - **Body type**: Herkent "SUV", "station", "sedan", "hatchback", "cabrio"
  - **Comfort/Sport**: Herkent "sportief", "comfortabel", "lange ritten"
- **Gewichten**: Detecteert belangrijkheid uit context:
  - **Cruciaal** (1.5): "moet", "absoluut", "cruciaal"
  - **Belangrijk** (1.0): "belangrijk", "wilt", "nodig"
  - **Liever** (0.6): "liever", "bij voorkeur", "graag"
  - **Optioneel** (0.3): Impliciet
- **Status**: ✅ Geïmplementeerd

### 4. **Explanation Builder**
- **Locatie**: `src/ExplanationBuilder.cs`
- **Functie**: Genereert Nederlandstalige uitleg waarom een auto wordt aanbevolen
- **Features**: Toont gewichten voor transparantie, matcht preferences met auto eigenschappen
- **Status**: ✅ Geïmplementeerd

### 5. **Ratings / Feedback**
- **Status**: ❌ **Nog niet geïmplementeerd**
- **TODO**: Implementeer ratings systeem voor collaborative filtering

---

## Data Laag

### CarRepository
- **Locatie**: `src/CarRepository.cs`
- **Interface**: `src/ICarRepository.cs`
- **Functie**: 
  - Leest CSV-bestand bij initialisatie
  - Filtert onrealistische waarden (prijs < €300, vermogen < 20 KW, etc.)
  - Genereert image paths en URLs
  - Biedt filtering op merk, budget, vermogen, jaar, brandstof
- **Data bestand**: `backend/data/Cleaned_Car_Data_For_App_Fully_Enriched.csv`
- **Status**: ✅ Geïmplementeerd (CSV-based)

### Toekomstige Migratie
- **TODO**: Maak `SqlCarRepository` implementatie voor Azure SQL Database
- **Voordeel**: Snellere queries, schaalbaarheid, real-time updates

---

## Frontend Screens / Flows

### 1. **Homepage (Index)**
- **Route**: `/` of `/Index`
- **Bestand**: `frontend/CarRecommender.Web/Pages/Index.cshtml`
- **Functie**: 
  - Tekst input formulier voor user preferences
  - Toont recommendations met cards (merk, model, prijs, brandstof, bouwjaar, vermogen, similarity score, explanation)
  - Image fallback systeem (ImageUrl → Auto-Data.net → Placeholder)
- **Status**: ✅ Geïmplementeerd

### 2. **Geavanceerde Filters**
- **Route**: `/advanced-filters`
- **Bestand**: `frontend/CarRecommender.Web/Pages/AdvancedFilters.cshtml`
- **Functie**: 
  - Manuele filters modus (zonder tekst parsing)
  - Formulier met dropdowns en input velden
  - Merk, model, brandstof, transmissie, carrosserie
  - Min/max prijs, bouwjaar, vermogen
  - Toont recommendations met cards
- **Status**: ✅ Geïmplementeerd

### 3. **Auto Detail Pagina**
- **Route**: `/car/{id}` (dynamische route met parameter)
- **Bestand**: `frontend/CarRecommender.Web/Pages/CarDetail.cshtml`
- **Functie**: 
  - Toont auto details op basis van route parameter `{id}`
  - Image carousel
  - Alle auto eigenschappen
  - Dynamische routing: elke kaart linkt naar zijn eigen detailpagina
- **Status**: ✅ Geïmplementeerd (volledig dynamisch)

### 4. **Cars Overzicht**
- **Route**: `/Cars`
- **Bestand**: `frontend/CarRecommender.Web/Pages/Cars.cshtml`
- **Functie**: 
  - Toont alle auto's uit de database
  - Elke kaart linkt naar `/car/{id}` detailpagina
- **Status**: ✅ Geïmplementeerd

### 5. **Error Page**
- **Route**: `/Error`
- **Functie**: Toont foutmeldingen
- **Status**: ✅ Geïmplementeerd

---

## Wat Werkt Nu

### ✅ Backend
- [x] API bouwt zonder errors
- [x] Health check endpoint
- [x] Cars endpoints (GET all, GET by ID) met paginatie
- [x] Recommendations endpoints (GET by ID, POST from text)
- [x] CSV data loading met filtering
- [x] Dependency injection configuratie
- [x] Error handling en logging
- [x] Swagger UI in development mode

### ✅ Frontend
- [x] Web applicatie bouwt zonder errors
- [x] Razor Pages structuur
- [x] API client service (`CarApiClient`)
- [x] Homepage met tekst input formulier
- [x] Recommendations weergave met cards
- [x] Error handling
- [x] Bootstrap styling

### ✅ Recommendation Engine
- [x] Content-based similarity berekening
- [x] Rule-based filtering op preferences
- [x] Text parsing (NLP) voor Nederlandse input
- [x] Weighted similarity met custom gewichten
- [x] Explanation generation
- [x] Deduplicatie (één auto per merk+model)

### ✅ Data
- [x] CSV parsing met robuuste error handling
- [x] Realistische waarde filtering
- [x] Image path generation
- [x] Flexibele kolom mapping (Nederlands/Engels)

---

## Duidelijke TODO's (Gebaseerd op Code Analyse)

### 🔴 Hoog Prioriteit

1. **Ratings / Feedback Systeem**
   - Implementeer user ratings voor auto's
   - Sla ratings op (database of bestand)
   - Integreer collaborative filtering in recommendation algoritme
   - **Locatie**: Nieuwe service `RatingService` in `src/`

2. **Azure SQL Database Migratie**
   - Maak `SqlCarRepository` implementatie van `ICarRepository`
   - Migreer CSV data naar SQL database
   - Update `Program.cs` om `SqlCarRepository` te gebruiken
   - **Locatie**: Nieuwe klasse in `src/` + connection string configuratie

3. **Transmissie (Automaat/Schakel) Ondersteuning**
   - Voeg `Transmission` property toe aan `Car` model
   - Update CSV parsing om transmissie te lezen
   - Update filtering en similarity berekening
   - **Locatie**: `src/Car.cs`, `src/CarRepository.cs`, `src/RecommendationService.cs`

4. **Body Type Ondersteuning**
   - Voeg `BodyType` property toe aan `Car` model
   - Update CSV parsing
   - Verbeter body type matching in `ExplanationBuilder`
   - **Locatie**: `src/Car.cs`, `src/CarRepository.cs`, `src/ExplanationBuilder.cs`

### 🟡 Medium Prioriteit

5. **Frontend: Auto Detail Pagina**
   - Maak detail pagina voor individuele auto's
   - Toon alle eigenschappen
   - Toon recommendations voor deze auto
   - **Locatie**: `frontend/CarRecommender.Web/Pages/CarDetail.cshtml`

6. **Frontend: Filtering UI**
   - Voeg filter formulier toe aan Cars pagina
   - Filter op merk, budget range, vermogen, jaar, brandstof
   - **Locatie**: `frontend/CarRecommender.Web/Pages/Cars.cshtml`

7. **API: Filtering Endpoint**
   - Maak `GET /api/cars/filter` endpoint
   - Gebruik bestaande `FilterCars()` methode
   - **Locatie**: `backend/CarRecommender.Api/Controllers/CarsController.cs`

8. **Performance Optimalisatie**
   - Cache similarity berekeningen
   - Indexering voor snellere filtering
   - Parallel processing voor grote datasets
   - **Locatie**: `src/RecommendationService.cs`, `src/RecommendationEngine.cs`

9. **Image Management**
   - Verbeter image URL generatie (gebruik echte auto API's)
   - Cache image URLs
   - Fallback strategie verbeteren
   - **Locatie**: `src/CarRepository.cs` → `GenerateImageUrl()`

### 🟢 Laag Prioriteit

10. **Testing**
    - Unit tests voor `RecommendationEngine`
    - Unit tests voor `TextParserService`
    - Integration tests voor API endpoints
    - **Locatie**: Nieuwe `tests/` directory

11. **Documentatie**
    - API documentatie verbeteren (Swagger annotations)
    - Code comments uitbreiden
    - User guide voor frontend

12. **Monitoring & Logging**
    - Application Insights integratie voor Azure
    - Performance metrics
    - Error tracking

13. **Caching**
    - Cache recommendations per user query
    - Cache car data in memory (al gedaan via singleton)
    - Redis cache voor Azure deployment

---

## Project Structuur Samenvatting

```
Recommendation_System_New/
├── backend/
│   ├── CarRecommender.Api/          # ASP.NET Core Web API
│   │   ├── Controllers/
│   │   │   ├── CarsController.cs
│   │   │   ├── RecommendationsController.cs  # + POST /hybrid/manual
│   │   │   └── HealthController.cs
│   │   ├── Program.cs
│   │   └── appsettings.json
│   ├── data/                        # CSV datasets
│   │   └── Cleaned_Car_Data_For_App_Fully_Enriched.csv
│   └── images/                      # Auto afbeeldingen (49k+)
│
├── frontend/
│   └── CarRecommender.Web/          # ASP.NET Core Razor Pages
│       ├── Pages/
│       │   ├── Index.cshtml         # Tekst modus
│       │   ├── AdvancedFilters.cshtml  # Manuele filters modus
│       │   ├── CarDetail.cshtml    # Dynamische detailpagina /car/{id}
│       │   ├── Cars.cshtml         # Auto's overzicht
│       │   └── Shared/
│       │       └── _Layout.cshtml  # Master layout
│       ├── Models/
│       │   ├── Car.cs
│       │   ├── RecommendationResult.cs
│       │   ├── RecommendationTextRequest.cs
│       │   └── ManualFilterRequest.cs  # Nieuw
│       ├── Services/
│       │   └── CarApiClient.cs     # + GetRecommendationsFromManualFiltersAsync()
│       └── appsettings.json
│
├── src/                             # Core business logic library
│   ├── Car.cs                       # + ManualFilterRequest
│   ├── CarRepository.cs
│   ├── ICarRepository.cs
│   ├── RecommendationEngine.cs
│   ├── RecommendationService.cs    # + RecommendFromManualFilters()
│   ├── IRecommendationService.cs   # + RecommendFromManualFilters()
│   ├── TextParserService.cs
│   ├── RuleBasedFilter.cs
│   ├── AdvancedScoringService.cs
│   ├── RankingService.cs
│   └── ExplanationBuilder.cs
│
├── docs/
│   ├── PROJECT_STRUCTURE.md        # Nieuw: Volledige structuur
│   ├── PROJECT_OVERVIEW.md         # Updated
│   ├── MANUAL_FILTERS_DOCUMENTATION.md  # Nieuw
│   └── [andere documentatie]
│
├── tools/
│   ├── notebooks/
│   └── scripts/
│
└── CarRecommender.sln
```

---

## Configuratie

### Backend API
- **Port**: Configureerbaar via `launchSettings.json`
- **Data**: CSV bestand via `appsettings.json` → `DataSettings:CsvFileName`
- **Swagger**: Beschikbaar in Development mode

### Frontend Web
- **API URL**: Configureerbaar via `appsettings.json` → `ApiSettings:BaseUrl`
- **Huidige configuratie**: Azure App Service URL (Production)

---

## Deployment Status

- **Backend**: ✅ Gedeployed naar Azure App Service
  - URL: `https://app-carrecommender-dev-h0agdkbfbcb3fxg7.westeurope-01.azurewebsites.net`
- **Frontend**: ⚠️ Configuratie wijst naar Azure API, maar deployment status onbekend

---

## Conclusie

Het project heeft een **solide basis** met:
- ✅ Werkende backend API met recommendation endpoints
- ✅ Werkende frontend met tekst-gebaseerde zoekfunctionaliteit
- ✅ Content-based recommendation engine met similarity berekening
- ✅ NLP text parsing voor Nederlandse input
- ✅ Clean architecture met duidelijke laag scheiding

**Belangrijkste ontbrekende features**:
- ❌ Ratings/feedback systeem (voor collaborative filtering)
- ❌ Database migratie (nu alleen CSV)
- ❌ Transmissie en body type ondersteuning in data model

Het project is **klaar voor verdere ontwikkeling** en kan gemakkelijk uitgebreid worden met de genoemde TODO's.



