# Debug: GitHub Deployment Succesvol maar URL Werkt Niet

## ✅ Goed Nieuws: GitHub Deployment Succesvol!

Als GitHub Actions deployment succesvol is, betekent dit:
- ✅ Code is gebuild
- ✅ Bestanden zijn geüpload naar Azure
- ✅ Deployment proces is voltooid

**Maar:** De applicatie start mogelijk niet correct op Azure.

---

## 🔍 Stap 1: Wat Zie Je Wanneer Je de URL Opent?

**Test deze URL:**
```
https://app-carrecommender-dev.azurewebsites.net/api/health
```

**Wat gebeurt er?**
- ❌ **404 Not Found** → Routing probleem of applicatie start niet
- ❌ **500 Internal Server Error** → Applicatie crasht bij startup
- ❌ **DNS_PROBE_FINISHED_NXDOMAIN** → App Service bestaat niet of is gestopt
- ❌ **Timeout** → Applicatie start niet
- ✅ **{"status": "OK"}** → Werkt perfect!

**Deel wat je ziet!**

---

## 🔍 Stap 2: Check Azure Portal → App Service Status

**BELANGRIJK:** App Service moet "Running" zijn!

1. **Azure Portal** → **App Services** → **`app-carrecommender-dev`**
2. Check **"Status"** bovenaan:
   - ✅ **"Running"** = Goed
   - ❌ **"Stopped"** = Klik **"Start"**

**Als App Service gestopt is:** URLs werken niet!

---

## 🔍 Stap 3: Check .NET Version Configuratie

**KRITIEK:** Azure moet op .NET 8.0 staan!

1. **Azure Portal** → **App Services** → **`app-carrecommender-dev`**
2. **Configuration** → **General settings**
3. Scroll naar **".NET Version"**
4. **Wat staat er?**
   - ✅ **`8.0`** of **`8.x`** = Goed
   - ❌ **`9.0`** = **PROBLEEM!** Wijzig naar **`8.0`** en klik **"Save"**
   - ❌ **Leeg** = **PROBLEEM!** Zet op **`8.0`** en klik **"Save"**

5. **Na wijziging:** Klik **"Save"** bovenaan
6. **Herstart App Service:**
   - Ga naar **"Overview"**
   - Klik **"Restart"**

**Dit is vaak de oorzaak!**

---

## 🔍 Stap 4: Check Application Logs (BELANGRIJK!)

**Dit vertelt je precies wat er mis is:**

### Via Azure Portal Log Stream:

1. **Azure Portal** → **App Services** → **`app-carrecommender-dev`**
2. **"Log stream"** (in menu links)
3. **Open de URL in browser** (andere tab): `https://app-carrecommender-dev.azurewebsites.net/api/health`
4. **Wat zie je in de logs?**
   - Startup messages?
   - Errors?
   - Exception details?

**Deel de logs met mij!**

### Via Kudu (Alternatief):

1. **Azure Portal** → **App Services** → **`app-carrecommender-dev`**
2. **"Advanced Tools"** → **"Go"** (opent Kudu)
3. **"Debug console"** → **"CMD"**
4. Navigate naar: `site/wwwroot`
5. Check logs: `LogFiles\Application`

---

## 🔍 Stap 5: Check Data File (Als Applicatie Start Maar Crasht)

**Als applicatie start maar crasht bij data loading:**

1. **Kudu** → **"Debug console"** → **"CMD"**
2. Navigate naar: `site/wwwroot/data`
3. **Check of CSV bestand bestaat:**
   ```cmd
   dir
   ```
4. **Verwacht:** `Cleaned_Car_Data_For_App_Fully_Enriched.csv`

**Als bestand ontbreekt:**
- Upload handmatig via Kudu File Manager
- Of check GitHub Actions deployment output

---

## 🔍 Stap 6: Test Handmatig Starten in Kudu

**Dit vertelt je precies wat er mis is:**

1. **Kudu** → **"Debug console"** → **"CMD"**
2. Navigate naar: `site/wwwroot`
3. **Start applicatie handmatig:**
   ```cmd
   dotnet CarRecommender.Api.dll
   ```
4. **Wat zie je?**
   - ✅ "Now listening on: http://localhost:5000" = Applicatie werkt!
   - ❌ Error message = Dit is het probleem!

**Deel de output!**

---

## 🔧 Veelvoorkomende Problemen

### Probleem 1: .NET Version Verkeerd

**Symptoom:** Applicatie start niet, geen logs.

**Fix:**
- Azure Portal → Configuration → .NET Version = 8.0
- Save en Restart

### Probleem 2: Data File Ontbreekt

**Symptoom:** Applicatie start maar crasht bij startup.

**Fix:**
- Upload CSV naar `site/wwwroot/data/`
- Herstart App Service

### Probleem 3: App Service Gestopt

**Symptoom:** DNS_PROBE_FINISHED_NXDOMAIN of timeout.

**Fix:**
- Azure Portal → Overview → Start

### Probleem 4: Startup Command Verkeerd

**Symptoom:** Applicatie start niet.

**Fix:**
- Check `web.config` in Kudu
- Moet zijn: `dotnet CarRecommender.Api.dll`

---

## 📋 Debug Checklist

- [ ] App Service Status = "Running"
- [ ] .NET Version = 8.0 (niet 9.0!)
- [ ] App Service herstart na configuratie wijziging
- [ ] Log stream bekeken voor errors
- [ ] Data file aanwezig in `site/wwwroot/data/`
- [ ] Handmatig starten getest in Kudu

---

## 💡 Geen VS Code Deployment Nodig!

**GitHub Actions deployment is genoeg!** Als GitHub Actions succesvol is, zijn de bestanden al op Azure.

**Het probleem zit waarschijnlijk in:**
- Azure Portal configuratie (.NET Version)
- App Service niet herstart
- Applicatie startup errors

**Niet in:** Missing deployment (dat is al gedaan via GitHub Actions).

---

**Volgende Stap:** Check Azure Portal → .NET Version en Log stream, deel wat je ziet!


