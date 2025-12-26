# Collaborative Filtering - User Ratings Systeem

## Overzicht

Het recommendation systeem heeft nu **collaborative filtering** geïmplementeerd op basis van user ratings. Gebruikers kunnen auto's beoordelen met 1-5 sterren, en het systeem gebruikt deze ratings om recommendations te verbeteren voor gebruikers met gelijkaardige voorkeuren.

## Hoe het werkt

### 1. User Ratings

**Wanneer:** Gebruiker kan een auto beoordelen na het bekijken van recommendations

**Wat wordt opgeslagen:**
- Auto ID
- Rating (1-5 sterren)
- User ID (session ID of echte user ID)
- Originele prompt/tekst input
- User preferences (opgeslagen als JSON)
- Recommendation context (text-based, similar-cars, manual-filters)
- Timestamp

**Database:** SQLite database (`data/user_ratings.db`)

### 2. Collaborative Filtering

**Algoritme:**
1. Vind gebruikers met gelijkaardige preferences
2. Kijk welke auto's zij hoog hebben beoordeeld (4+ sterren)
3. Geef deze auto's hogere scores in recommendations
4. Genereer uitleg: "Mensen die ook een sportieve SUV willen vonden deze het best"

**Preference Matching:**
- Budget similarity (30% gewicht)
- Brandstof match (20% gewicht)
- Merk match (15% gewicht)
- Transmissie match (10% gewicht)
- Carrosserie match (15% gewicht)
- Comfort vs Sport score (10% gewicht)

### 3. Score Berekening

**Nieuwe formule:**
```
Base Score = (SimilarityScore * 0.6) + (UtilityScore * 0.4)
↓
With ML = (BaseScore * 0.9) + (MLScore * 0.1)
↓
With Collaborative = (WithML * 0.85) + (CollaborativeScore * 0.15)
```

**Collaborative Score:**
- Gebaseerd op gemiddelde rating van gelijkaardige gebruikers
- Confidence factor: meer ratings = betrouwbaarder
- Alleen auto's met 4+ sterren ratings worden gebruikt

### 4. Uitleg Generatie

**Voorbeelden:**
- "Veel mensen met een sportieve SUV vonden deze uitstekend (⭐4.8, 12 beoordelingen)"
- "Iemand met vergelijkbare voorkeuren vond deze het best (⭐5.0)"
- "5 mensen met een hybride BMW vonden deze goed (⭐4.2)"

## API Endpoints

### Rating Toevoegen
```
POST /api/ratings
{
  "carId": 1234,
  "rating": 5,
  "userId": "session-id",
  "originalPrompt": "Ik wil een sportieve SUV",
  "userPreferences": {
    "maxBudget": 30000,
    "preferredFuel": "diesel",
    "bodyTypePreference": "suv",
    "comfortVsSportScore": 0.3
  },
  "recommendationContext": "text-based"
}
```

### Ratings voor Auto
```
GET /api/ratings/car/{carId}
```

Retourneert:
- Gemiddelde rating
- Totaal aantal ratings
- Verdeling per sterren (1-5)
- Genormaliseerde rating (0-1)

### Ratings voor Gebruiker
```
GET /api/ratings/user/{userId}
```

## Data Flow

```
1. USER RATES CAR
   ↓
2. POST /api/ratings
   ↓
3. UserRatingRepository.AddRatingAsync()
   ↓
4. Rating opgeslagen in SQLite database
   ↓
5. Bij volgende recommendation request:
   ↓
6. CollaborativeFilteringService vindt gelijkaardige gebruikers
   ↓
7. Berekent collaborative score op basis van hun ratings
   ↓
8. Combineert met base score (15% gewicht)
   ↓
9. Genereert uitleg met collaborative data
   ↓
10. User ziet: "Mensen die ook een sportieve SUV willen vonden deze het best"
```

## Database Schema

```sql
CREATE TABLE UserRatings (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    CarId INTEGER NOT NULL,
    Rating INTEGER NOT NULL CHECK (Rating >= 1 AND Rating <= 5),
    UserId TEXT NOT NULL,
    OriginalPrompt TEXT,
    UserPreferencesJson TEXT,
    RecommendationContext TEXT,
    Timestamp DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_carid (CarId),
    INDEX idx_userid (UserId),
    INDEX idx_timestamp (Timestamp)
)
```

## Voorbeeld Scenario

### Stap 1: Gebruiker A vraagt recommendations
```
Prompt: "Ik wil een sportieve SUV met veel vermogen"
→ Recommendations: BMW X5, Audi Q7, Mercedes GLE
```

### Stap 2: Gebruiker A beoordeelt BMW X5
```
POST /api/ratings
{
  "carId": 1234,  // BMW X5
  "rating": 5,
  "originalPrompt": "Ik wil een sportieve SUV met veel vermogen",
  "userPreferences": { ... }
}
```

### Stap 3: Gebruiker B vraagt gelijkaardige recommendations
```
Prompt: "Ik wil een sportieve SUV"
→ Systeem vindt: Gebruiker A had gelijkaardige preferences
→ BMW X5 krijgt hogere score (omdat Gebruiker A het 5 sterren gaf)
→ Uitleg: "Iemand met een sportieve SUV vond deze uitstekend (⭐5.0)"
```

### Stap 4: Meer gebruikers beoordelen
```
Na 10 gelijkaardige gebruikers:
→ Uitleg: "Veel mensen met een sportieve SUV vonden deze uitstekend (⭐4.8, 10 beoordelingen)"
```

## Voordelen

1. **Persoonlijke Recommendations**: Gebaseerd op wat gelijkaardige gebruikers leuk vonden
2. **Social Proof**: "Andere mensen vonden dit goed" verhoogt vertrouwen
3. **Continue Verbetering**: Meer ratings = betere recommendations
4. **Transparantie**: Duidelijke uitleg waarom auto wordt aanbevolen

## Toekomstige Uitbreidingen

1. **User Authentication**: Echte user accounts i.p.v. session IDs
2. **Rating Historie**: Gebruiker kan eigen ratings bekijken
3. **Rating Updates**: Gebruiker kan rating aanpassen
4. **Advanced Matching**: Machine learning voor preference matching
5. **Cold Start Problem**: Aanbevelingen voor nieuwe auto's zonder ratings

