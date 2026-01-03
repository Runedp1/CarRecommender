# Database Reset Guide - User Ratings

## Overzicht

Wanneer de dataset wordt verbeterd, kunnen CarIds veranderen. Dit betekent dat oude ratings niet meer overeenkomen met de juiste auto's. Deze guide legt uit hoe je de ratings database kunt resetten.

## Database Locatie

**Lokaal:**
```
data/user_ratings.db
```

**Of:**
```
backend/data/user_ratings.db
```

De exacte locatie wordt gelogd bij startup:
```
[INFO] User Ratings Database: C:\...\data\user_ratings.db
```

## API Endpoints voor Database Beheer

### 1. Database Statistieken Bekijken

```
GET /api/ratings/database/stats
```

**Response:**
```json
{
  "totalRatings": 150,
  "uniqueCars": 45,
  "uniqueUsers": 12,
  "oldestRating": "2025-01-15T10:30:00Z",
  "newestRating": "2025-01-20T14:22:00Z",
  "databasePath": "C:\\...\\data\\user_ratings.db"
}
```

**Gebruik:** Check hoeveel ratings er zijn voordat je reset.

### 2. Database Resetten (Volledig)

```
DELETE /api/ratings/database/reset
```

**Wat doet dit:**
- ✅ Verwijdert ALLE ratings
- ✅ Verwijdert de tabel structuur
- ✅ Maakt tabel opnieuw aan
- ⚠️ **Alleen in Development mode!**

**Wanneer gebruiken:**
- Dataset is verbeterd en CarIds zijn veranderd
- Je wilt een volledig schone start

**Response:**
```json
{
  "message": "Database succesvol gereset. Alle ratings zijn verwijderd en tabel is opnieuw aangemaakt.",
  "timestamp": "2025-01-20T15:30:00Z",
  "databasePath": "C:\\...\\data\\user_ratings.db"
}
```

### 3. Alleen Ratings Verwijderen

```
DELETE /api/ratings/database/clear
```

**Wat doet dit:**
- ✅ Verwijdert ALLE ratings
- ✅ Behoudt tabel structuur
- ⚠️ **Alleen in Development mode!**

**Wanneer gebruiken:**
- Je wilt alleen data verwijderen, niet de tabel
- Sneller dan volledige reset

**Response:**
```json
{
  "message": "Alle ratings zijn succesvol verwijderd. Tabel structuur blijft behouden.",
  "deletedCount": 150,
  "timestamp": "2025-01-20T15:30:00Z"
}
```

## Veiligheid

**Belangrijk:**
- ✅ Reset endpoints werken **ALLEEN in Development mode**
- ✅ In Production worden ze geblokkeerd (403 Forbidden)
- ✅ Alle acties worden gelogd (Warning level)

**Check Development mode:**
```json
{
  "ASPNETCORE_ENVIRONMENT": "Development"
}
```

## Workflow: Dataset Verbeteren

### Stap 1: Test Huidige Systeem
```
1. Test ratings systeem met huidige dataset
2. Voeg test ratings toe via POST /api/ratings
3. Check statistieken: GET /api/ratings/database/stats
```

### Stap 2: Verbeter Dataset
```
1. Fix merge errors in CSV
2. Update Cleaned_Car_Data_For_App_Fully_Enriched.csv
3. Test recommendations (check of CarIds kloppen)
```

### Stap 3: Reset Database
```
1. Check statistieken: GET /api/ratings/database/stats
2. Reset database: DELETE /api/ratings/database/reset
3. Verifieer: GET /api/ratings/database/stats (moet 0 ratings zijn)
```

### Stap 4: Nieuwe Ratings
```
1. Ratings worden opnieuw verzameld
2. CarIds matchen nu met nieuwe dataset
3. Collaborative filtering werkt correct
```

## Handmatige Reset (Alternatief)

Als je de API niet kunt gebruiken, kun je de database handmatig verwijderen:

**Windows PowerShell:**
```powershell
# Verwijder database bestand
Remove-Item "data\user_ratings.db" -ErrorAction SilentlyContinue

# Of via command line
del data\user_ratings.db
```

**Na verwijderen:**
- Database wordt automatisch opnieuw aangemaakt bij eerste rating
- Tabel structuur wordt automatisch gecreëerd

## Testen

### Test 1: Statistieken
```bash
curl http://localhost:5283/api/ratings/database/stats
```

### Test 2: Rating Toevoegen
```bash
curl -X POST http://localhost:5283/api/ratings \
  -H "Content-Type: application/json" \
  -d '{
    "carId": 1234,
    "rating": 5,
    "originalPrompt": "Ik wil een sportieve SUV"
  }'
```

### Test 3: Reset
```bash
curl -X DELETE http://localhost:5283/api/ratings/database/reset
```

### Test 4: Verifieer Reset
```bash
curl http://localhost:5283/api/ratings/database/stats
# Moet 0 ratings tonen
```

## Troubleshooting

### Reset werkt niet in Development
**Check:**
- `ASPNETCORE_ENVIRONMENT=Development` in appsettings.Development.json
- Of via environment variable

### Database wordt niet gevonden
**Check:**
- Database locatie in logs bij startup
- Check of `data/` directory bestaat
- Check file permissions

### Ratings blijven bestaan na reset
**Oplossing:**
- Check of reset succesvol was (check logs)
- Verifieer via GET /api/ratings/database/stats
- Probeer handmatige verwijdering

## Belangrijke Notities

1. **Backup maken:** Voor productie, maak altijd backup van database
2. **Development only:** Reset werkt alleen in Development mode
3. **Logging:** Alle reset acties worden gelogd (Warning level)
4. **Automatisch:** Database wordt automatisch aangemaakt bij eerste gebruik






