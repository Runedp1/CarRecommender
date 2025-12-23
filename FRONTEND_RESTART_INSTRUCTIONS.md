# Frontend Herstarten - Oplossing voor Wijzigingen Niet Zichtbaar

## Probleem
De wijzigingen zijn niet zichtbaar omdat de frontend nog draait met oude code.

## Oplossing: Frontend Herstarten

### Stap 1: Stop de Frontend
1. Zoek de terminal/console waar de frontend draait
2. Druk op **Ctrl+C** om de frontend te stoppen
3. Of stop het proces via Task Manager:
   - Open Task Manager (Ctrl+Shift+Esc)
   - Zoek "CarRecommender.Web" (proces ID 8400)
   - Klik op "Eind taak"

### Stap 2: Herstart de Frontend

**Optie A: Via Visual Studio**
1. Stop de applicatie (Shift+F5 of stop knop)
2. Start opnieuw (F5)

**Optie B: Via Terminal**
```powershell
cd frontend\CarRecommender.Web
dotnet run
```

### Stap 3: Browser Cache Leegmaken

**Hard Refresh in Browser:**
- **Chrome/Edge**: Ctrl+Shift+R of Ctrl+F5
- **Firefox**: Ctrl+Shift+R of Ctrl+F5
- **Safari**: Cmd+Shift+R

**Of:**
1. Open Developer Tools (F12)
2. Rechtsklik op de refresh knop
3. Selecteer "Empty Cache and Hard Reload"

### Stap 4: Verifieer Wijzigingen

Na herstarten zou je moeten zien:
- ✅ Transmissie wordt **ALTIJD** getoond (ook als data ontbreekt)
- ✅ Transmissie waarden zijn genormaliseerd: "Automaat", "Handmatig", of "Niet opgegeven"
- ✅ Merk voorkeur werkt bij zoekopdrachten

## Testen

1. Ga naar `http://localhost:7000`
2. Voer in: `"Ik zoek een BMW met maximaal 30.000 euro"`
3. Check of alleen BMW auto's worden getoond
4. Check of transmissie wordt getoond op alle auto cards

## Als het nog steeds niet werkt

1. **Controleer of backend ook herstart is:**
   - Backend moet draaien op `http://localhost:5283`
   - Test via Swagger: `http://localhost:5283/swagger`

2. **Controleer appsettings.Development.json:**
   - Moet `"BaseUrl": "http://localhost:5283"` bevatten

3. **Bouw de frontend opnieuw:**
   ```powershell
   cd frontend\CarRecommender.Web
   dotnet clean
   dotnet build
   dotnet run
   ```

4. **Check browser console voor errors:**
   - Open Developer Tools (F12)
   - Bekijk Console tab voor JavaScript errors
   - Bekijk Network tab voor API call errors

