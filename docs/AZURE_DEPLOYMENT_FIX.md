# Azure Deployment Fix - Analyse en Oplossing

## 🔴 Probleem Analyse

### Foutmelding
```
HTTP Error 404.0 - Not Found
The resource you are looking for has been removed, had its name changed, or is temporarily unavailable.
Requested URL: https://_1app-carrecommender-dev:80/api/deployments/temp-5af63a77/log
```

### Oorzaak
De deployment faalt omdat de **GitHub Actions workflows** het verkeerde project publiceren en de verkeerde bestanden uploaden naar Azure.

---

## 🐛 Geïdentificeerde Problemen

### Probleem 1: Verkeerd Project wordt Gepubliceerd ❌
**Locatie**: Beide workflows (API en Web)

**Foutieve code**:
```yaml
- name: dotnet publish
  run: dotnet publish -c Release -o "${{env.DOTNET_ROOT}}/myapp"
```

**Probleem**: 
- Publiceert GEEN specifiek project
- Publiceert waarschijnlijk de hele solution of root project
- De juiste DLL (`CarRecommender.Api.dll` of `CarRecommender.Web.dll`) wordt niet gegenereerd

**Impact**: 
- Azure kan de applicatie niet starten
- `web.config` verwacht `CarRecommender.Api.dll` maar die bestaat niet in de upload

### Probleem 2: Verkeerde Package Path in Deployment ❌
**Locatie**: Beide workflows, deployment step

**Foutieve code**:
```yaml
- name: Download artifact from build job
  uses: actions/download-artifact@v4
  with:
    name: .net-app
    # GEEN path gespecificeerd!

- name: Deploy to Azure Web App
  uses: azure/webapps-deploy@v3
  with:
    package: .  # Verwijst naar huidige directory, niet naar artifact
```

**Probleem**:
- Artifact wordt gedownload maar path is onduidelijk
- Package path verwijst naar verkeerde directory
- Verkeerde bestanden worden geüpload naar Azure

### Probleem 3: Build Step Publiceert Verkeerd Project ❌
**Locatie**: Beide workflows

**Foutieve code**:
```yaml
- name: Build with dotnet
  run: dotnet build --configuration Release
```

**Probleem**:
- Bouwt de hele solution, niet het specifieke project
- Onnodig langzaam
- Kan dependencies problemen veroorzaken

---

## ✅ Oplossingen (Toegepast)

### Fix 1: Specificeer Correct Project in Publish
**Voor API workflow** (`.github/workflows/main_app-carrecommender-dev.yml`):
```yaml
- name: Build with dotnet
  run: dotnet build backend/CarRecommender.Api/CarRecommender.Api.csproj --configuration Release

- name: dotnet publish
  run: dotnet publish backend/CarRecommender.Api/CarRecommender.Api.csproj -c Release -o ./publish
```

**Voor Web workflow** (`.github/workflows/main_pp-carrecommender-web-dev.yml`):
```yaml
- name: Build with dotnet
  run: dotnet build frontend/CarRecommender.Web/CarRecommender.Web.csproj --configuration Release

- name: dotnet publish
  run: dotnet publish frontend/CarRecommender.Web/CarRecommender.Web.csproj -c Release -o ./publish
```

### Fix 2: Specificeer Correct Artifact Path + ZIP Package
**Voor beide workflows**:
```yaml
- name: Upload artifact for deployment job
  uses: actions/upload-artifact@v4
  with:
    name: .net-app
    path: ./publish  # Expliciet path naar publish directory

- name: Download artifact from build job
  uses: actions/download-artifact@v4
  with:
    name: .net-app
    path: ./publish  # Download naar expliciete directory

- name: Verify publish output
  run: |
    # Controleer dat DLL bestaat
    if not exist "./publish/CarRecommender.Api.dll" (
      echo "ERROR: DLL not found!"
      exit 1
    )

- name: Create deployment package
  run: |
    # Maak ZIP file voor deployment (Kudu werkt beter met ZIP)
    powershell Compress-Archive -Path ./publish/* -DestinationPath ./deploy.zip -Force

- name: Deploy to Azure Web App
  uses: azure/webapps-deploy@v3
  with:
    package: ./deploy.zip  # Gebruik ZIP file in plaats van directory
```

