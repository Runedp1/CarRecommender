# Car Recommendation System

Een content-based recommendation systeem voor auto's geschreven in C#.

## Project Structuur

```
Recommendation_System_New/
├── backend/                      # Backend API
│   ├── CarRecommender.Api/      # ASP.NET Core Web API
│   │   ├── Controllers/         # API Controllers
│   │   ├── Program.cs           # Dependency injection & configuratie
│   │   └── appsettings.json     # Configuratie
│   └── data/                    # Datasets
│       ├── df_master_v8_def.csv # Hoofddataset
│       └── car_image_mapping.json
│
├── frontend/                     # Frontend Web App
│   └── CarRecommender.Web/      # ASP.NET Core Razor Pages
│
├── src/                          # Core Library (gedeelde business logic)
│   ├── Domain/Models/           # Domain entities (Car, UserRating, etc.)
│   ├── Data/                    # Data access (CarRepository, UserRatingRepository)
│   ├── ML/                      # ML services (MlRecommendationService, MlEvaluationService)
│   └── Services/                # Business logic services
│
├── docs/                         # Documentatie
│   ├── AI_ENGINE_OVERVIEW.md    # AI architectuur uitleg
│   ├── SCORING_LOGIC.md         # Scoring algoritme uitleg
│   ├── COLLABORATIVE_FILTERING.md # Collaborative filtering uitleg
│   └── ARCHITECTURE.md          # Systeem architectuur
│
└── CarRecommender.sln           # Visual Studio Solution
```

## Architectuur - Laag Scheiding

Het project volgt een **clean architecture** met duidelijke scheiding van verantwoordelijkheden:

### 1. **Data Model Laag** (`Car.cs`)
- Bevat domain entities (Car, RecommendationResult)
- Geen business logica, alleen data structuren
- Wordt gebruikt door alle andere lagen

### 2. **Data Laag** (`CarRepository.cs`)
- Verantwoordelijk voor data toegang (CSV lezen)
- Implementeert Repository Pattern
- Geen business logica, alleen data transformatie
- Handelt fouten af bij data parsing

### 3. **Business Logica Laag**
- **RecommendationEngine.cs**: Core algoritmes voor similarity berekening
- **RecommendationService.cs**: Service layer die recommendations coördineert
- Geen data toegang, geen presentatie
- Testbare, pure business logica

### 4. **Presentatie Laag** (`Program.cs`)
- Console UI en gebruikersinteractie
- Coördineert tussen lagen
- Geen business logica, geen data toegang

## Hoe te Gebruiken

### Vereisten
- .NET 8.0 SDK
- Python 3.x (voor data processing scripts)

### C# Applicatie Uitvoeren

```bash
cd "Recommendation System"
dotnet run --project CarRecommender.csproj
```

De applicatie:
1. Laadt auto's uit `data/Cleaned_Car_Data_For_App_Fully_Enriched.csv`
2. Genereert image paths voor alle auto's
3. Toont eerste 5 auto's
4. Demonstreert recommendation engine met top 5 suggestions

### Data Processing Scripts

#### Merge alle datasets:
```bash
cd scripts
python merge_all_datasets_comprehensive.py
```

#### Merge nieuwe datasets:
```bash
python merge_new_datasets.py
```

#### Koppel afbeeldingen:
```bash
python link_images_to_cars.py
```

#### Test database kwaliteit:
```bash
python test_database_quality.py
```

## Features

- ✅ Content-based recommendations zonder ML.NET
- ✅ Similarity berekening op basis van Power, Budget, Year, Fuel
- ✅ Robuuste CSV parsing met foutafhandeling
- ✅ Image path ondersteuning voor frontend
- ✅ Clean architecture met duidelijke laag scheiding
- ✅ Uitgebreide Nederlandse documentatie

## Database

- **Totaal auto's**: 20,755
- **Met afbeelding**: ~52.6%
- **Unieke merken**: 62
- **Unieke modellen**: 1,389
- **Bereik bouwjaren**: 2000-2023

## Licentie & Copyright

**BELANGRIJK**: Dit project gebruikt GEEN web scraping voor afbeeldingen. 
Alle afbeeldingen moeten legaal worden verkregen (zie `src/CarRepository.cs` voor details).

## Contact

Voor vragen over het project, zie de documentatie in de `docs/` map.



