# Azure App Service Deployment Handleiding

Deze handleiding legt uit hoe je de CarRecommender.Api naar Azure App Service kunt deployen binnen je Azure for Students subscription met een F1 (Free) App Service Plan.

## Vereisten

- Azure for Students subscription (gratis)
- Visual Studio Code met Azure extensie geïnstalleerd
- .NET 8.0 SDK geïnstalleerd
- Azure CLI (optioneel, voor alternatieve deployment methoden)

## Stap 1: Lokaal Builden en Testen

Voordat je naar Azure deployt, zorg ervoor dat alles lokaal werkt:

```bash
# Navigeer naar het API project
cd CarRecommender.Api

# Restore packages
dotnet restore

# Build het project
dotnet build

# Test lokaal (optioneel)
dotnet run
```

De API zou nu moeten draaien op `http://localhost:5283`. Test de endpoints:
- `http://localhost:5283/swagger` - Swagger UI (alleen in Development)
- `http://localhost:5283/api/health` - Health check endpoint
- `http://localhost:5283/api/cars` - Lijst van auto's

## Stap 2: Voorbereiden voor Azure Deployment

### 2.1 Controleer Configuratie

Zorg ervoor dat `appsettings.json` en `appsettings.Production.json` geen harde paden bevatten:

**appsettings.json:**
```json
{
  "DataSettings": {
    "CsvFileName": "Cleaned_Car_Data_For_App_Fully_Enriched.csv",
    "DataDirectory": "data"
  }
}
```

**appsettings.Production.json:**
```json
{
  "DataSettings": {
    "CsvFileName": "Cleaned_Car_Data_For_App_Fully_Enriched.csv",
    "DataDirectory": "data"
  }
}
```

### 2.2 Zorg dat Data Bestanden Beschikbaar Zijn

Voor Azure deployment moet je de CSV data bestanden meenemen. Zorg ervoor dat:
- Het CSV bestand `Cleaned_Car_Data_For_App_Fully_Enriched.csv` in de `data/` directory staat
- Deze directory wordt meegenomen tijdens deployment

**Optioneel:** Voeg een `.csproj` aanpassing toe om data bestanden te includeren:

```xml
<ItemGroup>
  <Content Include="data\**\*">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>
</ItemGroup>
```

## Stap 3: Deployen vanuit Visual Studio Code

### 3.1 Azure Extensie Installeren

1. Open VS Code
2. Ga naar Extensions (Ctrl+Shift+X)
3. Zoek naar "Azure App Service"
4. Installeer de extensie van Microsoft

### 3.2 Inloggen op Azure

1. Klik op het Azure icoon in de sidebar (of druk Ctrl+Shift+A)
2. Klik op "Sign in to Azure..."
3. Volg de instructies om in te loggen met je Azure for Students account

### 3.3 App Service Maken (als je er nog geen hebt)

1. Klik op het "+" icoon naast "App Service" in de Azure sidebar
2. Kies je subscription (Azure for Students)
3. Kies of maak een Resource Group
4. Geef je App Service een naam (bijv. `carrecommender-api`)
5. Kies "Linux" als OS (gratis plan werkt alleen op Linux)
6. Kies ".NET 8" als runtime stack
7. Kies "F1 Free" als App Service Plan
8. Wacht tot de App Service is aangemaakt

### 3.4 Deployen naar App Service

**BELANGRIJK:** Zorg dat je het **CarRecommender.Api** project deployt, niet het CarRecommender console project!

1. **Selecteer het API project:**
   - Rechtsklik op `CarRecommender.Api` **folder** in VS Code (niet op de root folder!)
   - Of gebruik Command Palette (Ctrl+Shift+P) → "Deploy to Web App..."
   - **Zorg dat je IN de CarRecommender.Api folder bent wanneer je deployt**

2. **Kies je App Service:**
   - Selecteer je subscription
   - Selecteer je Resource Group
   - Selecteer je App Service (bijv. `app-carrecommender-dev`)

3. **Bevestig deployment:**
   - VS Code zal vragen om te bevestigen
   - Klik "Deploy" of "Yes"

4. **Wacht op deployment:**
   - VS Code toont de deployment progress in de Output window
   - Dit kan enkele minuten duren
   - **Controleer in de deployment log dat `CarRecommender.Api.dll` wordt gedeployed, niet alleen `CarRecommender.dll`**

5. **Na deployment - Controleer Startup Command:**
   - Ga naar Azure Portal → je App Service → "Configuration" → "General settings"
   - Bij "Startup Command" moet staan: `dotnet CarRecommender.Api.dll`
   - Als dit leeg is of verkeerd, voeg het toe en klik "Save"
   - Herstart de App Service na het aanpassen

## Stap 4: Data Bestanden Uploaden

Na deployment moet je de CSV data bestanden uploaden naar Azure:

