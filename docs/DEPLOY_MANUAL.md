# Handmatige Deployment naar Azure App Service

Als de VS Code Azure extensie niet werkt, kun je handmatig deployen via ZIP deploy.

## Stap 1: Build het project voor Release

```bash
cd CarRecommender.Api
dotnet publish -c Release -o ./publish
```

Dit maakt een `publish` folder met alle benodigde bestanden.

## Stap 2: Maak een ZIP bestand

1. Ga naar `CarRecommender.Api/bin/Release/net8.0/publish`
2. Selecteer alle bestanden en folders
3. Maak een ZIP bestand (bijv. `CarRecommender.Api.zip`)

## Stap 3: Deploy via Azure Portal

1. Ga naar [Azure Portal](https://portal.azure.com)
2. Zoek je App Service: `app-carrecommender-dev`
3. Ga naar "Deployment Center"
4. Kies "Local Git" of "ZIP Deploy"
5. Upload je ZIP bestand
6. Wacht tot deployment klaar is

## Stap 4: Controleer Startup Command

1. Ga naar "Configuration" → "General settings"
2. Bij "Startup Command" voeg toe: `dotnet CarRecommender.Api.dll`
3. Klik "Save"
4. Herstart de App Service

## Stap 5: Test

Test de health endpoint:
```
https://app-carrecommender-dev.azurewebsites.net/api/health
```




