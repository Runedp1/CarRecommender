# Manuele Filters Documentatie

## Overzicht

De **Manuele Filters Modus** is een alternatief voor de tekst-gebaseerde zoekmodus. In plaats van vrije tekst te parsen, kunnen gebruikers expliciet alle filters instellen via een formulier.

## Endpoint

**POST** `/api/recommendations/hybrid/manual`

## Request Body

```json
{
  "minPrice": 10000,          // Optioneel: Minimum prijs in euro's
  "maxPrice": 30000,          // Optioneel: Maximum prijs in euro's
  "brand": "bmw",            // Optioneel: Merk (bijv. "bmw", "audi")
  "model": "x5",             // Optioneel: Model (bijv. "x5", "a4")
  "fuel": "diesel",          // Optioneel: Brandstof ("petrol", "diesel", "hybrid", "electric")
  "transmission": true,      // Optioneel: true = automaat, false = schakel, null = geen voorkeur
  "bodyType": "suv",         // Optioneel: Carrosserie ("suv", "sedan", "hatchback", "station", etc.)
  "minYear": 2015,           // Optioneel: Minimum bouwjaar
  "maxYear": 2023,           // Optioneel: Maximum bouwjaar
  "minPower": 150,           // Optioneel: Minimum vermogen in KW
  "top": 5                   // Optioneel: Aantal recommendations (standaard 5, max 20)
}
```

**Alle velden zijn optioneel**, maar ten minste één filter moet worden ingesteld.

## Response

Zelfde format als tekst modus: `List<RecommendationResult>` met:
- `Car`: Auto object met alle details
- `SimilarityScore`: Match score (0-1)
- `Explanation`: Uitleg waarom deze auto wordt aanbevolen
- `FeatureScores`: Gedetailleerde feature-scores (optioneel)

## Verschil met Tekst Modus

### Tekst Modus (`/api/recommendations/text`)

- **Input**: Vrije tekst beschrijving (bijv. "Ik zoek een automaat met veel vermogen, max 25k euro")
- **Processing**: `TextParserService` parseert tekst met NLP naar `UserPreferences`
- **Voordelen**: 
  - Natuurlijke taal input
  - Gebruiker hoeft niet alle velden te kennen
- **Nadelen**:
  - Parsing kan fouten maken
  - Minder controle over exacte filters

### Manuele Modus (`/api/recommendations/hybrid/manual`)

- **Input**: Expliciete formulier velden
- **Processing**: Directe mapping naar `ManualFilterRequest`, geen parsing
- **Voordelen**:
  - Volledige controle over alle filters
  - Geen parsing fouten
  - Duidelijke interface voor alle opties
- **Nadelen**:
  - Meer velden om in te vullen
  - Minder natuurlijk

## Velden die worden doorgestuurd

| Veld | Type | Beschrijving | Voorbeeld |
|------|------|--------------|-----------|
| `minPrice` | `decimal?` | Minimum prijs in euro's | `10000` |
| `maxPrice` | `decimal?` | Maximum prijs in euro's | `30000` |
| `brand` | `string?` | Merk (lowercase) | `"bmw"` |
| `model` | `string?` | Model naam | `"x5"` |
| `fuel` | `string?` | Brandstof type | `"diesel"` |
| `transmission` | `bool?` | true=automaat, false=schakel | `true` |
| `bodyType` | `string?` | Carrosserie type | `"suv"` |
| `minYear` | `int?` | Minimum bouwjaar | `2015` |
| `maxYear` | `int?` | Maximum bouwjaar | `2023` |
| `minPower` | `int?` | Minimum vermogen in KW | `150` |
| `top` | `int?` | Aantal results (1-20) | `5` |

## Velden die NIET worden ondersteund

- **km-stand**: Opzettelijk weggelaten zoals gevraagd door de gebruiker
- Andere velden kunnen in de toekomst worden toegevoegd

## Frontend Implementatie

