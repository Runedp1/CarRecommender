# Testgids - Nieuwe Functionaliteit

Deze gids helpt je om de nieuwe functionaliteit te testen:
- **Voorkeurmerk** (gebruiker kan een merk voorkeur opgeven)
- **Transmissie weergave** (altijd zichtbaar, genormaliseerd naar Nederlands)

## Vereisten

- .NET 9.0 SDK geïnstalleerd
- Visual Studio of VS Code (of terminal)
- CSV data bestand beschikbaar in `backend/CarRecommender.Api/data/`

## Stap 1: Project Bouwen

### Optie A: Via Visual Studio
1. Open `CarRecommender.sln` in Visual Studio
2. Klik rechts op de solution → **Rebuild Solution**
3. Controleer of er geen build errors zijn

### Optie B: Via Terminal
```powershell
# Navigeer naar project root
cd "C:\Users\runed\OneDrive - Thomas More\Recommendation_System_New"

# Bouw de solution
dotnet build CarRecommender.sln
```

## Stap 2: Backend API Starten

### Via Visual Studio:
1. Rechtsklik op `backend/CarRecommender.Api` project
2. Selecteer **Set as Startup Project**
3. Druk op **F5** of klik op **Run**

### Via Terminal:
```powershell
cd backend\CarRecommender.Api
dotnet run
```

**Verwachte output:**
- API draait op `http://localhost:5283`
- Swagger UI beschikbaar op `http://localhost:5283/swagger`
- CSV data wordt geladen bij opstarten

**Test de API:**
- Open browser naar `http://localhost:5283/swagger`
- Test endpoint: `POST /api/recommendations/text`
- Body: `{ "text": "Ik zoek een BMW met maximaal 30.000 euro", "top": 5 }`

## Stap 3: Frontend Configureren (voor lokale testing)

**Belangrijk:** De frontend moet naar de lokale API verwijzen, niet naar Azure.

### Update `frontend/CarRecommender.Web/appsettings.Development.json`:

```json
{
  "DetailedErrors": true,
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ApiSettings": {
    "BaseUrl": "http://localhost:5283"
  }
}
```

## Stap 4: Frontend Starten

### Via Visual Studio:
1. Rechtsklik op `frontend/CarRecommender.Web` project
2. Selecteer **Set as Startup Project**
3. Druk op **F5** of klik op **Run**

### Via Terminal (in nieuwe terminal):
```powershell
cd frontend\CarRecommender.Web
dotnet run
```

**Verwachte output:**
- Frontend draait op `http://localhost:7000`
- Browser opent automatisch

## Stap 5: Functionaliteit Testen

### Test 1: Merk Voorkeur

1. Ga naar de homepage (`http://localhost:7000`)
2. Voer de volgende zoekopdrachten in:

**Test cases:**
- `"Ik zoek een BMW met maximaal 30.000 euro budget"`
  - **Verwacht:** Alleen BMW auto's worden getoond
  - **Check:** Uitleg bevat "is van het merk BMW"

- `"Ik wil absoluut een Mercedes-Benz, max 25k"`
  - **Verwacht:** Alleen Mercedes-Benz auto's
  - **Check:** Uitleg bevat "is van het merk Mercedes-Benz"

- `"Een Audi zou leuk zijn, max 20.000 euro"`
  - **Verwacht:** Voornamelijk Audi auto's (maar kan ook andere tonen als geen match)
  - **Check:** Uitleg vermeldt merk voorkeur

- `"Geen Opel, liever een Volkswagen"`
  - **Verwacht:** Geen Opel auto's, voorkeur voor Volkswagen

**Ondersteunde merken:**
BMW, Audi, Mercedes-Benz, Volkswagen (VW), Ford, Opel, Peugeot, Citroën, Renault, Toyota, Honda, Nissan, Mazda, Volvo, Skoda, Seat, Fiat, Alfa Romeo, Jaguar, Land Rover, Mini, Porsche, Tesla, Hyundai, Kia, Lexus, Dacia, Suzuki, Mitsubishi, Subaru

