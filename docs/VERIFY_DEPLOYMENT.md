# Deployment Verificatie Guide

## ⚠️ Belangrijk: 404 op Deployment Logs betekent NIET altijd dat deployment faalt

Als je een 404 error ziet op de deployment log URL (`/api/deployments/temp-XXX/log`), betekent dit **niet automatisch** dat de deployment gefaald is. Dit is een bekend Kudu probleem waarbij de log file niet gevonden kan worden, maar de deployment zelf mogelijk wel succesvol was.

## ✅ Stap 1: Test of de Applicatie Werkt

### Test de Health Endpoint

Open in je browser:
```
https://app-carrecommender-dev.azurewebsites.net/api/health
```

**Verwacht resultaat:**
```json
{
  "status": "OK"
}
```

### ✅ Als je `{"status": "OK"}` ziet:
- **Deployment is succesvol!** ✅
- De applicatie draait correct
- Je kunt de 404 error op de deployment logs negeren

### ❌ Als je een error ziet:
- Controleer de volgende stappen

---

## ✅ Stap 2: Test andere Endpoints

### Test Cars Endpoint:
```
https://app-carrecommender-dev.azurewebsites.net/api/cars?page=1&pageSize=5
```

**Verwacht:** JSON array met auto's

### Test Recommendations Endpoint:
```
https://app-carrecommender-dev.azurewebsites.net/api/recommendations/1?top=5
```

**Verwacht:** JSON array met recommendations

---

## ✅ Stap 3: Check Azure Portal

### Via Azure Portal:

1. Ga naar [Azure Portal](https://portal.azure.com)
2. Zoek je App Service: `app-carrecommender-dev`
3. Check de volgende dingen:

#### A. Status
- **Status** moet "Running" zijn (groen)
- Als "Stopped": Klik op "Start"

#### B. Deployment Center
- Ga naar "Deployment Center"
- Check of er een recente deployment is
- Status moet "Success" of "Active" zijn

#### C. Log Stream
- Ga naar "Log stream" (in het menu links)
- Je zou real-time logs moeten zien
- Als je logs ziet: applicatie draait!

#### D. Application Logs
- Ga naar "Log stream" → "Application Logs"
- Check voor errors

---

## ✅ Stap 4: Check via Kudu

### Via Kudu Console:

1. Ga naar: `https://app-carrecommender-dev.scm.azurewebsites.net`
2. Klik op "Debug console" → "CMD"
3. Navigeer naar: `site/wwwroot`
4. Check of bestanden aanwezig zijn:
   ```
   dir
   ```
5. Zoek naar:
   - `CarRecommender.Api.dll` (moet bestaan)
   - `web.config` (moet bestaan)
   - `data/` folder (moet bestaan)
   - `appsettings.json` (moet bestaan)

### Als bestanden ontbreken:
- Deployment is mogelijk niet compleet
- Probeer opnieuw te deployen

---

## 🔍 Stap 5: Check Application Logs

### Via Azure Portal:

1. App Service → "Log stream"
2. Bekijk real-time logs
3. Zoek naar:
   - Startup messages
   - Errors
   - "Application started" messages

### Via Kudu:

1. `https://app-carrecommender-dev.scm.azurewebsites.net`
2. "Debug console" → "CMD"
3. Navigeer naar: `LogFiles\Application`
4. Bekijk recente log files:
   ```
   dir /O-D
   type stdout_*.log
   ```

---

## 🐛 Troubleshooting

### Probleem: Health endpoint geeft 404

**Mogelijke oorzaken:**
1. Applicatie is niet gestart
2. Verkeerde URL
3. Routes zijn niet correct geconfigureerd

**Oplossingen:**
1. Check Azure Portal → App Service → Status = "Running"
2. Herstart de App Service
3. Check `web.config` bestaat en is correct

### Probleem: Health endpoint geeft 500

**Mogelijke oorzaken:**
1. Applicatie crash bij startup
2. Missing dependencies
3. Data file niet gevonden

**Oplossingen:**
1. Check Application Logs (zie Stap 5)
2. Verifieer dat `data/Cleaned_Car_Data_For_App_Fully_Enriched.csv` bestaat
3. Check `appsettings.json` configuratie

### Probleem: Geen bestanden in wwwroot

**Oplossing:**
- Deployment is gefaald
- Probeer opnieuw te deployen via GitHub Actions
- Of deploy handmatig via Kudu (zie troubleshooting guide)

---

## ✅ Deployment Succes Checklist

- [ ] Health endpoint werkt: `/api/health` geeft `{"status": "OK"}`
- [ ] Cars endpoint werkt: `/api/cars` geeft JSON array
- [ ] App Service status is "Running" in Azure Portal
- [ ] Bestanden zijn aanwezig in `site/wwwroot` (via Kudu)
- [ ] Application logs tonen geen errors
- [ ] Data file bestaat: `site/wwwroot/data/Cleaned_Car_Data_For_App_Fully_Enriched.csv`

---

## 💡 Belangrijk

**De 404 error op deployment logs kan je negeren als:**
- ✅ Health endpoint werkt
- ✅ Andere endpoints werken
- ✅ App Service status is "Running"
- ✅ Bestanden zijn aanwezig in wwwroot

Dit is een bekend Kudu probleem en betekent niet dat je deployment gefaald is.

---

## 📞 Als Niets Werkt

1. Check GitHub Actions logs voor deployment errors
2. Check Azure Portal → App Service → Logs
3. Check Kudu → LogFiles → Application
4. Probeer handmatige deployment (zie AZURE_DEPLOYMENT_TROUBLESHOOTING.md)

