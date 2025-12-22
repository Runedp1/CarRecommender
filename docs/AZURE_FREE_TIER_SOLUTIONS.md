# Azure Free Tier - Oplossingen zonder Always On

## 🔍 Probleem

Je hebt een gratis/student Azure account en kan **"Always On"** niet inschakelen. Dit kan problemen veroorzaken:
- App Service gaat in slaapstand na ~20 minuten inactiviteit
- DNS kan problemen geven wanneer de app in slaapstand is
- Eerste request na slaapstand kan lang duren ("cold start")

---

## ✅ Oplossingen (Zonder Always On)

### Oplossing 1: Wacht even en probeer opnieuw

Als de App Service in slaapstand is:
1. **Wacht 30-60 seconden** na het openen van de URL
2. **Ververs de pagina** (F5)
3. De app zou moeten "waken" en werken

**Waarom:** De eerste request na slaapstand duurt langer omdat de app moet opstarten.

---

### Oplossing 2: Gebruik een "Wake-up" Script

Je kunt een script maken dat periodiek de app "wakker houdt":

**Optioneel - Wake-up Script (PowerShell):**
```powershell
# Voer dit periodiek uit (bijv. elke 15 minuten)
Invoke-WebRequest -Uri "https://app-carrecommender-web-dev2.azurewebsites.net" -UseBasicParsing
```

Of gebruik een gratis service zoals:
- **UptimeRobot** (gratis): https://uptimerobot.com
- **Pingdom** (gratis tier)
- **StatusCake** (gratis tier)

Deze services kunnen periodiek je app "wakker houden".

---

### Oplossing 3: Controleer of App Service bestaat

**Belangrijk:** Controleer eerst of de App Service überhaupt bestaat:

1. **Azure Portal** → **App Services**
2. **Zoek naar:** `app-carrecommender-web-dev2`
3. **Als deze NIET bestaat:**
   - Maak een nieuwe App Service aan
   - Deploy je frontend project
   - Gebruik de URL die Azure geeft

---

### Oplossing 4: Gebruik een Custom Domain (Optioneel)

Als je een eigen domein hebt, kan dit helpen met DNS caching. Maar dit is niet nodig voor de basis functionaliteit.

---

## 🎯 Aanbevolen Aanpak

### Voor Nu (Directe Test):

1. **Controleer Azure Portal:**
   - Ga naar https://portal.azure.com
   - App Services → Zoek naar `app-carrecommender-web-dev2`
   - **Als deze bestaat:** Noteer de exacte URL
   - **Als deze NIET bestaat:** Maak een nieuwe aan

2. **Test de URL:**
   - Open de URL in je browser
   - **Wacht 30-60 seconden** (voor cold start)
   - Ververs de pagina (F5)
   - Als het nog steeds niet werkt: zie volgende stappen

3. **Als App Service niet bestaat:**
   - Maak nieuwe App Service aan
   - Deploy `frontend/CarRecommender.Web`
   - Gebruik de nieuwe URL

---

## 📋 Checklist (Zonder Always On)

- [ ] App Service bestaat in Azure Portal
- [ ] App Service status is "Running"
- [ ] Frontend project is gedeployed
- [ ] Wacht 30-60 seconden na eerste request (cold start)
- [ ] Ververs pagina als het niet werkt
- [ ] Overweeg wake-up service voor productie gebruik

---

## 💡 Tips voor Gratis Tier

1. **Accepteer cold start:** Eerste request kan 10-30 seconden duren
2. **Gebruik wake-up service:** Houd app actief met periodieke requests
3. **Test regelmatig:** App blijft actief als er regelmatig requests zijn
4. **Overweeg upgrade:** Voor productie gebruik is betaalde tier aanbevolen

---

## 🚀 Voor Docenten

**Als je de URL deelt met docenten:**

1. **Leg uit:** Eerste keer kan even duren (10-30 seconden)
2. **Of:** Gebruik een wake-up service om app actief te houden
3. **Of:** Vraag docenten om de link regelmatig te gebruiken (houdt app actief)

---

**Status:** ✅ Oplossingen voor Gratis Tier
**Datum:** $(date)

