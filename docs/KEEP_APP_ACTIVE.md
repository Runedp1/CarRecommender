# App Actief Houden - Gratis Tier

## ⚠️ Misverstand Opgelost

**20 minuten** = Tijd VOOR de app in slaapstand gaat (niet de opstarttijd!)

**Cold start** = 10-30 seconden om op te starten (niet 20 minuten!)

---

## ✅ Oplossingen om App Actief te Houden

### Oplossing 1: Gratis Wake-up Service (Aanbevolen)

**UptimeRobot** (volledig gratis):
1. Ga naar: https://uptimerobot.com
2. Maak gratis account aan
3. Klik "Add New Monitor"
4. Vul in:
   - **Monitor Type:** HTTP(s)
   - **Friendly Name:** Car Recommender Frontend
   - **URL:** `https://app-carrecommender-web-dev2.azurewebsites.net`
   - **Monitoring Interval:** 5 minutes (of 10 minutes)
5. Klik "Create Monitor"

**Resultaat:**
- UptimeRobot checkt je app elke 5-10 minuten
- Dit houdt de app actief (geen slaapstand)
- Volledig gratis
- Automatisch

---

### Oplossing 2: Azure Logic App (Gratis Tier)

Je kunt een gratis Azure Logic App maken die periodiek je app "wakker houdt":

1. **Azure Portal** → **Logic Apps** → **Create**
2. Maak een **Recurrence** trigger (elke 10 minuten)
3. Voeg een **HTTP** action toe die je app URL aanroept
4. Sla op en activeer

**Voordeel:** Alles binnen Azure, geen externe service nodig

---

### Oplossing 3: GitHub Actions (Gratis)

Als je GitHub gebruikt, kun je een GitHub Action maken die periodiek je app aanroept:

```yaml
name: Keep App Active
on:
  schedule:
    - cron: '*/10 * * * *'  # Elke 10 minuten
jobs:
  wake-up:
    runs-on: ubuntu-latest
    steps:
      - name: Wake up app
        run: curl https://app-carrecommender-web-dev2.azurewebsites.net
```

---

### Oplossing 4: Lokale Script (Als je PC aan staat)

Je kunt een PowerShell script maken dat periodiek je app aanroept:

```powershell
# Wake-up script
while ($true) {
    Invoke-WebRequest -Uri "https://app-carrecommender-web-dev2.azurewebsites.net" -UseBasicParsing
    Start-Sleep -Seconds 600  # Elke 10 minuten
}
```

**Nadeel:** Werkt alleen als je PC aan staat

---

## 🎯 Aanbevolen: UptimeRobot

**Waarom UptimeRobot?**
- ✅ Volledig gratis
- ✅ Automatisch
- ✅ Werkt 24/7
- ✅ Geen eigen server nodig
- ✅ Eenvoudig te configureren
- ✅ Houdt app actief (geen slaapstand)

**Setup tijd:** 5 minuten

---

## 📊 Hoe het Werkt

### Zonder Wake-up Service:
```
Tijd 0:00 → App actief
Tijd 0:20 → App gaat in slaapstand (na 20 min inactiviteit)
Tijd 0:21 → Docent opent URL → Moet 10-30 seconden wachten (cold start)
```

### Met Wake-up Service (elke 10 minuten):
```
Tijd 0:00 → App actief
Tijd 0:10 → Wake-up service roept app aan → App blijft actief
Tijd 0:20 → Wake-up service roept app aan → App blijft actief
Tijd 0:21 → Docent opent URL → Direct beschikbaar! ✅
```

---

## ✅ Stappenplan

### Stap 1: Setup UptimeRobot (5 minuten)

1. Ga naar: https://uptimerobot.com
2. Klik "Sign Up" (gratis)
3. Verifieer email
4. Klik "Add New Monitor"
5. Vul in:
   - **Monitor Type:** HTTP(s)
   - **Friendly Name:** Car Recommender Frontend
   - **URL:** `https://app-carrecommender-web-dev2.azurewebsites.net`
   - **Monitoring Interval:** 5 minutes
6. Klik "Create Monitor"

### Stap 2: Test

1. Wacht 5 minuten
2. Open je frontend URL
3. App zou direct moeten werken (geen wachttijd!)

---

## 💡 Alternatief: Betaalde Tier

Als je een betaalde Azure tier gebruikt:
- **Always On** is beschikbaar
- App blijft altijd actief
- Geen wake-up service nodig
- **Kosten:** ~€10-15/maand voor Basic tier

**Voor studenten:** Gratis tier + UptimeRobot is meestal voldoende!

---

## 📋 Checklist

- [ ] UptimeRobot account aangemaakt
- [ ] Monitor toegevoegd voor frontend URL
- [ ] Monitoring interval ingesteld (5-10 minuten)
- [ ] Test na 5-10 minuten of app direct werkt
- [ ] Deel URL met docenten

---

**Status:** ✅ Oplossing voor Gratis Tier
**Aanbevolen:** UptimeRobot (gratis, automatisch, 24/7)

