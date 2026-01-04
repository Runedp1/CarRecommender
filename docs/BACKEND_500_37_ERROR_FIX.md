# Backend 500.37 Error Fix - Error Code 0x8007023e

## 🔴 Probleem
Backend geeft **HTTP Error 500.37** met error code **0x8007023e**. Dit betekent dat de ASP.NET Core applicatie niet kan starten binnen de startup time limit.

## 🔍 Diagnose Stappen

### Stap 1: Check Stdout Logs (BELANGRIJK!)

De stdout logs zijn enabled in `web.config`. Deze bevatten de exacte startup error.

**Via Kudu:**
1. Ga naar: `https://app-carrecommender-dev.scm.azurewebsites.net`
2. **Debug console** → **CMD**
3. Navigeer naar logs:
   ```cmd
   cd LogFiles
   dir
   ```
4. Check voor `Application` folder:
   ```cmd
   cd Application
   dir /O-D
   ```
5. Bekijk de meest recente stdout log:
   ```cmd
   type stdout_*.log
   ```
   (Of gebruik de nieuwste bestandsnaam)

**Via Azure Portal:**
1. **App Service** → **"Log stream"** (in menu links)
2. Bekijk real-time logs tijdens startup
3. Probeer de applicatie te starten (herstart App Service)
4. Je zou nu de exacte error moeten zien

### Stap 2: Test Handmatig Starten (KRITIEK!)

Dit vertelt je precies wat er mis is:

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
✅ **Applicatie kan starten!** Het probleem is IIS/AspNetCoreModule configuratie.

**Als het crasht:**
Je ziet een error zoals:
```
Unhandled exception. System.IO.FileNotFoundException: Could not find file '...'
```
of
```
System.InvalidOperationException: ...
```
❌ **Deel deze exacte error!** Dit is de root cause.

**Stop met Ctrl+C** na testen.

### Stap 3: Check .NET Version

**KRITIEK:** Azure moet op .NET 8.0 staan!

1. **Azure Portal** → **App Services** → **`app-carrecommender-dev`**
2. **Configuration** → **General settings**
3. Scroll naar **".NET Version"**
4. **Wat staat er?**
   - ✅ **`8.0`** of **`8.x`** = Goed
   - ❌ **`9.0`** = **PROBLEEM!** Wijzig naar **`8.0`** en klik **"Save"**
   - ❌ **Leeg** = **PROBLEEM!** Zet op **`8.0`** en klik **"Save"**
5. **Na wijziging:** Klik **"Save"** bovenaan
6. **Herstart App Service:**
   - Ga naar **"Overview"**
   - Klik **"Restart"**

### Stap 4: Check Data Files

**In Kudu Console:**
```cmd
cd site\wwwroot\data
dir
```

**Verwacht:**
- `df_master_v8_def.csv` (of vergelijkbaar CSV bestand)

**Als ontbreekt:**
- ❌ Dit kan ervoor zorgen dat de applicatie crasht bij startup
- Upload het CSV bestand naar `data/` folder via Kudu File Manager

### Stap 5: Check Bestanden in wwwroot

**In Kudu Console:**
```cmd
cd site\wwwroot
dir
```

**Verwacht deze bestanden:**
- ✅ `CarRecommender.Api.dll` - De applicatie
- ✅ `web.config` - IIS configuratie
- ✅ `appsettings.json` - Configuratie
- ✅ `CarRecommender.Core.dll` - Core library
- ✅ `data/` folder - Data directory

**Als iets ontbreekt:**
- De deployment was mogelijk niet compleet
- Deploy opnieuw via GitHub Actions

### Stap 6: Check web.config

**In Kudu Console:**
```cmd
cd site\wwwroot
type web.config
```

**Verwacht:**
```xml
<aspNetCore processPath="dotnet" 
            arguments=".\CarRecommender.Api.dll" 
            stdoutLogEnabled="true" 
            stdoutLogFile=".\logs\stdout" 
            hostingModel="inprocess"
            startupTimeLimit="600">
```

**Check:**
- ✅ `processPath="dotnet"` (niet `dotnet.exe`)
- ✅ `arguments=".\CarRecommender.Api.dll"` (correct DLL naam)
- ✅ `stdoutLogEnabled="true"` (logs aan)
- ✅ `startupTimeLimit="600"` (10 minuten - genoeg tijd)

## 🚀 Oplossingen

### Oplossing 1: Verhoog Startup Time Limit (Als Applicatie Langzaam Start)

Als de applicatie wel start maar te langzaam is:

**Update `web.config`:**
```xml
<aspNetCore processPath="dotnet" 
            arguments=".\CarRecommender.Api.dll" 
            stdoutLogEnabled="true" 
            stdoutLogFile=".\logs\stdout" 
            hostingModel="inprocess"
            startupTimeLimit="900">
```
(Verhoog naar 900 seconden = 15 minuten)

**Upload via Kudu:**
1. Kudu → **"Debug console"** → **"CMD"**
2. Navigeer naar `site/wwwroot`
3. Edit `web.config` via Kudu File Manager
4. Herstart App Service

### Oplossing 2: Fix .NET Version

Als .NET versie verkeerd is:
1. Azure Portal → Configuration → General settings
2. .NET Version = **8.0**
3. Save en Restart

### Oplossing 3: Upload Missing Data Files

Als data files ontbreken:
1. Kudu → File Manager
2. Navigeer naar `site/wwwroot/data`
3. Upload `df_master_v8_def.csv` (of het juiste CSV bestand)
4. Herstart App Service

### Oplossing 4: Check Application Logs voor Specifieke Errors

De stdout logs bevatten de exacte error. Volg **Stap 1** hierboven om de logs te bekijken.

## 📋 Checklist

- [ ] Stdout logs gecontroleerd (via Kudu of Azure Portal)
- [ ] Handmatig starten getest (via Kudu: `dotnet CarRecommender.Api.dll`)
- [ ] .NET versie gecontroleerd (moet 8.0 zijn)
- [ ] Data files aanwezig in `data/` folder
- [ ] Alle bestanden aanwezig in `wwwroot`
- [ ] `web.config` correct geconfigureerd
- [ ] App Service status = "Running"
- [ ] App Service herstart na wijzigingen

## 🔗 Handige Links

- **Kudu Console:** `https://app-carrecommender-dev.scm.azurewebsites.net`
- **Azure Portal:** `https://portal.azure.com`
- **Backend URL:** `https://app-carrecommender-dev-dxfgd4csg4ekaxgs.francecentral-01.azurewebsites.net`

## 💡 Belangrijk

**De stdout logs zijn de sleutel!** Deze bevatten de exacte error die de applicatie crash veroorzaakt. Volg **Stap 1** om deze logs te bekijken en deel de error met mij.


