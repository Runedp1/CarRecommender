# Backend 503 Service Unavailable Error Fix

## 🔴 Probleem
Backend geeft **HTTP Error 503 - Service Unavailable**. Dit betekent dat de service niet beschikbaar is.

## 🔍 Mogelijke Oorzaken

1. **App Service is gestopt** (meest waarschijnlijk)
2. **Applicatie crasht bij elke request**
3. **Deployment bezig** (tijdelijk)
4. **Resource constraints** (CPU/memory)
5. **Applicatie start maar crasht direct**

## ✅ Diagnose Stappen

### Stap 1: Check App Service Status (KRITIEK!)

**Via Azure Portal:**
1. **Azure Portal** → **App Services** → **`app-carrecommender-dev`**
2. Check **"Status"** bovenaan:
   - ✅ **"Running"** = Goed (ga naar Stap 2)
   - ❌ **"Stopped"** = **Klik "Start"** en wacht 30 seconden
   - ⚠️ **"Stopping"** of **"Starting"** = Wacht tot status "Running" is

**Dit is vaak de oorzaak!**

### Stap 2: Check Application Logs

**Via Azure Portal Log Stream:**
1. **App Service** → **"Log stream"** (in menu links)
2. Bekijk real-time logs
3. **Wat zie je?**
   - Startup messages?
   - Errors?
   - "Application started"?
   - Crashes?

**Via Kudu (stdout logs):**
1. Ga naar: `https://app-carrecommender-dev.scm.azurewebsites.net`
2. **Debug console** → **CMD**
3. Navigeer naar logs:
   ```cmd
   cd LogFiles\Application
   dir /O-D
   type stdout_*.log
   ```

### Stap 3: Check Recent Deployments

**Via Azure Portal:**
1. **App Service** → **"Deployment Center"** (in menu links)
2. Check of er een deployment bezig is
3. Als deployment bezig is: Wacht tot deze klaar is

### Stap 4: Check Resource Usage

**Via Azure Portal:**
1. **App Service** → **"Metrics"** (in menu links)
2. Check:
   - **CPU Percentage** - Is dit 100%?
   - **Memory Percentage** - Is dit 100%?
   - **HTTP Server Errors** - Zie je veel errors?

**Als resources 100% zijn:**
- App Service plan is mogelijk te klein
- Upgrade naar een hoger plan
- Of optimaliseer de applicatie

### Stap 5: Test Handmatig Starten (Als App Service Running Is)

**In Kudu Console:**
```cmd
cd site\wwwroot
dotnet CarRecommender.Api.dll
```

**Als het werkt:**
```
Now listening on: http://localhost:5000
Application started. Press Ctrl+C to shut down.
```
✅ **Applicatie kan starten!** Het probleem is mogelijk IIS configuratie of resource constraints.

**Als het crasht:**
Je ziet een error - deel deze exacte error!

**Stop met Ctrl+C** na testen.

## 🚀 Oplossingen

### Oplossing 1: Start App Service (Als Gestopt)

**Via Azure Portal:**
1. **App Service** → **"Overview"**
2. Klik **"Start"** (als status "Stopped" is)
3. Wacht 30-60 seconden
4. Test opnieuw: `https://app-carrecommender-dev-dxfgd4csg4ekaxgs.francecentral-01.azurewebsites.net/api/health`

### Oplossing 2: Herstart App Service

**Via Azure Portal:**
1. **App Service** → **"Overview"**
2. Klik **"Restart"**
3. Wacht 30-60 seconden
4. Test opnieuw

### Oplossing 3: Check Application Logs voor Crashes

Als de applicatie start maar crasht:
1. Bekijk de stdout logs (Stap 2)
2. Zoek naar exceptions of errors
3. Deel de error met mij voor specifieke fix

### Oplossing 4: Upgrade App Service Plan (Als Resources 100%)

Als CPU of Memory 100% is:
1. **Azure Portal** → **App Service Plan**
2. **"Scale up"** of **"Scale out"**
3. Kies een hoger plan
4. Wacht tot scaling klaar is
5. Test opnieuw

### Oplossing 5: Check Data Files

**In Kudu Console:**
```cmd
cd site\wwwroot\data
dir
```

**Verwacht:**
- `df_master_v8_def.csv` (of vergelijkbaar CSV bestand)

**Als ontbreekt:**
- Upload het CSV bestand via Kudu File Manager
- Herstart App Service

## 📋 Checklist

- [ ] App Service status = "Running"
- [ ] Geen deployment bezig
- [ ] Application logs gecontroleerd (via Log stream of Kudu)
- [ ] Resource usage gecontroleerd (CPU/Memory niet 100%)
- [ ] Handmatig starten getest (via Kudu)
- [ ] Data files aanwezig
- [ ] App Service herstart (als nodig)

## 🔗 Handige Links

- **Kudu Console:** `https://app-carrecommender-dev.scm.azurewebsites.net`
- **Azure Portal:** `https://portal.azure.com`
- **Backend URL:** `https://app-carrecommender-dev-dxfgd4csg4ekaxgs.francecentral-01.azurewebsites.net`
- **Backend Health:** `https://app-carrecommender-dev-dxfgd4csg4ekaxgs.francecentral-01.azurewebsites.net/api/health`

## 💡 Belangrijk

**Een 503 error betekent meestal dat de App Service gestopt is.** Check eerst de status in Azure Portal en start de service als deze gestopt is.


