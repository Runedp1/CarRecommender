# Backend API Starten - Oplossing voor API Verbindingsprobleem

## Probleem
De frontend kan niet verbinden met de API omdat de backend niet draait.

## Oplossing: Backend API Starten

### Stap 1: Open een Nieuwe Terminal

Open een **nieuwe PowerShell terminal** (houd de frontend terminal open).

### Stap 2: Navigeer naar Backend Directory

```powershell
cd "C:\Users\runed\OneDrive - Thomas More\Recommendation_System_New\backend\CarRecommender.Api"
```

### Stap 3: Start de Backend API

```powershell
dotnet run
```

**Verwachte output:**
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5283
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

### Stap 4: Verifieer dat API Draait

Open in browser: `http://localhost:5283/swagger`

Je zou de Swagger UI moeten zien met alle API endpoints.

### Stap 5: Test de Frontend Opnieuw

1. Ga terug naar de frontend: `http://localhost:7000`
2. Hard refresh: **Ctrl+Shift+R**
3. Probeer een zoekopdracht: `"ik wil een audi, manueel, max 25k"`

## Beide Applicaties Moeten Draaien

Voor de applicatie te laten werken heb je **2 terminals** nodig:

**Terminal 1 - Backend API:**
```powershell
cd backend\CarRecommender.Api
dotnet run
```
- Draait op: `http://localhost:5283`

**Terminal 2 - Frontend:**
```powershell
cd frontend\CarRecommender.Web
dotnet run
```
- Draait op: `http://localhost:7000`

## Troubleshooting

### Probleem: Poort 5283 is al in gebruik
**Oplossing:**
- Stop andere applicaties die poort 5283 gebruiken
- Of wijzig de poort in `launchSettings.json`

### Probleem: CSV bestand niet gevonden
**Oplossing:**
- Controleer of `backend/CarRecommender.Api/data/Cleaned_Car_Data_For_App_Fully_Enriched.csv` bestaat
- Check console output voor exacte pad

### Probleem: CORS errors in browser console
**Oplossing:**
- CORS is nu geconfigureerd in `Program.cs`
- Herstart de backend na wijzigingen

## Snelle Test

Test de API direct:
```powershell
Invoke-RestMethod -Uri "http://localhost:5283/api/health" -Method Get
```

Verwacht: `{ "status": "OK" }`





