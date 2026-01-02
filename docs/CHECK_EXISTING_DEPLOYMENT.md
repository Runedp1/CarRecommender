# Check Bestaande Deployment in Kudu

## ✅ Wat ik zie in je Kudu wwwroot:

Je hebt al bestanden in `wwwroot`:
- ✅ `CarRecommender.Api.dll` (26 KB) - **De applicatie zelf**
- ✅ `appsettings.json` - Configuratie
- ✅ `data/` folder - Data bestanden
- ✅ `images/` folder - Images
- ✅ `web.config` - IIS configuratie

**Dit betekent: De deployment is al gedaan!** 🎉

---

## 🔍 Het Probleem: Waarom Werkt het Niet?

Als de URLs niet werken, zijn er een paar mogelijkheden:

### Mogelijkheid 1: Applicatie Start Niet

**Check dit:**

1. **Via Kudu Console** (waar je nu bent):
   ```cmd
   # Check of er een web.config bestaat
   dir web.config
   
   # Check de inhoud van web.config
   type web.config
   ```

2. **Check Application Logs**:
   - Ga naar Azure Portal → App Service → **"Log stream"**
   - Of via Kudu: `LogFiles\Application`
   - Zoek naar errors bij startup

### Mogelijkheid 2: Data File Ontbreekt

**Check dit:**

```cmd
# In Kudu console, check data folder
cd data
dir

# Je zou moeten zien:
# Cleaned_Car_Data_For_App_Fully_Enriched.csv
```

Als het CSV bestand ontbreekt:
- De applicatie kan crashen bij startup
- Upload het CSV bestand naar `data/` folder

### Mogelijkheid 3: App Service Niet Gestart

**Check dit:**

1. Azure Portal → App Service → **Status**
2. Moet **"Running"** zijn (groen)
3. Als **"Stopped"**: Klik op **"Start"**

### Mogelijkheid 4: Verkeerde URL

**Check de exacte URL:**

De URL moet zijn:
```
https://app-carrecommender-dev.azurewebsites.net/api/health
```

**NIET:**
- `https://app-carrecommender-dev-h0agdkbfbcb3fxg7.westeurope-01.azurewebsites.net` (oude URL?)
- `http://` in plaats van `https://`

---

## 🔧 Stap-voor-Stap Diagnose

### Stap 1: Check App Service Status

1. Ga naar [Azure Portal](https://portal.azure.com)
2. Zoek: `app-carrecommender-dev`
3. Check **Status**:
   - ✅ **Running** = Goed
   - ❌ **Stopped** = Klik "Start"

### Stap 2: Check Application Logs

**Via Azure Portal:**
1. App Service → **"Log stream"** (in menu links)
2. Bekijk real-time logs
3. Zoek naar:
   - "Application started"
   - Errors
   - "Failed to start"

**Via Kudu:**
```cmd
# In Kudu console
cd LogFiles\Application
dir /O-D
type stdout_*.log
```

### Stap 3: Check web.config

```cmd
# In Kudu console (wwwroot directory)
type web.config
```

**Verwacht:**
```xml
<aspNetCore processPath="dotnet" 
            arguments=".\CarRecommender.Api.dll" 
            ... />
```

Als `arguments` verkeerd is of ontbreekt, fix het.

### Stap 4: Check Data File

```cmd
# In Kudu console
cd data
dir
```

**Verwacht:**
- `Cleaned_Car_Data_For_App_Fully_Enriched.csv`

Als ontbreekt:
- Upload het CSV bestand naar `data/` folder

### Stap 5: Test Handmatig Starten

```cmd
# In Kudu console (wwwroot directory)
dotnet CarRecommender.Api.dll
```

Als dit werkt, zie je output. Als het crasht, zie je de error.

**Stop met Ctrl+C** na testen.

---

## 🚀 Snelle Fixes

### Fix 1: Herstart App Service

**Via Azure Portal:**
1. App Service → **"Restart"**
2. Wacht 30 seconden
3. Test opnieuw

### Fix 2: Check Startup Command

**Via Azure Portal:**
1. App Service → **"Configuration"** → **"General settings"**
2. Check **"Startup Command"**:
   - Moet leeg zijn (web.config handelt het af)
   - OF: `dotnet CarRecommender.Api.dll`
3. Klik **"Save"**
4. Herstart App Service

### Fix 3: Upload Ontbrekende Data File

Als `data/Cleaned_Car_Data_For_App_Fully_Enriched.csv` ontbreekt:

1. Download het CSV bestand van je lokale project:
   - `backend/data/Cleaned_Car_Data_For_App_Fully_Enriched.csv`

2. Upload via Kudu:
   - Ga naar `data/` folder in Kudu file explorer
   - Sleep het CSV bestand naar het venster

---

## 📋 Checklist

- [ ] App Service status = "Running"
- [ ] `web.config` bestaat en is correct
- [ ] `data/Cleaned_Car_Data_For_App_Fully_Enriched.csv` bestaat
- [ ] Application logs tonen geen errors
- [ ] Startup command is correct (of leeg)
- [ ] Test URL is correct: `https://app-carrecommender-dev.azurewebsites.net/api/health`

---

## 💡 Wat Nu?

**Als alles er is maar het werkt niet:**

1. **Check Application Logs** - Dit vertelt je precies wat er mis gaat
2. **Herstart App Service** - Soms lost dit problemen op
3. **Test handmatig** - `dotnet CarRecommender.Api.dll` in Kudu console

**Deel de Application Logs met mij** als je de exacte error wilt weten!

---

## 🔍 Debug Commands voor Kudu

```cmd
# Check alle bestanden
dir

# Check web.config
type web.config

# Check appsettings.json
type appsettings.json

# Check data folder
cd data
dir

# Check logs
cd ..\LogFiles\Application
dir /O-D
type stdout_*.log

# Test applicatie starten
cd ..\..\wwwroot
dotnet CarRecommender.Api.dll
```