### Test 2: Transmissie Weergave

1. Ga naar de homepage of `/Cars` pagina
2. Bekijk de auto cards

**Verwachte resultaten:**
- **Transmissie wordt ALTIJD getoond** (ook als data ontbreekt)
- Genormaliseerde waarden:
  - `"Automaat"` voor: automatic, automaat, automatisch, CVT, DCT
  - `"Handmatig"` voor: manual, handmatig, schakel, handbak
  - `"Niet opgegeven"` als er geen data is

**Check:**
- Elke auto card heeft een "Transmissie:" regel
- Waarden zijn in het Nederlands
- Geen lege regels waar transmissie zou moeten staan

### Test 3: Combinatie Test

Test met complexe voorkeuren:

```
"Ik zoek een BMW met automaat, maximaal 35.000 euro, benzine, voor dagelijks gebruik"
```

**Verwacht:**
- Alleen BMW auto's
- Alleen automaten
- Binnen budget
- Benzine motor
- Transmissie wordt correct getoond als "Automaat"

## Stap 6: API Direct Testen (Optioneel)

Je kunt de API ook direct testen via Swagger of curl:

### Via Swagger UI:
1. Ga naar `http://localhost:5283/swagger`
2. Klik op `POST /api/recommendations/text`
3. Klik op "Try it out"
4. Voer in:
```json
{
  "text": "Ik zoek een BMW met automaat, max 30k",
  "top": 5
}
```
5. Klik op "Execute"
6. Bekijk de response - controleer of `PreferredBrand` is ingesteld

### Via curl (PowerShell):
```powershell
$body = @{
    text = "Ik zoek een BMW met automaat, max 30k"
    top = 5
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5283/api/recommendations/text" -Method Post -Body $body -ContentType "application/json"
```

## Troubleshooting

### Probleem: Frontend kan niet verbinden met API
**Oplossing:** 
- Controleer of backend API draait op `http://localhost:5283`
- Controleer `appsettings.Development.json` in frontend
- Zorg dat beide projecten draaien

### Probleem: Geen auto's gevonden
**Oplossing:**
- Controleer of CSV bestand bestaat in `backend/CarRecommender.Api/data/`
- Check console output van backend voor foutmeldingen
- Controleer of CSV correct wordt geladen

### Probleem: Transmissie wordt niet getoond
**Oplossing:**
- Hard refresh browser (Ctrl+F5)
- Controleer browser console voor JavaScript errors
- Controleer of frontend correct is gebouwd

### Probleem: Merk wordt niet herkend
**Oplossing:**
- Controleer spelling (case-insensitive, maar probeer exacte merknaam)
- Bekijk backend logs om te zien welke preferences worden geparsed
- Test met verschillende formuleringen

## Debug Tips

### Backend Logs Bekijken:
- Console output toont welke preferences worden geparsed
- Swagger UI toont API responses
- Check `UserPreferences` object in response

### Frontend Debuggen:
- Open browser Developer Tools (F12)
- Bekijk Network tab voor API calls
- Check Console voor errors
- Inspecteer HTML om te zien of transmissie wordt gerenderd

## Snelle Test Checklist

- [ ] Backend API start zonder errors
- [ ] Frontend start zonder errors
- [ ] Homepage laadt correct
- [ ] Zoekopdracht met merk voorkeur werkt
- [ ] Alleen auto's van gekozen merk worden getoond
- [ ] Transmissie wordt altijd getoond op alle auto cards
- [ ] Transmissie waarden zijn genormaliseerd (Automaat/Handmatig)
- [ ] Uitleg bevat merk informatie wanneer van toepassing
- [ ] `/Cars` pagina toont ook transmissie correct

## Volgende Stappen

Na succesvol testen:
1. Test met verschillende combinaties van voorkeuren
2. Test edge cases (geen merk voorkeur, meerdere merken genoemd, etc.)
3. Test met echte gebruikersscenario's
4. Overweeg om unit tests toe te voegen voor `TextParserService.ExtractBrandPreference`






