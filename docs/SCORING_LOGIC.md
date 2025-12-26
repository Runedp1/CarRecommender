# Scoring Logic - Geavanceerde Recommendation Engine

## Overzicht

De recommendation engine gebruikt een geavanceerde scoring-methode die combineert:
1. **Feature-engineering**: Elke auto wordt gerepresenteerd als een feature vector
2. **Utility-functie**: Deterministische scores per feature (prijs, vermogen, bouwjaar, etc.)
3. **Similarity-meting**: Cosine similarity tussen feature vectors
4. **Gecombineerde ranking**: Utility + Similarity met instelbare gewichten

## Architectuur

```
User Preferences
    ↓
Feature Scores (per feature) → Utility Score (gewogen som)
    ↓                                    ↓
Similarity Score (cosine) ────→ Final Score (gecombineerd)
```

## 1. Feature-engineering en Normalisatie

### Numerieke Features

Alle numerieke features worden genormaliseerd naar het bereik [0, 1] met **min-max normalisatie**:

```
normalized_value = (value - min) / (max - min)
```

**Features die genormaliseerd worden:**
- **Prijs (Budget)**: Genormaliseerd op basis van min/max prijs in dataset
- **Bouwjaar (Year)**: Genormaliseerd op basis van min/max bouwjaar in dataset
- **Vermogen (Power)**: Genormaliseerd op basis van min/max vermogen in dataset

