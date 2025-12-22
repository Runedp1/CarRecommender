# Testing Guide - Car Recommender

## 🔗 URLs

### Backend API
- **URL:** `https://app-carrecommender-dev-h0agdkbfbcb3fxg7.westeurope-01.azurewebsites.net`
- **Type:** JSON API
- **Azure Web App:** `app-carrecommender-dev`

### Frontend Website
- **URL:** `https://app-carrecommender-web-dev2.azurewebsites.net`
- **Type:** Website met UI
- **Azure Web App:** `app-carrecommender-web-dev2`

---

## ✅ Test 1: Backend API

### Stap 1: Open de API URL
Open in je browser: `https://app-carrecommender-dev-h0agdkbfbcb3fxg7.westeurope-01.azurewebsites.net`

### Verwacht Resultaat:
Je zou een **JSON response** moeten zien:
```json
{
  "welkom": "Welkom bij de Car Recommender API!",
  "beschrijving": "Deze API helpt je bij het vinden van auto's op basis van je voorkeuren.",
  "endpoints": {
    "health": "https://app-carrecommender-dev-h0agdkbfbcb3fxg7.westeurope-01.azurewebsites.net/api/health",
    "cars": "https://app-carrecommender-dev-h0agdkbfbcb3fxg7.westeurope-01.azurewebsites.net/api/cars",
    ...
  }
}
```

### ✅ Als je JSON ziet:
- Backend API werkt correct! ✅

### ❌ Als je een fout ziet:
- Controleer of de deployment succesvol was
- Controleer Azure Portal voor errors

---

## ✅ Test 2: Frontend Website

### Stap 1: Open de Frontend URL
Open in je browser: `https://app-carrecommender-web-dev2.azurewebsites.net`

### Verwacht Resultaat:
Je zou een **HTML website** moeten zien met:
- Een zoekbalk bovenaan
- Mogelijkheid om auto's te zoeken
- Lijst van auto's
- Recommendations functionaliteit
- **GEEN JSON!**

### ✅ Als je een website ziet:
- Frontend werkt correct! ✅
- Test de zoekfunctionaliteit:
  1. Typ iets in de zoekbalk (bijv. "elektrische auto onder 30000")
  2. Klik op zoeken
  3. Je zou recommendations moeten zien

### ❌ Als je JSON ziet in plaats van website:
- Je hebt waarschijnlijk het verkeerde project gedeployed
- Deploy `frontend/CarRecommender.Web` naar `app-carrecommender-web-dev2`

### ❌ Als je een fout ziet:
- Controleer of de deployment succesvol was
- Controleer Azure Portal voor errors
- Controleer of de API URL correct is geconfigureerd

---

## 🔍 Test 3: API Endpoints (Optioneel)

### Test Health Endpoint:
```
https://app-carrecommender-dev-h0agdkbfbcb3fxg7.westeurope-01.azurewebsites.net/api/health
```

**Verwacht:** `{ "status": "healthy" }`

### Test Cars Endpoint:
```
https://app-carrecommender-dev-h0agdkbfbcb3fxg7.westeurope-01.azurewebsites.net/api/cars
```

**Verwacht:** JSON array met auto's

---

## 🎯 Voor Docenten

**Deel deze URL met je docenten:**
```
https://app-carrecommender-web-dev2.azurewebsites.net
```

Ze kunnen:
1. De URL openen in hun browser
2. Auto's zoeken via de zoekbalk
3. Recommendations bekijken
4. Geen technische kennis nodig!

---

## 📋 Checklist

- [ ] Backend API toont JSON op root URL
- [ ] Frontend website toont HTML (niet JSON)
- [ ] Zoekfunctionaliteit werkt op frontend
- [ ] Frontend kan API calls maken (test door te zoeken)
- [ ] Geen errors in browser console (F12 → Console)

---

## 🐛 Troubleshooting

### Frontend toont JSON in plaats van website:
- **Oplossing:** Deploy `frontend/CarRecommender.Web` naar `app-carrecommender-web-dev2`

### Frontend kan geen API calls maken:
- **Controleer:** `frontend/CarRecommender.Web/appsettings.json` heeft correcte API URL
- **Controleer:** Browser console (F12) voor CORS errors

### Beide URLs geven een fout:
- **Controleer:** Azure Portal → App Services → Logs
- **Controleer:** Of de deployments succesvol waren

---

**Laatste update:** $(date)