---

## 📋 Deployment Checklist

Gebruik deze checklist **elke keer** voordat je deployt:

### Pre-Deployment Checklist

#### 1. ✅ Project Structuur Controleren
- [ ] `backend/CarRecommender.Api/` bestaat en bevat `CarRecommender.Api.csproj`
- [ ] `frontend/CarRecommender.Web/` bestaat en bevat `CarRecommender.Web.csproj`
- [ ] `backend/data/Cleaned_Car_Data_For_App_Fully_Enriched.csv` bestaat
- [ ] `backend/images/` directory bestaat (mag leeg zijn)

#### 2. ✅ Lokale Build Test
```bash
# Test API build
cd backend/CarRecommender.Api
dotnet build -c Release
dotnet publish -c Release -o ./publish-test
# Controleer of CarRecommender.Api.dll bestaat in ./publish-test/

# Test Web build
cd ../../frontend/CarRecommender.Web
dotnet build -c Release
dotnet publish -c Release -o ./publish-test
# Controleer of CarRecommender.Web.dll bestaat in ./publish-test/
```

#### 3. ✅ GitHub Actions Workflows Controleren
- [ ] **API workflow** (`.github/workflows/main_app-carrecommender-dev.yml`):
  - [ ] Build step: `dotnet build backend/CarRecommender.Api/CarRecommender.Api.csproj`
  - [ ] Publish step: `dotnet publish backend/CarRecommender.Api/CarRecommender.Api.csproj -c Release -o ./publish`
  - [ ] Artifact path: `path: ./publish`
  - [ ] Package path: `package: ./publish`
  - [ ] App name: `app-carrecommender-dev`

- [ ] **Web workflow** (`.github/workflows/main_pp-carrecommender-web-dev.yml`):
  - [ ] Build step: `dotnet build frontend/CarRecommender.Web/CarRecommender.Web.csproj`
  - [ ] Publish step: `dotnet publish frontend/CarRecommender.Web/CarRecommender.Web.csproj -c Release -o ./publish`
  - [ ] Artifact path: `path: ./publish`
  - [ ] Package path: `package: ./publish`
  - [ ] App name: `pp-carrecommender-web-dev`

#### 4. ✅ Azure App Service Configuratie
- [ ] **API App Service** (`app-carrecommender-dev`):
  - [ ] Startup Command: `dotnet CarRecommender.Api.dll` (of leeg laten, web.config handelt dit af)
  - [ ] .NET Version: 9.0
  - [ ] Platform: Windows
  - [ ] Always On: Enabled (voor free tier)

- [ ] **Web App Service** (`pp-carrecommender-web-dev`):
  - [ ] Startup Command: `dotnet CarRecommender.Web.dll` (of leeg laten)
  - [ ] .NET Version: 9.0
  - [ ] Platform: Windows
  - [ ] Always On: Enabled

#### 5. ✅ Configuratie Bestanden
- [ ] **API** (`backend/CarRecommender.Api/appsettings.json`):
  - [ ] `DataSettings:CsvFileName`: `Cleaned_Car_Data_For_App_Fully_Enriched.csv`
  - [ ] `DataSettings:DataDirectory`: `data`

- [ ] **Web** (`frontend/CarRecommender.Web/appsettings.json`):
  - [ ] `ApiSettings:BaseUrl`: Correcte Azure API URL
  - [ ] Geen localhost referenties

#### 6. ✅ web.config Controleren
- [ ] **API** (`backend/CarRecommender.Api/web.config`):
  - [ ] `arguments=".\CarRecommender.Api.dll"` (correct DLL naam)
  - [ ] `processPath="dotnet"`

### Deployment Stappen

#### Stap 1: Commit en Push
```bash
git add .
git commit -m "Fix deployment workflows"
git push origin main
```

