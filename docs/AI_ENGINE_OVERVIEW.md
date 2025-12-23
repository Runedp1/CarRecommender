# AI Engine Overzicht

Dit document beschrijft de AI-architectuur van de Car Recommendation Engine, gebaseerd op de concepten uit de AI-cursus (Old AI vs New AI).

## Architectuur Overzicht

De recommendation engine combineert twee AI-benaderingen:

1. **Old AI - Rule-based filtering**: Harde filters die de candidate set bepalen
2. **New AI - Content-based recommender**: Feature vectors en cosine similarity voor intelligente matching

## Old AI - Rule-Based Filtering

### Concept
Rule-based filtering gebruikt expliciete regels om auto's te filteren. Dit is de eerste laag van filtering die de **candidate set** bepaalt. Niets mag deze regels omzeilen.

### Implementatie: `RuleBasedFilter`

De `RuleBasedFilter` klasse filtert auto's op basis van harde criteria:

- **Budget**: Min/max prijs filters
- **Brandstof**: Exacte match of varianten (bijv. "hybrid" matcht "plug-in hybrid")
- **Merk**: Exacte match (case-insensitive)
- **Carrosserie**: Body type matching met varianten (bijv. "suv" matcht "jeep", "4x4")
- **Transmissie**: Automatic vs manual matching
- **Bouwjaar**: Min/max jaar filters

### Gebruik

```csharp
var filter = new RuleBasedFilter();
var criteria = new RuleBasedFilter.FilterCriteria
{
    MaxBudget = 25000,
    PreferredFuel = "hybrid",
    AutomaticTransmission = true
};
var candidateCars = filter.FilterCars(allCars, criteria);
```

### Waarom Old AI?

- **Voorspelbaarheid**: Gebruikers verwachten dat hun harde voorkeuren worden gerespecteerd
- **Efficiëntie**: Reduceert de dataset voordat dure similarity berekeningen worden uitgevoerd
- **Transparantie**: Duidelijke regels die gebruikers kunnen begrijpen

## New AI - Content-Based Recommender

### Concept
Content-based recommendation gebruikt feature vectors en similarity meting om auto's te vergelijken op basis van hun intrinsieke eigenschappen, niet alleen op expliciete filters.

### Componenten

#### 1. CarFeatureVector

Elke auto wordt gerepresenteerd als een numerieke feature vector:

- **Genormaliseerde numerieke waarden** (0.0 - 1.0):
  - Prijs (min-max normalisatie)
  - Bouwjaar (min-max normalisatie)
  - Vermogen/KW (min-max normalisatie)

- **One-hot encoding voor categorische velden**:
  - Merk (1.0 voor aanwezige merk, 0.0 voor alle anderen)
  - Brandstof (1.0 voor aanwezige brandstof, 0.0 voor alle anderen)
  - Transmissie (1.0 voor aanwezige transmissie, 0.0 voor alle anderen)
  - Carrosserie (1.0 voor aanwezige carrosserie, 0.0 voor alle anderen)

#### 2. CarFeatureVectorFactory

De factory converteert `Car` objecten naar `CarFeatureVector` objecten:

- Initialiseert normalisatie ranges op basis van alle auto's in de dataset
- Maakt one-hot encoding dictionaries voor alle mogelijke categorische waarden
- Normaliseert numerieke waarden naar 0-1 bereik
- Creëert ideale feature vectors op basis van user preferences

#### 3. SimilarityService

Berekent **cosine similarity** tussen feature vectors:

- Cosine similarity meet de hoek tussen twee vectoren
- Waarde tussen 0.0 (orthogonaal) en 1.0 (identiek)
- Voor auto recommendations gebruiken we alleen positieve waarden (0.0 - 1.0)

**Formule**: `similarity = dot_product(v1, v2) / (magnitude(v1) * magnitude(v2))`

#### 4. RankingService

Combineert verschillende scores en voegt controlled randomness toe:

**Gecombineerde Score**:
```
final_score = (similarity_score * similarity_weight) + 
              (preference_match_score * preference_weight) + 
              (randomness_component * randomness_weight)
```

**Gewichten** (configureerbaar):
- Similarity weight: 0.7 (70% van de score)
- Preference match weight: 0.2 (20% van de score)
- Randomness weight: 0.1 (10% van de score)

**Preference Matching**:
- Sportief: Bonus voor auto's met hoger vermogen
- Comfort: Bonus voor auto's met redelijk vermogen (niet te laag, niet te hoog)

