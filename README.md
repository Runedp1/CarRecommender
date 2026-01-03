# Car Recommendation System

Een recommendation systeem voor auto's geschreven in C# gebaseerd op volgende AI-Algoritmen:
- Rule-Based Filtering (deterministisch)
- Content-Based Recommendation
- Ranking & Scoring Engine
- NLP / Text Parsing (lichte AI)

## Project Structuur (wordt nog aangepast)

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

Het project volgt een **clean architecture** met een duidelijke scheiding van verantwoordelijkheden:

### 1. Data Model Laag (`Car.cs`, `UserWeights.cs`)
* Bevat de **domain entities** die de kern van de applicatie vormen.
* Definieert de datastructuren voor voertuigen en gebruikersvoorkeuren.
* Volledig vrij van business logica.

### 2. Data Laag (`CarRepository.cs`, `Database.cs`, `master_v8_def.csv`)
* Verantwoordelijk voor **data-acquisitie en persistentie**.
* **master_v8_def.csv**: De fysieke dataset (backend) bestaande uit geaggregeerde data op basis van geselecteerde features.
* **Database.cs**: Verzorgt de in-memory representatie en ontsluiting van de CSV-data.
* **CarRepository.cs**: Implementeert het Repository Pattern voor een gestructureerde toegang tot de voertuiggegevens.

### 3. Business Logica Laag (`RecommendationEngine.cs`, `RecommendationService.cs`)
* **RecommendationEngine.cs**: Bevat de core algoritmes voor de similarity-berekeningen.
* **RecommendationService.cs**: De service layer die de logica coördineert tussen de data en de presentatie.
* Focus op testbare, pure business logica zonder directe afhankelijkheid van de UI.

### 4. Presentatie Laag (`Program.cs`, `Menu.cs`)
* Verantwoordelijk voor de **gebruikersinteractie en interface-navigatie**.
* **Menu.cs**: Beheert de visuele weergave en menustructuur in de console.
* **Program.cs**: Fungeert als de 'composition root' die de applicatie opstart en de verschillende lagen verbindt.

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
1. Laadt auto's uit `data/df.master_v8_def.csv`
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

- Content-based recommendations zonder ML.NET  
- Similarity berekening op basis van o.a. vermogen, budget, bouwjaar en brandstof  
- Regelgebaseerde filtering (carrosserietype, transmissie, budgetgrenzen)  
- Robuuste datasetverwerking met fouttolerante parsing  
- Image mapping en image path ondersteuning voor frontend  
- Architectuur gebaseerd op clean-architecture-principes met duidelijke laagscheiding  
- Uitlegbare recommendations (scoring en ranking inzichtelijk)  
- Uitgebreide Nederlandstalige technische documentatie  


## Database (nog aanpassen naar nieuwe)

- **Totaal auto's**: 20,755
- **Met afbeelding**: ~52.6%
- **Unieke merken**: 62
- **Unieke modellen**: 1,389
- **Bereik bouwjaren**: 2000-2023

## Licentie & Copyright

### Afbeeldingen & Databron

De afbeeldingen die in dit project worden gebruikt, zijn afkomstig uit de Kaggle-dataset **“60,000+ Images of Cars”**, samengesteld door **Paul Rondeau**.  
De dataset wordt gebruikt uitsluitend voor educatieve en niet-commerciële doeleinden.

**Bron:**  
Paul Rondeau, *60,000+ Images of Cars*. Kaggle.  
https://www.kaggle.com/datasets/prondeau/the-car-connection-picture-dataset

## Contact

Voor vragen over het project, zie de documentatie in de `docs/` map.