#### Stap 2: Monitor GitHub Actions
1. Ga naar GitHub repository
2. Klik op "Actions" tab
3. Selecteer de running workflow
4. Controleer build job:
   - [ ] Build stap slaagt
   - [ ] Publish stap slaagt
   - [ ] Artifact wordt geüpload

5. Controleer deploy job:
   - [ ] Artifact wordt gedownload
   - [ ] DLL verificatie slaagt
   - [ ] ZIP package wordt gemaakt
   - [ ] Azure login slaagt
   - [ ] Deployment slaagt (geen 404/500 errors)

#### Stap 3: Verifieer Deployment
**API Health Check**:
```bash
curl https://app-carrecommender-dev.azurewebsites.net/api/health
# Verwacht: {"status":"OK"}
```

**Web Check**:
```bash
curl https://pp-carrecommender-web-dev.azurewebsites.net/
# Verwacht: HTML response (200 OK)
```

### Post-Deployment Checklist

- [ ] API health endpoint werkt: `/api/health`
- [ ] API cars endpoint werkt: `/api/cars?page=1&pageSize=5`
- [ ] API recommendations endpoint werkt: `/api/recommendations/1?top=5`
- [ ] Web homepage laadt correct
- [ ] Web kan API aanroepen (check browser console voor errors)
- [ ] Data wordt correct geladen (check API responses)

---

## 🔧 Troubleshooting

### Fout: 404/500 bij Deployment Logs (Kudu Error)
**Oorzaak**: 
- Deployment faalt voordat logs kunnen worden geschreven
- Kudu kan de deployment log file niet vinden: `Could not find a part of the path 'C:\home\site\deployments\temp-XXX\log.log'`
- Dit gebeurt vaak wanneer de zip deploy faalt of de package structuur incorrect is

**Oplossing**: 
1. ✅ **ZIP Package Toegevoegd**: Workflows maken nu expliciet een ZIP file voor deployment
2. Check GitHub Actions logs voor build errors
3. Verifieer dat correct project wordt gepubliceerd (debug stappen toegevoegd)
4. Check artifact upload/download
5. Verifieer dat DLL bestaat in publish output (automatische check toegevoegd)

### Fout: Application Error na Deployment
**Oorzaak**: Verkeerde DLL of missing dependencies
**Oplossing**:
1. Check Azure App Service logs: `https://portal.azure.com` → App Service → Log stream
2. Verifieer `web.config` DLL naam
3. Check of alle dependencies in publish folder zitten

### Fout: Data File Not Found
**Oorzaak**: CSV bestand niet meegenomen in deployment
**Oplossing**:
1. Check `CarRecommender.Api.csproj` → `Content Include` voor data files
2. Verifieer dat `CopyToOutputDirectory` is ingesteld op `PreserveNewest`
3. Check publish folder of `data/` directory bestaat

### Fout: API Connection Error (Frontend)
**Oorzaak**: Verkeerde API URL in frontend configuratie
**Oplossing**:
1. Check `frontend/CarRecommender.Web/appsettings.json` → `ApiSettings:BaseUrl`
2. Verifieer CORS instellingen in API (als nodig)
3. Test API URL direct in browser

---

## 📝 Samenvatting Wijzigingen

### Gewijzigde Bestanden
1. `.github/workflows/main_app-carrecommender-dev.yml`
   - Build: Specificeer API project path
   - Publish: Specificeer API project path en output directory
   - Artifact: Expliciet path `./publish`
   - Deployment: Package path `./publish`

2. `.github/workflows/main_pp-carrecommender-web-dev.yml`
   - Build: Specificeer Web project path
   - Publish: Specificeer Web project path en output directory
   - Artifact: Expliciet path `./publish`
   - Deployment: Package path `./publish`

### Belangrijkste Fixes
✅ Specificeer correct project in build/publish commands
✅ Gebruik expliciete paths voor artifacts
✅ Verwijs naar correcte package directory in deployment

---

## 🚀 Volgende Deployment

Na deze fixes zou de deployment moeten werken. Volg de checklist hierboven voor elke deployment om problemen te voorkomen.

