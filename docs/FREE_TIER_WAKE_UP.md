# Free Tier App Service - Wake-up Oplossing

## 🔍 Het Probleem

Je gebruikt een **Free Tier** App Service Plan:
- ❌ "Always On" is niet beschikbaar (vereist Basic of hoger)
- ⚠️ App gaat in slaapstand na ~20 minuten inactiviteit
- ⏱️ Eerste request na slapen duurt 30-60 seconden (cold start)
- ❌ URLs werken niet direct omdat app in slaapstand is

**Dit verklaart waarom je URLs niet werken!**

---

## ✅ Oplossing 1: Test Eerst of App Werkt (Na Wakker Maken)

### Stap 1: Maak App Wakker

Open deze URL in je browser en **wacht 30-60 seconden**:

```
https://app-carrecommender-dev.azurewebsites.net/api/health
```

**Verwacht:**
- Eerste keer: Kan 30-60 seconden duren (cold start) ⏱️
- Daarna: Direct `{"status": "OK"}` ✅

### Stap 2: Als het Werkt

✅ **De app werkt!** Het probleem is alleen dat het in slaapstand gaat na inactiviteit.

---

## ✅ Oplossing 2: GitHub Actions Wake-up Workflow (Gratis)

Je hebt al een wake-up workflow! Deze is nu geüpdatet met de juiste URLs.

### Hoe het Werkt:

1. **GitHub Actions** draait automatisch elke 10 minuten
2. Roept beide apps aan (backend + frontend)
3. Houdt apps actief (geen slaapstand)
4. **Volledig gratis** (GitHub Actions heeft gratis tier)

### Check of Workflow Actief Is:

1. Ga naar GitHub → **"Actions"** tab
2. Zoek naar **"Keep App Active"** workflow
3. Check of deze regelmatig draait (elke 10 minuten)

### Handmatig Triggeren (Test):

1. GitHub → **"Actions"** → **"Keep App Active"**
2. Klik op **"Run workflow"** → **"Run workflow"**
3. Wacht tot workflow klaar is
4. Test je app URL - zou direct moeten werken!

### Workflow Details:

- **Trigger:** Elke 10 minuten (cron: `*/10 * * * *`)
- **Backend:** `https://app-carrecommender-dev.azurewebsites.net/api/health`
- **Frontend:** `https://pp-carrecommender-web-dev.azurewebsites.net/`

---

## ✅ Oplossing 3: UptimeRobot (Aanbevolen - Gratis, 24/7)

**UptimeRobot** is een gratis cloud service die je app elke 5-10 minuten "wakker houdt".

### Voordelen:
- ✅ **Volledig gratis**
- ✅ **Werkt 24/7** (ook als je PC uitstaat)
- ✅ **Automatisch** (setup één keer)
- ✅ **Betrouwbaar** (cloud service)

### Stap 1: Maak Account

1. Ga naar: https://uptimerobot.com
2. Klik **"Sign Up"** (gratis)
3. Verifieer email

### Stap 2: Voeg Monitor Toe voor Backend

1. Klik **"Add New Monitor"**
2. Vul in:
   - **Monitor Type:** `HTTP(s)`
   - **Friendly Name:** `Car Recommender Backend`
   - **URL:** `https://app-carrecommender-dev.azurewebsites.net/api/health`
   - **Monitoring Interval:** `5 minutes` (of 10 minutes)
3. Klik **"Create Monitor"**

### Stap 3: Voeg Monitor Toe voor Frontend

1. Klik opnieuw **"Add New Monitor"**
2. Vul in:
   - **Monitor Type:** `HTTP(s)`
   - **Friendly Name:** `Car Recommender Frontend`
   - **URL:** `https://pp-carrecommender-web-dev.azurewebsites.net/`
   - **Monitoring Interval:** `5 minutes` (of 10 minutes)
3. Klik **"Create Monitor"**

### Resultaat:

- UptimeRobot checkt beide apps elke 5-10 minuten
- Apps blijven actief (geen slaapstand)
- Docenten kunnen altijd direct de website openen ✅

---

## 📊 Hoe het Werkt

### Zonder Wake-up Service:
```
Tijd 0:00 → App actief
Tijd 0:20 → App gaat in slaapstand (na 20 min inactiviteit)
Tijd 0:21 → Docent opent URL → Moet 30-60 seconden wachten (cold start) ⏱️
```

### Met Wake-up Service (elke 10 minuten):
```
Tijd 0:00 → App actief
Tijd 0:10 → Wake-up service roept app aan → App blijft actief ✅
Tijd 0:20 → Wake-up service roept app aan → App blijft actief ✅
Tijd 0:21 → Docent opent URL → Direct beschikbaar! ✅
```

---

## 🎯 Aanbevolen Aanpak

### Voor Nu (Directe Test):

1. **Test eerst of app werkt:**
   - Open: `https://app-carrecommender-dev.azurewebsites.net/api/health`
   - Wacht 30-60 seconden (cold start)
   - Als je `{"status": "OK"}` ziet: ✅ App werkt!

2. **Activeer GitHub Actions wake-up:**
   - Check of workflow actief is
   - Trigger handmatig om te testen

### Voor Productie (Lang Termijn):

**Gebruik UptimeRobot:**
- Setup één keer (5 minuten)
- Werkt automatisch 24/7
- Geen onderhoud nodig
- Betrouwbaarder dan GitHub Actions (werkt ook als repo privé is)

---

## 📋 Checklist

- [ ] Test app werkt na wakker maken (wacht 30-60 sec)
- [ ] GitHub Actions wake-up workflow is actief
- [ ] UptimeRobot account aangemaakt (optioneel maar aanbevolen)
- [ ] Monitors toegevoegd voor beide apps
- [ ] Test na 10 minuten of app direct werkt (geen wachttijd)

---

## 💡 Belangrijk

**Free Tier = Geen Always On, maar wel werkbaar!**

Met een wake-up service:
- ✅ Apps blijven actief
- ✅ Geen wachttijd voor gebruikers
- ✅ Volledig gratis
- ✅ Automatisch

**Je hoeft NIET te upgraden naar betaalde tier!** 🎉

---

## 🔧 Troubleshooting

### Probleem: App werkt nog steeds niet na wake-up

**Check:**
1. App Service status = "Running" in Azure Portal
2. Data file bestaat: `data/Cleaned_Car_Data_For_App_Fully_Enriched.csv`
3. web.config is correct
4. Test handmatig: `dotnet CarRecommender.Api.dll` in Kudu

### Probleem: GitHub Actions workflow draait niet

**Oplossing:**
- Check GitHub → Settings → Actions → Workflow permissions
- Zorg dat workflows enabled zijn
- Trigger handmatig om te testen

### Probleem: UptimeRobot werkt niet

**Check:**
- URLs zijn correct
- Monitoring interval is ingesteld (5-10 minuten)
- Monitor status is "Up"

---

**Status:** ✅ Oplossing voor Free Tier beschikbaar
**Aanbevolen:** UptimeRobot (gratis, automatisch, 24/7)