### Pagina
- **Route**: `/advanced-filters`
- **Bestand**: `frontend/CarRecommender.Web/Pages/AdvancedFilters.cshtml`
- **Page Model**: `AdvancedFiltersModel` in `AdvancedFilters.cshtml.cs`

### Formulier Velden

1. **Prijs Range**: Min/Max prijs input velden
2. **Merk**: Dropdown met beschikbare merken
3. **Model**: Tekst input veld
4. **Brandstof**: Dropdown (petrol, diesel, hybrid, electric)
5. **Transmissie**: Dropdown (Automaat, Schakel)
6. **Carrosserie**: Dropdown (suv, sedan, hatchback, station, etc.)
7. **Bouwjaar Range**: Min/Max bouwjaar input velden
8. **Minimum Vermogen**: Input veld voor KW
9. **Aantal Resultaten**: Input veld (1-20)

### API Client

```csharp
// In CarApiClient.cs
public async Task<List<RecommendationResult>?> GetRecommendationsFromManualFiltersAsync(ManualFilterRequest request)
```

## Backend Implementatie

### Controller
- **Endpoint**: `POST /api/recommendations/hybrid/manual` in `RecommendationsController.cs`
- **Validatie**: 
  - Request mag niet null zijn
  - Ten minste één filter moet zijn ingesteld
  - Top parameter moet tussen 1 en 20 zijn

### Service
- **Methode**: `RecommendFromManualFilters()` in `RecommendationService.cs`
- **Processing**:
  1. Converteer `ManualFilterRequest` naar `FilterCriteria`
  2. Pas rule-based filtering toe
  3. Gebruik content-based similarity voor ranking
  4. Retourneer top N results

### Request Model
- **Klasse**: `ManualFilterRequest` in `src/Car.cs`
- **Mapping**: Direct naar `RuleBasedFilter.FilterCriteria`

## Gebruik Voorbeelden

### Voorbeeld 1: Budget en Brandstof
```json
{
  "minPrice": 15000,
  "maxPrice": 25000,
  "fuel": "diesel",
  "top": 10
}
```

### Voorbeeld 2: Specifiek Merk en Model
```json
{
  "brand": "bmw",
  "model": "x5",
  "minYear": 2018,
  "transmission": true
}
```

### Voorbeeld 3: Alleen Carrosserie
```json
{
  "bodyType": "suv",
  "minPower": 200,
  "top": 5
}
```

## Technische Details

### Filter Logica

1. **Rule-Based Filtering**: Harde filters worden eerst toegepast
   - Budget range
   - Brandstof match
   - Merk match
   - Model match (partial)
   - Transmissie match
   - Carrosserie match
   - Bouwjaar range
   - Minimum vermogen

2. **Content-Based Similarity**: Auto's worden gerankt op basis van similarity scores
   - Gebruikt `AdvancedScoringService` voor geavanceerde scoring
   - Feature scores per aspect (prijs, vermogen, bouwjaar, etc.)

3. **Ranking**: 
   - Sorteer op similarity score
   - Verwijder dubbele modellen (behoud hoogste score per merk+model)
   - Pak top N results

### Geen km-stand

De km-stand wordt **niet** ondersteund in deze versie. Dit veld is opzettelijk weggelaten zoals gevraagd door de gebruiker. Als dit in de toekomst nodig is, kan het worden toegevoegd aan:
- `ManualFilterRequest` model
- Frontend formulier
- Backend filter logica

## Foutafhandeling

- **400 Bad Request**: 
  - Geen filters ingesteld
  - Ongeldige top parameter (< 1 of > 20)
  
- **500 Internal Server Error**: 
  - Server fouten worden gelogd maar niet naar client gestuurd

## Toekomstige Uitbreidingen

Mogelijke verbeteringen:
- Dynamische dropdown opties (ophalen van API)
- Sliders voor prijs en bouwjaar ranges
- Meerdere merken selecteren
- Geavanceerde vermogen filters (min/max)
- Sorteer opties (prijs, bouwjaar, vermogen)
- Paginatie voor grote result sets








