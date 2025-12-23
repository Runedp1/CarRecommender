# Kudu Deployment Diagnose - Waarom Werken URLs Niet?

## 🔍 Het Probleem: Bestanden naar Kudu

Je hebt bestanden in wwwroot, maar de URLs werken niet. Dit betekent dat er iets mis is met:
- De bestanden zelf
- De structuur
- Missing dependencies
- Configuratie problemen

---

## ✅ Stap 1: Check Data File (KRITIEK!)

**Dit is vaak de oorzaak!**

In Kudu console:

```cmd
cd data
dir
```

**Verwacht:**
```
Cleaned_Car_Data_For_App_Fully_Enriched.csv
```

**Als dit ontbreekt:**
- ❌ De applicatie crasht bij startup
- ❌ CarRepository kan CSV niet vinden
- ❌ Geen auto's geladen = applicatie werkt niet

**Fix:**
1. Download CSV van je lokale project: `backend/data/Cleaned_Car_Data_For_App_Fully_Enriched.csv`
2. Upload naar Kudu: `data/` folder
3. Herstart App Service

---

## ✅ Stap 2: Test Handmatig Starten (BELANGRIJK!)

Dit vertelt je precies wat er mis is:

In Kudu console (wwwroot directory):

```cmd
dotnet CarRecommender.Api.dll
```

### Als het werkt:
Je ziet output zoals:
```
Now listening on: http://localhost:5000
Application started. Press Ctrl+C to shut down.
```

✅ **Applicatie kan starten!** Het probleem is waarschijnlijk IIS configuratie.

### Als het crasht:
Je ziet een error zoals:
```
Unhandled exception. System.IO.FileNotFoundException: Could not find file '...'
```

❌ **Dit vertelt je precies wat er ontbreekt!**

**Deel de error met mij!**

---

## ✅ Stap 3: Check Runtime Dependencies

De applicatie heeft .NET 9.0 runtime nodig. Check of deze beschikbaar is:

In Kudu console:

```cmd
dotnet --version
```

**Verwacht:** `9.0.x` of hoger

**Als verkeerde versie:**
- Azure Portal → App Service → Configuration → General settings
- Check ".NET Version" = 9.0
- Save en herstart

---

## ✅ Stap 4: Check Alle Benodigde Bestanden

In Kudu console (wwwroot):

```cmd
dir
```

**Verwacht deze bestanden:**

### Core Bestanden (VERPLICHT):
- ✅ `CarRecommender.Api.dll` - De applicatie
- ✅ `web.config` - IIS configuratie
- ✅ `appsettings.json` - Configuratie
- ✅ `CarRecommender.Api.runtimeconfig.json` - Runtime configuratie

### Dependencies (VERPLICHT):
- ✅ `CarRecommender.dll` - Core library
- ✅ `Microsoft.OpenApi.dll` - Swagger dependencies
- ✅ `Swashbuckle.AspNetCore.*.dll` - Swagger dependencies

### Data (VERPLICHT):
- ✅ `data/` folder
- ✅ `data/Cleaned_Car_Data_For_App_Fully_Enriched.csv`

**Als iets ontbreekt:**
- Upload ontbrekende bestanden
- Of deploy opnieuw met correcte ZIP

---

## ✅ Stap 5: Check web.config

In Kudu console:

```cmd
type web.config
```

**Verwacht:**
```xml
<aspNetCore processPath="dotnet" 
            arguments=".\CarRecommender.Api.dll" 
            stdoutLogEnabled="true" 
            stdoutLogFile=".\logs\stdout" 
            hostingModel="inprocess">
```

**Check:**
- ✅ `processPath="dotnet"` (niet `dotnet.exe`)
- ✅ `arguments=".\CarRecommender.Api.dll"` (correct DLL naam)
- ✅ `stdoutLogEnabled="true"` (logs aan)

**Als verkeerd:**
- Fix web.config
- Upload opnieuw

---

## ✅ Stap 6: Check Application Logs

**Via Azure Portal (BETROUWBAARSTE):**

1. Azure Portal → App Service → **"Log stream"**
2. Open de URL in een andere tab: `https://app-carrecommender-dev.azurewebsites.net/api/health`
3. Bekijk de logs in real-time
4. Je ziet nu precies wat er gebeurt bij startup

**Via Kudu:**

```cmd
cd LogFiles\Application
dir /O-D
type stdout_*.log
```

**Als geen logs:**
- Applicatie start niet
- Check web.config
- Check App Service status

---

## ✅ Stap 7: Check App Service Status

**Via Azure Portal:**

1. App Service → **Status** (bovenaan)
2. Moet **"Running"** zijn
3. Als **"Stopped"**: Klik **"Start"**

**Check ook:**
- **Configuration** → **General settings** → **.NET Version** = 9.0
- **Configuration** → **General settings** → **Platform** = 64 Bit

---

## 🔧 Meest Waarschijnlijke Problemen

### Probleem 1: Data File Ontbreekt (90% kans)

**Symptoom:** Applicatie start maar crasht direct

**Fix:**
```cmd
# Upload CSV naar data folder via Kudu
cd data
# Sleep CSV bestand naar Kudu file browser
```

### Probleem 2: Verkeerde .NET Version

**Symptoom:** `dotnet CarRecommender.Api.dll` geeft versie error

**Fix:**
- Azure Portal → Configuration → .NET Version = 9.0
- Save en herstart

### Probleem 3: Missing Dependencies

**Symptoom:** `dotnet CarRecommender.Api.dll` geeft "Could not load file or assembly"

**Fix:**
- Deploy opnieuw met volledige publish output
- Zorg dat alle DLL files meegenomen worden

### Probleem 4: web.config Verkeerd

**Symptoom:** IIS kan applicatie niet starten

**Fix:**
- Check web.config syntax
- Verifieer DLL naam klopt

---

## 🚀 Snelle Fix: Herdeploy met Correcte ZIP

Als niets werkt, deploy opnieuw met handmatige ZIP:

### Stap 1: Download Artifact van GitHub Actions

1. GitHub → Actions → Laatste workflow run → build job
2. Download ".net-app" artifact
3. Pak uit

### Stap 2: Maak Nieuwe ZIP

```powershell
# Ga naar publish folder
cd <uitgepakte-artifact>\publish

# Verifieer data file bestaat
dir data

# Maak ZIP
Compress-Archive -Path * -DestinationPath ..\deploy.zip -Force
```

### Stap 3: Upload naar Kudu

1. Kudu → `site/wwwroot`
2. Sleep `deploy.zip` naar venster
3. Unzip:
   ```cmd
   powershell Expand-Archive -Path deploy.zip -DestinationPath . -Force
   ```
4. Verifieer:
   ```cmd
   dir
   dir data
   ```
5. Herstart App Service

---

## 📋 Diagnose Checklist

Voer deze checks uit in Kudu en deel de resultaten:

- [ ] `cd data` → `dir` → CSV bestand bestaat?
- [ ] `dotnet CarRecommender.Api.dll` → Werkt of crasht? (deel error)
- [ ] `dotnet --version` → Welke versie?
- [ ] `type web.config` → Is correct?
- [ ] Azure Portal → Log stream → Wat zie je bij startup?
- [ ] Azure Portal → App Service → Status = Running?

**Deel de resultaten van deze checks, dan kan ik precies zien wat er mis is!**

