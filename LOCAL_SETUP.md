# Lokaal Opzetten - Backend en Frontend

## Vereisten

- **.NET 8.0 SDK** - [Download hier](https://dotnet.microsoft.com/download/dotnet/8.0)
- **PowerShell** (standaard op Windows)

## Snel Starten

### Optie 1: PowerShell Script (Aanbevolen)

Open PowerShell in de project root directory en voer uit:

```powershell
.\start-local.ps1
```

Dit script start automatisch beide projecten in aparte terminal vensters.

### Optie 2: Handmatig Starten

#### Terminal 1 - Backend API

```powershell
cd backend\CarRecommender.Api
dotnet run
```

Backend draait op: **http://localhost:5283**
- Swagger UI: http://localhost:5283/swagger

#### Terminal 2 - Frontend Web

```powershell
cd frontend\CarRecommender.Web
dotnet run
```

Frontend draait op: **http://localhost:7000**

## Configuratie

### Backend Configuratie
- Poort: `5283` (HTTP) of `7086` (HTTPS)
- Configuratie: `backend/CarRecommender.Api/appsettings.Development.json`
- Data directory: `backend/data/`

### Frontend Configuratie
- Poort: `7000` (HTTP) of `7001` (HTTPS)
- Configuratie: `frontend/CarRecommender.Web/appsettings.Development.json`
- API URL: `http://localhost:5283` (automatisch geconfigureerd)

## Troubleshooting

### Poort al in gebruik
Als poort 5283 of 7000 al in gebruik is:
1. Stop andere applicaties die deze poorten gebruiken
2. Of wijzig de poorten in `launchSettings.json`

### Backend kan data niet vinden
Zorg dat `backend/data/df_master_v8_def.csv` bestaat. De applicatie zoekt automatisch naar dit bestand.

### Frontend kan niet verbinden met backend
1. Controleer dat backend draait op poort 5283
2. Controleer `frontend/CarRecommender.Web/appsettings.Development.json`:
   ```json
   {
     "ApiSettings": {
       "BaseUrl": "http://localhost:5283"
     }
   }
   ```

### .NET SDK niet gevonden
Installeer .NET 8.0 SDK en controleer met:
```powershell
dotnet --version
```

## Stoppen

Druk op `Ctrl+C` in beide terminal vensters om de applicaties te stoppen.


