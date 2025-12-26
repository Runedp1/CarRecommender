# Azure Deployment Troubleshooting Guide

## Probleem: Kudu Deployment Log Error (500/404)

### Symptomen
- GitHub Actions build slaagt
- Deployment start maar faalt met: `Could not find a part of the path 'C:\home\site\deployments\temp-XXX\log.log'`
- Kudu kan deployment logs niet vinden

### Mogelijke Oorzaken

1. **ZIP structuur probleem**: De ZIP file bevat een subfolder in plaats van bestanden direct in root
2. **Azure webapps-deploy action probleem**: De action heeft problemen met de ZIP upload
3. **Kudu deployment directory niet aangemaakt**: Deployment faalt voordat logs kunnen worden geschreven

## Oplossingen

### Oplossing 1: Verbeterde ZIP Creatie (Huidige Aanpak)

De workflow maakt nu de ZIP correct:
- Gaat eerst naar publish directory
- Maakt ZIP met bestanden direct in root
- Voegt startup-command toe

### Oplossing 2: Handmatige Deployment via Kudu (Als Automatisch Faalt)

Als de automatische deployment blijft falen, kun je handmatig deployen:

#### Stap 1: Download de Artifact van GitHub Actions

1. Ga naar GitHub → Actions → Je laatste workflow run
2. Klik op "build" job
3. Scroll naar "Upload artifact for deployment job"
4. Klik op de artifact download link
5. Pak de ZIP uit

#### Stap 2: Maak een Nieuwe ZIP met Correcte Structuur

```powershell
# Navigeer naar de uitgepakte publish folder
cd <uitgepakte-artifact-folder>

# Maak een nieuwe ZIP met alle bestanden direct in root
Compress-Archive -Path * -DestinationPath deploy.zip -Force
```

**BELANGRIJK**: Zorg dat de ZIP de volgende structuur heeft:
```
deploy.zip
├── CarRecommender.Api.dll (of CarRecommender.Web.dll)
├── web.config
├── appsettings.json
├── data/
├── images/
└── ... (alle andere bestanden)
```

**NIET**:
```
deploy.zip
└── publish/
    ├── CarRecommender.Api.dll
    └── ...
```

#### Stap 3: Upload via Kudu

**Optie A: Via Kudu Console**
1. Ga naar: `https://app-carrecommender-dev.scm.azurewebsites.net`
2. Klik op "Debug console" → "CMD"
3. Navigeer naar: `site/wwwroot`
4. Sleep je `deploy.zip` naar het browser venster
5. Unzip: `unzip deploy.zip -d .`
6. Verwijder de ZIP: `del deploy.zip`

**Optie B: Via Azure Portal ZIP Deploy**
1. Ga naar [Azure Portal](https://portal.azure.com)
2. Zoek je App Service: `app-carrecommender-dev`
3. Ga naar "Deployment Center"
4. Klik op "ZIP Deploy" (of "Manual deploy")
5. Upload je `deploy.zip`
6. Wacht tot deployment klaar is

**Optie C: Via Kudu API (PowerShell)**
```powershell
# Login eerst
az login

# Deploy ZIP
$zipPath = "C:\path\to\deploy.zip"
$appName = "app-carrecommender-dev"
$resourceGroup = "your-resource-group"

az webapp deployment source config-zip `
  --resource-group $resourceGroup `
  --name $appName `
  --src $zipPath
```

### Oplossing 3: Check Azure App Service Logs

Als deployment faalt, check de logs:

1. **Via Azure Portal**:
   - App Service → "Log stream"
   - Bekijk real-time logs

2. **Via Kudu**:
   - `https://app-carrecommender-dev.scm.azurewebsites.net`
   - "Debug console" → "CMD"
   - Navigeer naar: `LogFiles\Application`
   - Bekijk recente log files

3. **Via Azure CLI**:
   ```bash
   az webapp log tail --name app-carrecommender-dev --resource-group your-resource-group
   ```

### Oplossing 4: Verifieer App Service Configuratie

1. **Startup Command**:
   - Azure Portal → App Service → Configuration → General settings
   - Startup Command: `dotnet CarRecommender.Api.dll` (of leeg laten als web.config het afhandelt)

2. **.NET Version**:
   - Configuration → General settings
   - Stack: `.NET`
   - Version: `9.0` (of nieuwste beschikbare)

3. **Platform**:
   - Configuration → General settings
   - Platform: `64 Bit` (aanbevolen)

4. **Always On**:
   - Configuration → General settings
   - Always On: `On` (voor free tier)

### Oplossing 5: Test Lokale Publish Output

Voordat je deployt, test lokaal:

```powershell
# Build en publish
cd backend/CarRecommender.Api
dotnet publish -c Release -o ./publish

# Verifieer output
dir ./publish
# Controleer dat CarRecommender.Api.dll bestaat
# Controleer dat web.config bestaat
# Controleer dat data folder bestaat

# Test lokaal (optioneel)
cd ./publish
dotnet CarRecommender.Api.dll
# Stop met Ctrl+C
```

## Debug Checklist

Als deployment faalt, check:

- [ ] GitHub Actions build job slaagt
- [ ] DLL bestaat in publish output (check verify step)
- [ ] ZIP file wordt gemaakt (check "Create deployment package" step)
- [ ] ZIP file size > 0 (check logs)
- [ ] Azure login slaagt (check deploy job)
- [ ] App Service bestaat en is actief
- [ ] App Service heeft correcte .NET version
- [ ] App Service heeft correcte startup command (of web.config)
- [ ] Data files worden meegenomen in publish (check csproj)

## Alternatieve Deployment Methoden

### Methode 1: GitHub Actions (Huidig)
- ✅ Automatisch
- ✅ Geïntegreerd met GitHub
- ❌ Kan problemen hebben met ZIP structuur

### Methode 2: Azure CLI ZIP Deploy
```bash
az webapp deployment source config-zip \
  --resource-group <resource-group> \
  --name <app-name> \
  --src <path-to-zip>
```

### Methode 3: VS Code Azure Extension
- Installeer "Azure App Service" extension
- Rechtsklik op project → "Deploy to Web App"

### Methode 4: Visual Studio Publish
- Open solution in Visual Studio
- Rechtsklik op project → "Publish"
- Selecteer Azure App Service

## Veelvoorkomende Fouten

### Fout: "Could not find a part of the path"
**Oorzaak**: Deployment faalt voordat logs kunnen worden geschreven
**Oplossing**: Check GitHub Actions logs, verifieer ZIP structuur

### Fout: "Application Error"
**Oorzaak**: DLL niet gevonden of verkeerde startup command
**Oplossing**: Check startup command, verifieer DLL naam in web.config

### Fout: "Data file not found"
**Oorzaak**: CSV bestand niet meegenomen in deployment
**Oplossing**: Check csproj → Content Include voor data files

### Fout: "500 Internal Server Error"
**Oorzaak**: Application crash bij startup
**Oplossing**: Check Application logs, verifieer dependencies

## Contact & Support

Als niets werkt:
1. Check [Azure App Service Troubleshooting](https://docs.microsoft.com/en-us/azure/app-service/troubleshoot-diagnostic-logs)
2. Check [Kudu Documentation](https://github.com/projectkudu/kudu/wiki)
3. Check GitHub Actions logs voor meer details





