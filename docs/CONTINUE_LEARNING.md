# Continue Learning Systeem - Documentatie

## Overzicht

Het recommendation systeem heeft nu **continue learning** geïmplementeerd. Het ML model leert van user feedback en wordt automatisch opnieuw getraind wanneer er voldoende nieuwe data beschikbaar is.

## Hoe het werkt

### 1. Feedback Tracking

**Wanneer:** Elke keer dat een gebruiker op een auto klikt (via detailpagina)

**Wat wordt getrackt:**
- Auto ID
- Recommendation score (hoe hoog stond de auto in de lijst)
- Positie in de recommendation lijst (1 = eerste, 2 = tweede, etc.)
- Context (text-based, similar-cars, manual-filters)
- Timestamp

**API Endpoint:**
```
POST /api/feedback/click
{
  "carId": 1234,
  "recommendationScore": 0.85,
  "position": 1,
  "recommendationContext": "text-based",
  "sessionId": "optional-session-id"
}
```

### 2. Aggregated Feedback

Het systeem aggregeert feedback per auto:

- **TotalClicks**: Totaal aantal clicks/views
- **ClickThroughRate (CTR)**: Aantal clicks / aantal keer aanbevolen
- **PopularityScore**: Genormaliseerde populairiteit (0-1) op basis van:
  - CTR (40% gewicht)
  - Positieve feedback (30% gewicht)
  - Gemiddelde positie (20% gewicht)
  - Negatieve feedback penalty (10% gewicht)

### 3. Retraining Mechanisme

**Wanneer wordt het model opnieuw getraind?**

Het model wordt automatisch opnieuw getraind wanneer:
- Er minimaal **50 nieuwe feedback entries** zijn sinds laatste training
- Er minimaal **1 uur** is verstreken sinds laatste training

**Hoe werkt retraining?**

1. **Data verzameling**: 
   - Genereert recommendations voor 100 sample auto's
   - Geeft voorkeur aan auto's met feedback (hogere populairiteit eerst)

2. **Label aanpassing**:
   - Combineert recommendation score (70%) met populairiteit score (30%)
   - Auto's met hoge populairiteit krijgen hogere labels

3. **Model training**:
   - Traint ML.NET regression model met nieuwe data
   - Model leert: "Auto's met deze features + hoge populairiteit = hogere score"

4. **Asynchroon**:
   - Retraining gebeurt op de achtergrond (niet blokkerend)
   - Recommendations blijven werken tijdens retraining

### 4. Automatische Retraining Scheduler

**Background Service**: `RetrainingBackgroundService`

- Draait op de achtergrond
- Checkt elke **30 minuten** of retraining nodig is
- Voert automatisch retraining uit indien nodig
- Logt alle retraining activiteit

### 5. Performance Monitoring

**API Endpoint:**
```
GET /api/feedback/performance
```

Retourneert:
- Model training status
- Laatste training tijd
- Totaal aantal feedback entries
- Gemiddelde CTR
- Gemiddelde populairiteit score
- Feedback distributie (high/medium/low popularity)

## Data Flow

```
1. USER CLICKS ON CAR
   ↓
2. Frontend → POST /api/feedback/click
   ↓
3. FeedbackRepository.AddFeedback()
   ↓
4. AggregatedFeedback wordt bijgewerkt
   ↓
5. Background Service checkt (elke 30 min)
   ↓
6. Als 50+ nieuwe feedback: Retraining
   ↓
7. Model wordt opnieuw getraind met feedback data
   ↓
8. Nieuwe recommendations gebruiken verbeterd model
```

## Voorbeeld Scenario

### Dag 1 - Initial Training
- Model wordt getraind op 50 sample auto's
- Leert basis patterns: "BMW diesel SUV's scoren 0.75"

### Dag 2 - User Feedback
- 100 gebruikers klikken op recommendations
- Feedback wordt getrackt:
  - Auto ID 1234: 20 clicks, CTR 0.4, PopularityScore 0.8
  - Auto ID 5678: 5 clicks, CTR 0.1, PopularityScore 0.3

### Dag 3 - Retraining
- Background service detecteert 100 nieuwe feedback entries
- Retraining wordt getriggerd
- Model leert: "Auto 1234 heeft hoge populairiteit → verhoog score"
- Model leert: "Auto 5678 heeft lage populairiteit → verlaag score"

### Dag 4 - Verbeterde Recommendations
- Nieuwe recommendations gebruiken verbeterd model
- Auto 1234 krijgt hogere score (omdat populair)
- Auto 5678 krijgt lagere score (omdat niet populair)

## API Endpoints

### Feedback Tracking
- `POST /api/feedback/click` - Track click feedback
- `GET /api/feedback/car/{carId}` - Haal feedback op voor auto
- `GET /api/feedback/stats` - Algemene feedback statistieken
- `GET /api/feedback/performance` - ML model performance metrics

## Configuratie

Retraining parameters (in `ModelRetrainingService`):
- `minNewFeedbackForRetraining`: 50 (standaard)
- `retrainingSampleSize`: 100 (standaard)
- `minTimeBetweenRetraining`: 1 uur (standaard)

Background service interval:
- `checkInterval`: 30 minuten (standaard)

## Monitoring

### Logs
Alle retraining activiteit wordt gelogd:
```
[INFO] Automatische retraining uitgevoerd: Model succesvol opnieuw getraind. 
       Training data: 500, Feedback: 150
```

### Performance Metrics
Gebruik `/api/feedback/performance` om te monitoren:
- Of model getraind is
- Wanneer laatste training was
- Hoeveel feedback er is
- Gemiddelde CTR en populairiteit

## Toekomstige Uitbreidingen

1. **User Ratings**: Expliciete 1-5 sterren ratings
2. **Purchase Tracking**: Track welke auto's worden gekocht
3. **Personalization**: User-specifieke modellen
4. **A/B Testing**: Test verschillende modellen
5. **Database Persistence**: Opslaan van feedback in database (nu in-memory)

## Belangrijke Notities

- **In-memory storage**: Feedback wordt nu in-memory opgeslagen (vervalt bij restart)
- **Voor productie**: Implementeer database persistence voor feedback
- **Performance**: Retraining gebeurt asynchroon, blokkeert geen requests
- **Fallback**: Als retraining faalt, blijft oude model werken






