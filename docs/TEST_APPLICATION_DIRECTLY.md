# Test Applicatie Direct - Niet Deployment Logs!

## ⚠️ Belangrijk: Je Test Nu de Verkeerde URL!

De 404 error die je ziet is op:
```
/api/deployments/temp-b4c48ce7/log
```

**Dit is de deployment log URL, NIET je applicatie!**

---

## ✅ Test de Echte Applicatie URLs

### Test 1: Health Endpoint (BELANGRIJK!)

Open deze URL in je browser:

```
https://app-carrecommender-dev.azurewebsites.net/api/health
```

**NIET:**
- ❌ `https://app-carrecommender-dev.azurewebsites.net/api/deployments/...` (deployment logs)
- ❌ `https://app-carrecommender-dev-h0agdkbfbcb3fxg7.westeurope-01.azurewebsites.net` (oude URL?)

**WEL:**
- ✅ `https://app-carrecommender-dev.azurewebsites.net/api/health`

**Wat zie je?**
- `{"status": "OK"}` = ✅ Werkt!
- 404 = ❌ Routes werken niet
- 500 = ❌ Applicatie crasht
- Timeout = ❌ Applicatie start niet

### Test 2: Root URL

```
https://app-carrecommender-dev.azurewebsites.net/
```

**Wat zie je?**
- JSON response = ✅ API werkt
- 404 = ❌ Routes niet geconfigureerd
- Error = ❌ Applicatie probleem

### Test 3: Cars Endpoint

```
https://app-carrecommender-dev.azurewebsites.net/api/cars?page=1&pageSize=5
```

**Verwacht:** JSON array met auto's

---

## 🔍 Test Handmatig in Kudu (KRITIEK!)

**In Kudu Console (wwwroot directory):**

```cmd
cd site\wwwroot
dotnet CarRecommender.Api.dll
```

**Wat gebeurt er?**

### Als het werkt:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

✅ **Applicatie kan starten!** Het probleem is IIS/routing.

### Als het crasht:
Je ziet een error zoals:
```
Unhandled exception. System.IO.FileNotFoundException: ...
```

❌ **Deel deze exacte error!**

**Stop met Ctrl+C** na testen.

---

## 🔍 Check Application Logs (Niet Deployment Logs!)

**Via Azure Portal:**

1. App Service → **"Log stream"** (in menu links)
2. **NIET** "Deployment Center" → "Logs"
3. Open in een andere tab: `https://app-carrecommender-dev.azurewebsites.net/api/health`
4. Bekijk de **Application Logs** (niet deployment logs!)

**Wat zie je in Application Logs?**
- Startup messages?
- Errors?
- Niets?

---

## 🔍 Check Routes Configuratie

De applicatie moet routes hebben voor `/api/health`. Check of controllers correct zijn geconfigureerd.

**Mogelijk probleem:** Routes werken niet omdat:
- Controllers niet gevonden worden
- Routing niet correct geconfigureerd
- web.config blokkeert routes

---

## 📋 Wat Ik Nodig Heb

Test deze URLs en deel wat je ziet:

1. **`https://app-carrecommender-dev.azurewebsites.net/api/health`**
   - Wat zie je? (JSON, 404, 500, timeout?)

2. **`dotnet CarRecommender.Api.dll` in Kudu**
   - Werkt het? (zie je "Now listening on...")
   - Of crasht het? (deel de error)

3. **Azure Portal → Log stream → Application Logs**
   - Wat zie je bij startup?

**De deployment log 404 kunnen we negeren - dat is niet relevant!**

---

## 💡 Belangrijk

**De 404 op `/api/deployments/...` is NIET je applicatie!**

Test de echte applicatie endpoints:
- `/api/health`
- `/api/cars`
- `/`

**Deel wat je ziet bij deze URLs!**

