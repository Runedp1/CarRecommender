# Azure Troubleshooting - DNS_PROBE_FINISHED_NXDOMAIN

## 🔍 Probleem

Je krijgt de error: `DNS_PROBE_FINISHED_NXDOMAIN` bij het openen van:
`https://app-carrecommender-web-dev2.azurewebsites.net`

Dit betekent dat het domein niet gevonden kan worden.

---

## 🔧 Mogelijke Oorzaken

### 1. App Service bestaat niet of is verwijderd
- De App Service `app-carrecommender-web-dev2` bestaat mogelijk niet meer
- Of heeft een andere naam gekregen

### 2. App Service is gestopt
- De App Service kan gestopt zijn
- Controleer Azure Portal → App Services

### 3. "Always On" is uitgeschakeld
- Als Always On uit staat, gaat de App Service in slaapstand na inactiviteit
- Dit kan DNS problemen veroorzaken

### 4. Verkeerde App Service naam
- De App Service heeft mogelijk een andere naam

---

## ✅ Oplossingen

### Stap 1: Controleer Azure Portal

1. **Ga naar Azure Portal:** https://portal.azure.com
2. **Zoek naar "App Services"**
3. **Controleer welke App Services je hebt:**
   - `app-carrecommender-dev` (Backend API)
   - `app-carrecommender-web-dev2` (Frontend - als deze bestaat)
   - Of een andere naam?

### Stap 2: Controleer App Service Status

Voor elke App Service:
1. Klik op de App Service naam
2. Controleer de **Status** (moet "Running" zijn)
3. Als gestopt: Klik op **"Start"**

### Stap 3: Controleer "Always On" Instelling

1. In de App Service → **Configuration** → **General settings**
2. Zoek naar **"Always On"**
3. **Zet dit op "On"** (aanbevolen voor productie)
4. Klik **"Save"**

**Waarom Always On?**
- Zorgt dat de app altijd draait (ook na inactiviteit)
- Voorkomt "cold start" delays
- Kan DNS problemen voorkomen

### Stap 4: Controleer de Juiste URL

De URL zou moeten zijn:
```
https://[app-service-name].azurewebsites.net
```

**Voorbeelden:**
- `https://app-carrecommender-web-dev2.azurewebsites.net`
- `https://app-carrecommender-web-dev.azurewebsites.net`
- Of een andere naam die je hebt gekozen

---

## 🆕 Als de App Service niet bestaat

### Optie 1: Maak een Nieuwe App Service

1. **Azure Portal** → **App Services** → **"Create"**
2. Vul in:
   - **Name:** `app-carrecommender-web-dev2` (of een andere unieke naam)
   - **Resource Group:** Zelfde als je API (bijv. `carrecommender-dev-rg`)
   - **App Service Plan:** Zelfde als je API (of maak nieuwe)
   - **Region:** `West Europe` (zelfde als API)
3. Klik **"Create"**
4. Wacht tot deployment klaar is
5. Deploy je frontend project naar deze nieuwe App Service

### Optie 2: Gebruik Bestaande App Service

Als je al een App Service hebt met een andere naam:
1. Noteer de exacte naam
2. Deploy `frontend/CarRecommender.Web` naar die App Service
3. Gebruik de URL van die App Service

---

## 📋 Checklist

- [ ] App Service bestaat in Azure Portal
- [ ] App Service status is "Running"
- [ ] Always On is ingeschakeld
- [ ] De URL klopt (geen typfouten)
- [ ] Frontend project is gedeployed naar de juiste App Service

---

## 🎯 Snelle Test

### Test 1: Controleer of App Service bestaat
```powershell
# Via Azure CLI (als geïnstalleerd)
az webapp list --query "[].{Name:name, State:state}" --output table
```

### Test 2: Controleer Backend API (werkt die wel?)
Open: `https://app-carrecommender-dev-h0agdkbfbcb3fxg7.westeurope-01.azurewebsites.net`

Als deze WEL werkt:
- Backend is OK
- Frontend App Service bestaat mogelijk niet of heeft andere naam

---

## 💡 Aanbeveling

1. **Controleer eerst Azure Portal** om te zien welke App Services je hebt
2. **Noteer de exacte naam** van je frontend App Service
3. **Zet Always On aan** voor beide App Services
4. **Deploy opnieuw** als nodig

---

**Status:** ⚠️ Vereist Azure Portal Check
**Datum:** $(date)

