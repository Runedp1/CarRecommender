# Car Recommendation System

Een content-based recommendation systeem voor auto's geschreven in C#.

## Project Structuur

```
Recommendation System/
├── src/                          # C# broncode
│   ├── Program.cs               # Presentatie laag - Console UI en entry point
│   ├── Car.cs                   # Data model laag - Domain entities
│   ├── CarRepository.cs         # Data laag - CSV data toegang
│   ├── RecommendationEngine.cs  # Business logica - Similarity algoritmes
│   └── RecommendationService.cs # Business logica - Recommendation coordinatie
│
├── data/                         # CSV datasets
│   ├── Cleaned_Car_Data_For_App_Fully_Enriched.csv
│   ├── vehicles.csv
│   ├── car_price_prediction_.csv
│   └── ...
│
├── notebooks/                    # Python notebooks voor analyse
│   └── recommender.ipynb
│
├── scripts/                      # Python scripts voor data processing
│   ├── merge_all_datasets_comprehensive.py  # Comprehensive merge script
│   ├── merge_all_datasets.py                # Originele merge
│   ├── merge_new_datasets.py                # Merge nieuwe datasets
│   ├── link_images_to_cars.py               # Image koppeling
│   └── test_database_quality.py             # Kwaliteitstests
│
├── docs/                         # Documentatie
│   ├── FRONTEND_IMAGE_GUIDE.md
│   ├── MERGE_SUMMARY.md
│   └── ...
│
└── images/                       # Auto afbeeldingen (lege map structuur)
    └── {brand}/
        └── {model}/
            └── {id}.jpg
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