**Controlled Randomness**:
- Alleen toegepast als base score > 0.5 (voorkomt dat slechte matches naar boven komen)
- Kleine random variatie voor variatie bij gelijke scores
- Gebruikt Fisher-Yates shuffle voor controlled randomness bij ranking

### Waarom New AI?

- **Flexibiliteit**: Kan subtiele voorkeuren leren zonder expliciete regels
- **Personalization**: Aanpassing aan individuele voorkeuren
- **Variatie**: Controlled randomness voorkomt repetitieve resultaten

## Samenwerking: Old AI + New AI

### Workflow

1. **Old AI - Rule-based filtering**:
   - Parse user preferences uit tekst
   - Converteer naar `FilterCriteria`
   - Filter alle auto's → **candidate set**

2. **New AI - Content-based similarity**:
   - Maak ideale feature vector op basis van preferences
   - Bereken cosine similarity voor elke auto in candidate set
   - Bereken gecombineerde score (similarity + preference matching)

3. **Ranking met controlled randomness**:
   - Sorteer op gecombineerde score
   - Voeg controlled randomness toe voor variatie
   - Retourneer top N recommendations

### Voorbeeld

```csharp
// 1. Old AI: Filter op harde regels
var filter = new RuleBasedFilter();
var criteria = filter.ConvertPreferencesToCriteria(prefs);
var candidates = filter.FilterCars(allCars, criteria);

// 2. New AI: Bereken similarity
var factory = new CarFeatureVectorFactory();
factory.Initialize(allCars);
var idealVector = factory.CreateIdealVector(prefs, candidates);

var similarityService = new SimilarityService();
var rankingService = new RankingService();

foreach (var car in candidates)
{
    var similarity = similarityService.CalculateSimilarity(car, idealVector, factory);
    var score = rankingService.CalculateCombinedScore(car, similarity, prefs);
    // ...
}
```

## Configuratie

### Ranking Gewichten

De gewichten kunnen worden aangepast in `RankingService.RankingWeights`:

```csharp
var weights = new RankingService.RankingWeights
{
    SimilarityWeight = 0.7,      // 70% similarity
    PreferenceMatchWeight = 0.2,  // 20% preference matching
    RandomnessWeight = 0.1        // 10% randomness
};
var rankingService = new RankingService(weights);
```

### Hoe Gewichten Kiezen?

1. **Similarity Weight (0.7)**:
   - Hoog gewicht omdat cosine similarity de kern is van content-based recommendation
   - Meet objectieve gelijkenis tussen auto's

2. **Preference Match Weight (0.2)**:
   - Middelmatig gewicht voor preference matching (sportief/comfort)
   - Voegt subjectieve voorkeuren toe zonder similarity te overschaduwen

3. **Randomness Weight (0.1)**:
   - Laag gewicht om variatie toe te voegen zonder kwaliteit te verliezen
   - Alleen actief bij goede scores (> 0.5)

### Experimenten

Gewichten kunnen worden geoptimaliseerd via:
- A/B testing met gebruikers
- Offline evaluatie (precision@k, recall@k)
- User feedback analyse

## Cursus Concepten

### Old AI (Rule-Based)
- **Expliciete regels**: Harde filters die niet kunnen worden omzeild
- **Deterministisch**: Zelfde input = zelfde output
- **Transparant**: Gebruikers begrijpen waarom auto's worden gefilterd

### New AI (Content-Based)
- **Feature vectors**: Numerieke representatie van auto's
- **Similarity meting**: Cosine similarity voor gelijkenis
- **Learning**: Aanpassing aan user preferences via gewichten
- **Controlled randomness**: Variatie zonder kwaliteit te verliezen

## Bestanden

- `src/RuleBasedFilter.cs`: Old AI - Rule-based filtering
- `src/CarFeatureVector.cs`: New AI - Feature vector representatie
- `src/CarFeatureVectorFactory.cs`: New AI - Factory voor feature vectors
- `src/SimilarityService.cs`: New AI - Cosine similarity berekening
- `src/RankingService.cs`: New AI - Ranking met controlled randomness
- `src/RecommendationService.cs`: Coördineert Old AI + New AI

## Toekomstige Uitbreidingen

- **Collaborative Filtering**: User-user similarity (als ratings beschikbaar zijn)
- **Hybrid Approach**: Combineer content-based + collaborative filtering
- **Machine Learning**: Gebruik ML models voor betere feature extraction
- **Deep Learning**: Neural networks voor complexere preference learning

