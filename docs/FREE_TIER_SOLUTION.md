# Free Tier App Service - Oplossing

## 🔍 Het Probleem

Je gebruikt een **Free Tier** App Service plan:
- ❌ "Always On" is niet beschikbaar
- ⚠️ App gaat in slaapstand na ~20 minuten inactiviteit
- ⏱️ Eerste request na slapen duurt 30-60 seconden (cold start)

**Dit verklaart waarom je URLs niet werken!**

---

## ✅ Oplossing 1: Test Eerst of App Werkt (Na Wakker Maken)

### Stap 1: Maak App Wakker

Open deze URL in je browser en **wacht 30-60 seconden**:
```
https://app-carrecommender-dev.azurewebsites.net/api/health
```

**Verwacht:**
- Eerste keer: Kan 30-60 seconden duren (cold start)
- Daarna: Direct `{"status": "OK"}`

### Stap 2: Als het Werkt

✅ De app werkt! Het probleem is alleen dat het in slaapstand gaat.

---

## ✅ Oplossing 2: Gebruik Bestaande GitHub Actions Wake-up Workflow

Je hebt al een wake-up workflow! Deze moet alleen geactiveerd worden.

### Stap 1: Check of Workflow Actief Is

1. Ga naar GitHub → **"Actions"** tab
2. Zoek naar **"Keep App Active"** workflow
3. Check of deze regelmatig draait (elke 10 minuten)

### Stap 2: Activeer Workflow (Als Niet Actief)

De workflow zou automatisch moeten draaien, maar check:

1. GitHub → **"Actions"** → **"Keep App Active"**
2. Klik op **"Run workflow"** → **"Run workflow"** (handmatig triggeren)
3. Check of het werkt

### Stap 3: Update URLs in Workflow (Als Nodig)

Check of de URLs in `.github/workflows/wake-up.yml` correct zijn:
- Backend: `https://app-carrecommender-dev.azurewebsites.net/api/health`
- Frontend: `https://pp-carrecommender-web-dev.azurewebsites.net`

---

## ✅ Oplossing 3: Gebruik UptimeRobot (Aanbevolen - Gratis)

**UptimeRobot** is een gratis service die je app elke 5-10 minuten "wakker houdt".

### Stap 1: Maak Account

1. Ga naar: https://uptimerobot.com
2. Klik **"Sign Up"** (gratis)
3. Verifieer email

### Stap 2: Voeg Monitor Toe

1. Klik **"Add New Monitor"**
2. Vul in:
   - **Monitor Type:** `HTTP(s)`
   - **Friendly Name:** `Car Recommender API`
   - **URL:** `https://app-carrecommender-dev.azurewebsites.net/api/health`
   - **Monitoring Interval:** `5 minutes`
3. Klik **"Create Monitor"**

### Stap 3: Herhaal voor Frontend (Als Je Die Ook Hebt)

Voeg nog een monitor toe voor je frontend URL.

**Resultaat:**
- ✅ App blijft actief (geen slaapstand)
- ✅ URLs werken altijd direct
- ✅ Volledig gratis
- ✅ Werkt 24/7 (ook als je PC uitstaat)

---

## 🔧 Snelle Test Nu

### Test 1: Maak App Wakker

1. Open: `https://app-carrecommender-dev.azurewebsites.net/api/health`
2. **Wacht 30-60 seconden** (cold start)
3. Je zou moeten zien: `{"status": "OK"}`

### Test 2: Als Het Werkt

✅ De app werkt! Setup dan UptimeRobot of activeer de GitHub Actions workflow om het actief te houden.

### Test 3: Als Het Niet Werkt

Check dan:
1. App Service status in Azure Portal (moet "Running" zijn)
2. Data file bestaat (`cd data` → `dir` in Kudu)
3. Application logs voor errors

---

## 📋 Checklist

- [ ] Test app na wakker maken (wacht 30-60 sec)
- [ ] Als werkt: Setup UptimeRobot of activeer GitHub Actions workflow
- [ ] Test opnieuw na 10 minuten (zou direct moeten werken)
- [ ] Deel URL met docenten

---

## 💡 Aanbeveling

**Voor nu (directe test):**
1. Open de URL en wacht 30-60 seconden
2. Als het werkt: Setup UptimeRobot (5 minuten werk)
3. App blijft dan altijd actief

**Voor productie:**
- UptimeRobot (gratis, automatisch, 24/7) ✅
- OF GitHub Actions wake-up workflow (als je GitHub gebruikt) ✅

---

**Test eerst of de app werkt na wakker maken, dan kunnen we verder!**