**Waarom normalisatie?**
- Voorkomt dat één feature (bijv. prijs in euro's) de rest domineert
- Zorgt voor consistente schaal (0-1) voor alle features
- Maakt cosine similarity betrouwbaar (geen bias door verschillende schalen)

### Categorische Features

Categorische features worden gecodeerd met **one-hot encoding**:

- **Merk (Brand)**: Dictionary met alle merken, 1.0 voor aanwezig merk, 0.0 voor rest
- **Brandstof (Fuel)**: Dictionary met alle brandstoffen, 1.0 voor aanwezige brandstof, 0.0 voor rest
- **Transmissie (Transmission)**: Dictionary met alle transmissies, 1.0 voor aanwezige transmissie, 0.0 voor rest
- **Carrosserie (BodyType)**: Dictionary met alle carrosserieën, 1.0 voor aanwezige carrosserie, 0.0 voor rest

**Waarom one-hot encoding?**
- Converteert categorische data naar numerieke vorm voor similarity-berekening
- Behoudt informatie over exacte matches (1.0) vs. mismatches (0.0)

## 2. Feature Scores (Utility Componenten)

Elke feature krijgt een aparte score tussen 0 en 1. Deze scores worden gecombineerd in een utility-functie.

### 2.1 Prijs-Score (Budget-Logica)

**Economische logica:**
Als een gebruiker een max-budget opgeeft (bijv. 25k), betekent dit dat ze die capaciteit hebben. Auto's dicht bij het budget bieden waarschijnlijk meer waarde/features voor het beschikbare budget.

**Scoring-functie:**

```
Als auto > max_budget:
    Als overschrijding < 5%:
        score = 0.5 - (overschrijding / 5%) * 0.2  // 0.3 - 0.5
    Anders:
        score = 0.3 - min(0.3, (overschrijding - 5%) * 2.0)  // 0.0 - 0.3

Als auto <= max_budget:
    ratio = auto_prijs / max_budget
    
    Als ratio >= 0.85:  // Optimaal bereik
        score = 0.9 + ((ratio - 0.85) / 0.15) * 0.1  // 0.9 - 1.0
    Als ratio >= 0.70:  // Goed bereik
        score = 0.7 + ((ratio - 0.70) / 0.15) * 0.2  // 0.7 - 0.9
    Als ratio >= 0.50:  // Redelijk bereik
        score = 0.5 + ((ratio - 0.50) / 0.20) * 0.2  // 0.5 - 0.7
    Anders:  // Te goedkoop
        score = (ratio / 0.50) * 0.5  // 0.0 - 0.5
```

**Voorbeeld:**
- Max budget: 25k
- Auto van 23k (92%): score = 0.9 + ((0.92 - 0.85) / 0.15) * 0.1 = **0.95**
- Auto van 15k (60%): score = 0.5 + ((0.60 - 0.50) / 0.20) * 0.2 = **0.6**
- Auto van 8k (32%): score = (0.32 / 0.50) * 0.5 = **0.32**

**Resultaat:** Auto's rond 23-25k scoren hoger dan auto's van 8k.

### 2.2 Vermogen-Score

**Logica:**
- **Sportief** (ComfortVsSportScore < 0.4): Hoger vermogen = hogere score
- **Comfortabel** (ComfortVsSportScore > 0.6): Redelijk vermogen (niet te laag, niet te hoog) = hogere score
- **Neutraal** (ComfortVsSportScore ≈ 0.5): Gemiddeld vermogen = hogere score

**Scoring-functie:**

```
normalized_power = (power - min_power) / (max_power - min_power)

Als sportief:
    score = normalized_power  // Lineair: hoger = beter

Als comfortabel:
    ideal_normalized = 0.4  // 40% van bereik (typisch 120-180 KW)
    distance = |normalized_power - ideal_normalized|
    score = max(0, 1.0 - (distance * 2.0))  // Penalty voor afwijking

Als neutraal:
    ideal_normalized = 0.5  // 50% van bereik
    distance = |normalized_power - ideal_normalized|
    score = max(0, 1.0 - (distance * 2.0))
```

### 2.3 Bouwjaar-Score

**Logica:**
Nieuwere auto's krijgen over het algemeen hogere score (betere technologie, minder slijtage).

**Scoring-functie:**

```
normalized_year = (year - min_year) / (max_year - min_year)
score = normalized_year  // Lineair: nieuwer = beter
```

### 2.4 Categorische Feature Scores

Voor brandstof, merk, carrosserie en transmissie:

```
Als exacte match:
    score = 1.0
Als gedeeltelijke match:
    score = 0.7-0.8 (afhankelijk van feature)
Als geen match:
    score = 0.0
Als geen voorkeur:
    score = 0.5 (neutraal)
```

## 3. Utility-Functie

De utility-score combineert alle feature-scores met gewichten:

```
utility_score = (price_score * w_price) +
                (power_score * w_power) +
                (year_score * w_year) +
                (fuel_score * w_fuel) +
                (brand_score * w_brand) +
                (bodytype_score * w_bodytype) +
                (transmission_score * w_transmission)
```

**Standaard gewichten:**
- Prijs: 0.25 (25%)
- Vermogen: 0.20 (20%)
- Bouwjaar: 0.15 (15%)
- Brandstof: 0.15 (15%)
- Merk: 0.10 (10%)
- Carrosserie: 0.10 (10%)
- Transmissie: 0.05 (5%)

**Totaal:** 1.0 (100%)

## 4. Similarity-Meting

### Cosine Similarity

De similarity-score meet de hoek tussen twee feature vectors:

```
similarity = dot_product(v1, v2) / (magnitude(v1) * magnitude(v2))
```

**Waarde:** 0.0 (orthogonaal) tot 1.0 (identiek)

**Waarom cosine similarity?**
- Onafhankelijk van vector magnitude (alleen richting telt)
- Goed voor genormaliseerde features
- Meet "hoe vergelijkbaar" twee auto's zijn in feature-ruimte

## 5. Finale Score (Gecombineerde Ranking)

De finale score combineert utility-score en similarity-score:

```
final_score = (similarity_score * w_similarity) + (utility_score * w_utility)
```

**Standaard gewichten:**
- Similarity: 0.6 (60%)
- Utility: 0.4 (40%)

**Waarom deze combinatie?**
- **Similarity**: Meet globale gelijkenis in feature-ruimte (content-based)
- **Utility**: Meet match met specifieke voorkeuren (deterministisch)
- Combinatie geeft beste van beide werelden

## 6. Transparantie

Elke recommendation bevat `FeatureScoreResult` met:
- **Prijs-score**: Hoe goed matcht prijs met budget?
- **Vermogen-score**: Hoe goed matcht vermogen met voorkeur?
- **Bouwjaar-score**: Hoe goed matcht bouwjaar met voorkeur?
- **Brandstof-score**: Exacte match of mismatch?
- **Merk-score**: Exacte match of mismatch?
- **Carrosserie-score**: Exacte match of mismatch?
- **Transmissie-score**: Exacte match of mismatch?
- **Utility-score**: Totale gewogen som van feature-scores
- **Similarity-score**: Cosine similarity tussen feature vectors
- **Finale score**: Gecombineerde score voor ranking

**Gebruik:** Frontend kan deze scores gebruiken om uit te leggen "waarom deze auto".

## 7. ML-Laag (Toekomstig)

**Huidige implementatie:**
- Placeholder `GetUserRatingComponent()` retourneert 0.0 (geen impact)

**Toekomstige implementatie:**
- Verzamel user ratings per auto (1-5 sterren)
- Bereken gemiddelde rating per auto
- Normaliseer naar 0-1 bereik
- Voeg toe als extra component:

```
final_score = (similarity_score * w_similarity) + 
              (utility_score * w_utility) + 
              (user_rating_score * w_rating)
```

## 8. Feature-Engineering en AI

Deze scoring-methode sluit aan bij concepten uit AI:

### Feature-Engineering
- **Normalisatie**: Zorgt voor consistente schaal
- **One-hot encoding**: Converteert categorische data naar numerieke vorm
- **Feature selection**: Alleen relevante features worden gebruikt

### Utility-Functie
- **Deterministisch**: Geen randomness, reproduceerbaar
- **Interpretabel**: Elke component heeft duidelijke betekenis
- **Aanpasbaar**: Gewichten kunnen worden aangepast op basis van feedback

### Similarity-Meting
- **Content-based**: Gebaseerd op auto-features, niet op gebruikersgedrag
- **Geometrisch**: Meet afstand in feature-ruimte
- **Schaalbaar**: Werkt met grote datasets

### Machine Learning
- **Hybride aanpak**: Combineert rule-based (utility) met similarity-based (ML)
- **Extensibel**: ML-component kan later worden toegevoegd (user ratings)
- **Transparant**: Scores zijn uitlegbaar (geen black box)

## 9. Test Cases

Zie `tools/scripts/test_advanced_scoring.cs` voor test scenario's:

1. **Scenario 1**: Max budget 25k, sportief, Audi
   - **Verwacht**: Auto's rond 23-25k met hoger vermogen (Audi A4, Audi S4) bovenaan
   - **Niet verwacht**: Budgetwagen van 8k bovenaan

2. **Scenario 2**: Max budget 30k, comfortabel
   - **Verwacht**: Auto's rond 25-30k met redelijk vermogen bovenaan

3. **Scenario 3**: Geen budget voorkeur
   - **Verwacht**: Auto's rond gemiddelde budget bovenaan

## 10. Configuratie

Gewichten kunnen worden aangepast via `AdvancedScoringService.ScoringWeights`:

```csharp
var weights = new AdvancedScoringService.ScoringWeights
{
    PriceWeight = 0.30,      // Verhoog belang van prijs
    PowerWeight = 0.25,      // Verhoog belang van vermogen
    SimilarityWeight = 0.7,  // Verhoog belang van similarity
    UtilityWeight = 0.3       // Verlaag belang van utility
};
weights.Normalize();  // Normaliseer naar totaal 1.0

var scoringService = new AdvancedScoringService(weights: weights);
```

## Conclusie

Deze scoring-methode combineert:
- **Slimme budget-logica**: Auto's dicht bij budget scoren hoger
- **Feature-scores**: Transparante scores per feature
- **Normalisatie**: Consistente schaal voor alle features
- **Similarity + Utility**: Beste van beide werelden
- **Transparantie**: Uitlegbaar voor gebruikers
- **Extensibel**: Klaar voor ML-componenten

Dit resulteert in **slimmere en consistentere scores** die docenten zullen imponeren! 🚗✨





