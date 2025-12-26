# Diagnose: Geen Log Files Gevonden

## 🔍 Situatie
- ✅ Alle bestanden zijn aanwezig in wwwroot
- ✅ `CarRecommender.Api.dll` bestaat
- ✅ `web.config` bestaat
- ✅ `data/` folder bestaat
- ❌ Geen log files gevonden

**Dit betekent:** De applicatie is waarschijnlijk nog niet gestart of crasht direct bij startup.

---

## ✅ Stap 1: Check App Service Status

**Via Azure Portal:**
1. Ga naar [Azure Portal](https://portal.azure.com)
2. Zoek: `app-carrecommender-dev`
3. Check **Status**:
   - Moet **"Running"** zijn
   - Als **"Stopped"**: Klik op **"Start"**

**Dit is de meest waarschijnlijke oorzaak!**

---

## ✅ Stap 2: Check Data File

In Kudu console:

```cmd
cd data
dir
```

**Verwacht:**
- `Cleaned_Car_Data_For_App_Fully_Enriched.csv`

**Als ontbreekt:**
- Dit kan ervoor zorgen dat de applicatie crasht bij startup
- Upload het CSV bestand naar `data/` folder

---

## ✅ Stap 3: Check web.config

In Kudu console:

```cmd
cd ..\wwwroot
type web.config
```

**Verwacht:**
```xml
<aspNetCore processPath="dotnet" 
            arguments=".\CarRecommender.Api.dll" 
            ... />
```

**Als verkeerd:**
- Fix het of upload een nieuwe web.config

---

## ✅ Stap 4: Test Handmatig Starten

In Kudu console (wwwroot directory):

```cmd
dotnet CarRecommender.Api.dll
```

**Als dit werkt:**
- Je ziet output zoals "Now listening on: http://localhost:5000"
- De applicatie kan starten!
- Stop met Ctrl+C

**Als dit crasht:**
- Je ziet een error message
- Dit vertelt je precies wat er mis is
- Deel de error met mij!

---

## ✅ Stap 5: Check Application Logs via Azure Portal

**Alternatieve manier om logs te zien:**

1. Azure Portal → App Service → **"Log stream"**
2. Of: **"Monitoring"** → **"Log stream"**
3. Bekijk real-time logs
4. Probeer de applicatie te starten (herstart App Service)
5. Je zou nu logs moeten zien

---

## ✅ Stap 6: Check Startup Command

**Via Azure Portal:**

1. App Service → **"Configuration"** → **"General settings"**
2. Scroll naar **"Startup Command"**
3. Moet zijn:
   - **Leeg** (web.config handelt het af) ✅
   - OF: `dotnet CarRecommender.Api.dll`
4. Als verkeerd: Fix het en klik **"Save"**
5. **Herstart** App Service

---

## 🚀 Snelle Fixes

### Fix 1: Herstart App Service

**Dit lost vaak problemen op:**

1. Azure Portal → App Service → **"Restart"**
2. Wacht 30-60 seconden
3. Test: `https://app-carrecommender-dev.azurewebsites.net/api/health`

### Fix 2: Check Always On

**Voor Free Tier moet "Always On" aan staan:**

1. Azure Portal → App Service → **"Configuration"** → **"General settings"**
2. Scroll naar **"Always On"**
3. Moet **"On"** zijn
4. Als "Off": Zet aan en klik **"Save"**
5. Herstart App Service

### Fix 3: Upload Data File (Als Ontbreekt)

Als `data/Cleaned_Car_Data_For_App_Fully_Enriched.csv` ontbreekt:

1. Download van je lokale project: `backend/data/Cleaned_Car_Data_For_App_Fully_Enriched.csv`
2. Via Kudu: Ga naar `data/` folder
3. Sleep het CSV bestand naar het venster
4. Herstart App Service

---

## 📋 Checklist

- [ ] App Service status = **"Running"**
- [ ] Always On = **"On"**
- [ ] Startup Command = **Leeg** of `dotnet CarRecommender.Api.dll`
- [ ] `data/Cleaned_Car_Data_For_App_Fully_Enriched.csv` bestaat
- [ ] `web.config` is correct
- [ ] Test handmatig: `dotnet CarRecommender.Api.dll` werkt
- [ ] App Service herstart na wijzigingen

---

## 💡 Meest Waarschijnlijke Oorzaak

**App Service is gestopt!**

Check dit eerst:
1. Azure Portal → App Service → Status
2. Als "Stopped": Klik "Start"
3. Wacht tot status "Running" is
4. Test opnieuw

---

## 🔍 Debug Commands

```cmd
# Check data file
cd data
dir

# Check web.config
cd ..\wwwroot
type web.config

# Test applicatie starten
dotnet CarRecommender.Api.dll

# Check of process draait (na starten)
tasklist | findstr dotnet
```

---

**Deel de resultaten van deze checks, dan kan ik je verder helpen!**






