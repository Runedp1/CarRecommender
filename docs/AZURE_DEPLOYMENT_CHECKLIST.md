# Azure Deployment Checklist - .NET 8.0

## ✅ Stap 1: Commit en Push Wijzigingen

**Alle wijzigingen moeten eerst gecommit worden voordat GitHub Actions kan deployen:**

```bash
# Check wat er gewijzigd is
git status

# Voeg alle wijzigingen toe
git add .

# Commit met duidelijke message
git commit -m "Downgrade to .NET 8.0 for Azure compatibility - Fix MapStaticAssets"

# Push naar GitHub (triggert automatisch deployment)
git push origin main
```

**Na push:** GitHub Actions start automatisch een nieuwe deployment.

---

## ✅ Stap 2: Check Azure Portal Configuratie

**BELANGRIJK:** Azure App Service moet op .NET 8.0 staan!

### Backend API (`app-carrecommender-dev`):

1. **Azure Portal** → **App Services** → **`app-carrecommender-dev`**
2. **Configuration** → **General settings**
3. Check **".NET Version"**:
   - ✅ Moet zijn: **`8.0`** (of nieuwste 8.x)
   - ❌ Als het nog **`9.0`** is: Wijzig naar **`8.0`** en klik **"Save"**
4. **Herstart** App Service:
   - Ga naar **"Overview"**
   - Klik **"Restart"**

### Frontend Web (`pp-carrecommender-web-dev`):

1. **Azure Portal** → **App Services** → **`pp-carrecommender-web-dev`**
2. **Configuration** → **General settings**
3. Check **".NET Version"**:
   - ✅ Moet zijn: **`8.0`** (of nieuwste 8.x)
   - ❌ Als het nog **`9.0`** is: Wijzig naar **`8.0`** en klik **"Save"**
4. **Herstart** App Service

---

## ✅ Stap 3: Wacht op GitHub Actions Deployment

**Na push:**

1. Ga naar **GitHub** → **Actions** tab
2. Zoek naar workflow: **"Build and deploy ASP.Net Core app to Azure Web App"**
3. Klik op de **nieuwste run**
4. **Wacht tot deployment klaar is** (groene checkmark)

**Als deployment faalt:**
- Check de error logs in GitHub Actions
- Deel de error met mij

---

## ✅ Stap 4: Test Azure URLs

### Backend API:

**URL:** `https://app-carrecommender-dev.azurewebsites.net/api/health`

**Verwacht:**
```json
{"status": "OK"}
```

**Als het niet werkt:**
- Check Azure Portal → **Log stream** voor errors
- Check of App Service **"Running"** is
- Check of **.NET Version** = **8.0**

### Frontend Web:

**URL:** `https://pp-carrecommender-web-dev.azurewebsites.net`

**Verwacht:**
- HTML website (niet JSON!)
- Homepage met zoekfunctionaliteit

---

## ✅ Stap 5: Check Application Logs

**Als URLs nog steeds niet werken:**

### Via Azure Portal:

1. App Service → **"Log stream"**
2. Open de URL in browser (andere tab)
3. **Wat zie je in de logs?**
   - Startup messages?
   - Errors?
   - Requests?

### Via Kudu (Alternatief):

1. App Service → **"Advanced Tools"** → **"Go"** (Kudu)
2. **"Debug console"** → **"CMD"**
3. Navigate naar: `site/wwwroot`
4. Check logs: `LogFiles\Application`

---

## 🔍 Troubleshooting

### Probleem: DNS_PROBE_FINISHED_NXDOMAIN

**Oorzaak:** App Service bestaat niet of is gestopt.

**Fix:**
1. Azure Portal → Check of App Service bestaat
2. Check **Status** = **"Running"**
3. Als **"Stopped"**: Klik **"Start"**

### Probleem: 404 Not Found

**Oorzaak:** Applicatie start niet of routing werkt niet.

**Fix:**
1. Check Azure Portal → **Log stream** voor startup errors
2. Check **.NET Version** = **8.0**
3. Herstart App Service

### Probleem: 500 Internal Server Error

**Oorzaak:** Applicatie crasht bij startup.

**Fix:**
1. Check Azure Portal → **Log stream** voor exception details
2. Check of data file aanwezig is: `site/wwwroot/data/Cleaned_Car_Data_For_App_Fully_Enriched.csv`
3. Check Application Logs in Kudu

---

## 📋 Quick Checklist

- [ ] Wijzigingen gecommit en gepusht
- [ ] GitHub Actions deployment succesvol
- [ ] Azure Portal → .NET Version = 8.0 (Backend)
- [ ] Azure Portal → .NET Version = 8.0 (Frontend)
- [ ] App Services herstart
- [ ] Backend API URL werkt: `/api/health`
- [ ] Frontend Web URL werkt: `/`

---

**Status:** ⏳ Wacht op commit en deployment
**Volgende:** Commit, push, en test Azure URLs!






