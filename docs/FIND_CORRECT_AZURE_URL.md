# Vind de Juiste Azure App Service URL

## 🔴 Het Probleem: DNS_PROBE_FINISHED_NXDOMAIN

Deze error betekent dat de DNS lookup faalt - de App Service bestaat mogelijk niet of de URL is verkeerd.

---

## ✅ Stap 1: Vind de Juiste URL via Azure Portal

### Methode 1: Via App Service Overzicht

1. **Ga naar Azure Portal:** https://portal.azure.com
2. **Zoek naar "App Services"** (in zoekbalk bovenaan)
3. **Klik op "App Services"**
4. **Zoek naar je App Service:**
   - `app-carrecommender-dev` (Backend API)
   - Of een andere naam?

5. **Klik op de App Service naam**
6. **Bovenaan zie je de URL:**
   - Bijvoorbeeld: `https://app-carrecommender-dev-h0agdkbfbcb3fxg7.westeurope-01.azurewebsites.net`
   - OF: `https://app-carrecommender-dev.azurewebsites.net`

**Noteer deze exacte URL!**

### Methode 2: Via App Service Properties

1. App Service → **"Overview"** (in menu links)
2. Zoek naar **"Default domain"** of **"URL"**
3. **Dit is je App Service URL**

---

## ✅ Stap 2: Test de URL

Gebruik de URL die je in Azure Portal ziet:

### Als URL is: `https://app-carrecommender-dev-h0agdkbfbcb3fxg7.westeurope-01.azurewebsites.net`

Test:
```
https://app-carrecommender-dev-h0agdkbfbcb3fxg7.westeurope-01.azurewebsites.net/api/health
```

### Als URL is: `https://app-carrecommender-dev.azurewebsites.net`

Test:
```
https://app-carrecommender-dev.azurewebsites.net/api/health
```

---

## 🔍 Stap 3: Check App Service Status

**In Azure Portal:**

1. App Service → **"Overview"**
2. Check **"Status"** bovenaan:
   - ✅ **"Running"** = Goed
   - ❌ **"Stopped"** = Klik op **"Start"**

**Als App Service gestopt is:**
- DNS werkt niet
- URLs geven DNS errors
- Start de App Service eerst!

---

## 🔍 Stap 4: Check of App Service Bestaat

**In Azure Portal:**

1. **"App Services"** → Bekijk alle App Services
2. **Zoek naar:**
   - `app-carrecommender-dev`
   - `app-carrecommender-web-dev`
   - `pp-carrecommender-web-dev`
   - Of andere namen?

**Als App Service NIET bestaat:**
- Je moet een nieuwe App Service aanmaken
- Of de deployment is naar een andere App Service gegaan

---

## 🔍 Stap 5: Check via Kudu (Als Je Kudu Kunt Openen)

Als je Kudu kunt openen (`https://app-carrecommender-dev.scm.azurewebsites.net`), dan bestaat de App Service wel!

**Maar de publieke URL kan anders zijn:**
- Kudu URL: `*.scm.azurewebsites.net` (werkt altijd als App Service bestaat)
- Publieke URL: `*.azurewebsites.net` (kan regionale suffix hebben)

---

## 🚀 Snelle Fix

### Optie 1: Gebruik de URL uit Azure Portal

1. Azure Portal → App Service → **"Overview"**
2. Kopieer de **"Default domain"** URL
3. Test deze URL

### Optie 2: Check App Service Naam

De App Service naam in GitHub Actions workflow is: `app-carrecommender-dev`

**Maar de echte URL kan zijn:**
- `https://app-carrecommender-dev.azurewebsites.net` (kort)
- `https://app-carrecommender-dev-h0agdkbfbcb3fxg7.westeurope-01.azurewebsites.net` (met regionale suffix)

**Gebruik de URL die Azure Portal toont!**

---

## 📋 Checklist

- [ ] Azure Portal → App Services → Check welke App Services bestaan
- [ ] Noteer de exacte naam van je App Service
- [ ] Kopieer de "Default domain" URL uit Azure Portal
- [ ] Test deze URL in browser
- [ ] Check App Service status = "Running"

---

## 💡 Belangrijk

**Azure App Service URLs kunnen verschillen:**
- Soms: `app-name.azurewebsites.net`
- Soms: `app-name-xxxxx.region.azurewebsites.net`

**Gebruik altijd de URL die Azure Portal toont!**

---

## 🔧 Als App Service Niet Bestaat

Als de App Service niet bestaat in Azure Portal:

1. **Maak nieuwe App Service aan:**
   - Azure Portal → App Services → "Create"
   - Name: `app-carrecommender-dev`
   - Resource Group: Bestaande of nieuwe
   - App Service Plan: Bestaande of nieuwe (Free tier)
   - Region: West Europe

2. **Deploy opnieuw:**
   - Via GitHub Actions (automatisch)
   - Of handmatig via Kudu

---

**Deel de exacte URL die je ziet in Azure Portal, dan kunnen we verder!**




