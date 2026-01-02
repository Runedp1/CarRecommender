# Debug: App Werkt Nog Steeds Niet Na Data Upload

## ✅ Wat Je Al Gedaan Hebt:
- ✅ CSV bestand geüpload naar `data/` folder
- ✅ App Service herstart
- ❌ URLs werken nog steeds niet

**Nu moeten we de exacte error vinden!**

---

## 🔍 Stap 1: Test Handmatig Starten (KRITIEK!)

Dit vertelt je precies wat er mis is:

**In Kudu Console (wwwroot directory):**

```cmd
cd site\wwwroot
dotnet CarRecommender.Api.dll
```

### Wat Te Zien:

**Als het werkt:**
```
Now listening on: http://localhost:5000
Application started. Press Ctrl+C to shut down.
```
✅ **Applicatie kan starten!** Het probleem is IIS configuratie.

**Als het crasht:**
Je ziet een error zoals:
```
Unhandled exception. System.IO.FileNotFoundException: Could not find file '...'
```
of
```
System.InvalidOperationException: ...
```

❌ **Deel deze exacte error met mij!**

**Stop met Ctrl+C** na testen.

---

## 🔍 Stap 2: Check Application Logs (BELANGRIJK!)

**Via Azure Portal (BETROUWBAARSTE):**

1. Azure Portal → App Service → **"Log stream"** (in menu links)
2. Open in een andere tab: `https://app-carrecommender-dev.azurewebsites.net/api/health`
3. Bekijk de logs in real-time
4. **Wat zie je?** (errors, warnings, of niets?)

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

## 🔍 Stap 3: Verifieer Data File

**In Kudu Console:**

```cmd
cd site\wwwroot\data
dir
```

**Verwacht:**
```
Cleaned_Car_Data_For_App_Fully_Enriched.csv
```

**Check ook bestandsgrootte:**
```cmd
dir Cleaned_Car_Data_For_App_Fully_Enriched.csv
```

**Als bestand 0 bytes of zeer klein:**
- Upload was mogelijk niet compleet
- Upload opnieuw

---

## 🔍 Stap 4: Check web.config

**In Kudu Console:**

```cmd
cd site\wwwroot
type web.config
```

**Verwacht:**
```xml
<aspNetCore processPath="dotnet" 
            arguments=".\CarRecommender.Api.dll" 
            ... />
```

**Check:**
- ✅ `processPath="dotnet"` (niet `dotnet.exe`)
- ✅ `arguments=".\CarRecommender.Api.dll"` (correct DLL naam)
- ✅ Geen syntax errors

---

## 🔍 Stap 5: Check App Service Status & Configuratie

**Via Azure Portal:**

1. **Status:**
   - Moet **"Running"** zijn
   - Als **"Stopped"**: Klik **"Start"**

2. **Configuration → General settings:**
   - **.NET Version:** Moet `9.0` zijn
   - **Platform:** Moet `64 Bit` zijn
   - **Startup Command:** Moet **leeg** zijn (of `dotnet CarRecommender.Api.dll`)
   - Klik **"Save"** als je iets gewijzigd hebt
   - **Herstart** App Service

---

## 🔍 Stap 6: Test Direct in Browser

Open deze URL en **wacht 30-60 seconden**:

```
https://app-carrecommender-dev.azurewebsites.net/api/health
```

**Wat zie je?**
- ❌ 404 Not Found?
- ❌ 500 Internal Server Error?
- ❌ Timeout?
- ❌ Andere error?

**Deel de exacte error die je ziet!**

---

## 🔍 Stap 7: Check Runtime Dependencies

**In Kudu Console:**

```cmd
dotnet --version
```

**Verwacht:** `9.0.x` of hoger

**Als verkeerde versie:**
- Azure Portal → Configuration → .NET Version = 9.0
- Save en herstart

---

## 🐛 Meest Waarschijnlijke Problemen

### Probleem 1: Applicatie Crasht Bij Startup

**Symptoom:** `dotnet CarRecommender.Api.dll` crasht

**Mogelijke oorzaken:**
- Missing dependency DLL
- Verkeerde .NET runtime versie
- Configuratie probleem

**Fix:** Deel de exacte error van `dotnet CarRecommender.Api.dll`

### Probleem 2: IIS Kan Applicatie Niet Starten

**Symptoom:** Geen logs, applicatie start niet

**Mogelijke oorzaken:**
- web.config verkeerd
- Missing ASP.NET Core Module
- Verkeerde startup command

**Fix:** Check web.config, check App Service configuratie

### Probleem 3: Routes Werken Niet

**Symptoom:** Applicatie start maar endpoints geven 404

**Mogelijke oorzaken:**
- Routes niet correct geconfigureerd
- Controllers niet gevonden

**Fix:** Check Program.cs routing configuratie

---

## 📋 Wat Ik Nodig Heb

Voer deze checks uit en deel de resultaten:

1. **`dotnet CarRecommender.Api.dll` resultaat:**
   - Werkt het? (zie je "Now listening on...")
   - Of crasht het? (deel de exacte error)

2. **Azure Portal → Log stream:**
   - Wat zie je bij startup?
   - Zijn er errors?

3. **Browser test:**
   - Wat gebeurt er als je de URL opent?
   - Welke error zie je? (404, 500, timeout, etc.)

4. **Data file verificatie:**
   - Bestaat het CSV bestand? (ja/nee)
   - Wat is de bestandsgrootte?

**Met deze informatie kan ik precies zien wat er mis is!**

---

## 🚀 Snelle Test Commands

Voer deze uit in Kudu en deel de output:

```cmd
# Check data file
cd site\wwwroot\data
dir

# Check .NET version
cd ..\wwwroot
dotnet --version

# Test applicatie starten
dotnet CarRecommender.Api.dll
```

**Stop applicatie met Ctrl+C na testen!**

---

**Deel de resultaten van deze checks, dan kan ik je precies helpen!**