### Optie A: Via Azure Portal

1. Ga naar [Azure Portal](https://portal.azure.com)
2. Zoek je App Service
3. Ga naar "Advanced Tools" → "Go" (opent Kudu)
4. Ga naar "Debug console" → "CMD"
5. Navigeer naar `site/wwwroot`
6. Upload de `data` folder met het CSV bestand

### Optie B: Via VS Code Azure Extensie

1. In VS Code, rechtsklik op je App Service
2. Kies "Deploy to Web App..." → "Browse..."
3. Upload de `data` folder

### Optie C: Via FTP

1. In Azure Portal, ga naar je App Service → "Deployment Center" → "FTP"
2. Download FTP credentials
3. Gebruik een FTP client om de `data` folder te uploaden naar `site/wwwroot/data`

## Stap 5: Verificatie na Deployment

### 5.1 Vind je API URL

Na deployment krijg je een URL zoals:
- `https://carrecommender-api.azurewebsites.net`

### 5.2 Test de Health Endpoint

Open in je browser:
```
https://<jouw-app-name>.azurewebsites.net/api/health
```

Je zou moeten zien:
```json
{
  "status": "OK"
}
```

### 5.3 Test andere Endpoints

**Belangrijk:** Swagger UI is alleen beschikbaar in Development mode, dus werkt niet op Azure Production.

Test de endpoints direct:
- `https://<jouw-app-name>.azurewebsites.net/api/cars`
- `https://<jouw-app-name>.azurewebsites.net/api/cars/1`
- `https://<jouw-app-name>.azurewebsites.net/api/recommendations/1?top=5`

### 5.4 Test POST Endpoint (met curl of Postman)

```bash
curl -X POST "https://<jouw-app-name>.azurewebsites.net/api/recommendations/text" \
  -H "Content-Type: application/json" \
  -d '{"text": "Ik zou liever een automaat hebben met veel vermogen, max 25k euro", "top": 5}'
```

## Stap 6: Troubleshooting

### Probleem: Health endpoint geeft 404

**Oplossing:** Controleer of de deployment succesvol was. Herstart de App Service in Azure Portal.

### Probleem: API geeft lege lijst terug

**Oplossing:** 
1. Controleer of het CSV bestand is geüpload naar `site/wwwroot/data/`
2. Controleer de logs in Azure Portal → App Service → "Log stream"
3. Controleer of de bestandsnaam overeenkomt met `appsettings.Production.json`

### Probleem: Deployment faalt

**Oplossing:**
1. Controleer of je .NET 8.0 SDK hebt geïnstalleerd
2. Controleer of alle packages correct zijn gerestored
3. Probeer eerst lokaal te builden: `dotnet build -c Release`

### Probleem: Swagger werkt niet op Azure

**Oplossing:** Dit is normaal! Swagger is alleen ingeschakeld in Development mode voor veiligheid. Gebruik de endpoints direct of gebruik Postman/curl.

## Stap 7: Monitoring en Logs

### Logs Bekijken

1. Ga naar Azure Portal → je App Service
2. Klik op "Log stream" om real-time logs te zien
3. Of klik op "Logs" voor historische logs

### Application Insights (Optioneel)

Voor uitgebreidere monitoring:
1. Ga naar je App Service → "Application Insights"
2. Klik "Turn on Application Insights"
3. Volg de wizard

## Belangrijke Notities

### F1 Free Plan Limitaties

- **CPU:** Gedeeld, kan traag zijn tijdens piekuren
- **Memory:** 1 GB RAM
- **Storage:** 1 GB
- **Schaalbaarheid:** Geen auto-scaling
- **SLA:** Geen SLA garantie

### Kosten

- **F1 Plan:** Gratis (binnen Azure for Students)
- **Data Transfer:** Eerste 5 GB/maand gratis
- **Storage:** Binnen de gratis tier

### Best Practices

1. **Gebruik Health Endpoint:** Azure gebruikt dit voor health checks
2. **Monitor Logs:** Houd de logs in de gaten voor errors
3. **Test Lokaal Eerst:** Test altijd lokaal voordat je deployt
4. **Backup Data:** Zorg voor backups van je CSV data

## Alternatieve Deployment Methoden

### Via Azure CLI

```bash
# Login
az login

# Build
dotnet publish -c Release -o ./publish

# Deploy
az webapp deploy --name <app-name> --resource-group <resource-group> --src-path ./publish
```

### Via GitHub Actions (CI/CD)

Zie Azure Portal → App Service → "Deployment Center" voor GitHub Actions setup.

## Hulp Nodig?

- Azure Documentation: https://docs.microsoft.com/azure/app-service
- .NET Documentation: https://docs.microsoft.com/dotnet
- Azure Support: Via Azure Portal → "Help + Support"

---

**Laatste update:** December 2025
**Versie:** 1.0

