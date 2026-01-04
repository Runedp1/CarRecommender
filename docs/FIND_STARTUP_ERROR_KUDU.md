# Vind Startup Error via Kudu - 500.37 Error

## 🔴 Probleem
Backend geeft **500.37 error (0x8007023e)** - applicatie kan niet starten binnen startup time limit.

## ✅ Stap 1: Test Handmatig Starten (KRITIEK!)

Dit vertelt je precies wat er mis is:

**In Kudu Console:**
```cmd
cd C:\home\site\wwwroot
dotnet CarRecommender.Api.dll
```

**Wacht 10-30 seconden en kijk naar de output.**

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
❌ **Deel deze EXACTE error!** Dit is de root cause.

**Stop met Ctrl+C** na testen.

## ✅ Stap 2: Check Stdout Logs (Als Ze Bestaan)

Stdout logs kunnen op verschillende locaties staan:

**Optie A: In wwwroot/logs**
```cmd
cd C:\home\site\wwwroot\logs
dir
type stdout_*.log
```

**Optie B: In LogFiles/Application**
```cmd
cd C:\home\LogFiles\Application
dir /S stdout*.log
```

**Optie C: Zoek overal**
```cmd
cd C:\home
dir /S stdout*.log
```

## ✅ Stap 3: Check Data Files

```cmd
cd C:\home\site\wwwroot\data
dir
```

**Verwacht:**
- `df_master_v8_def.csv` (of vergelijkbaar CSV bestand)

**Als ontbreekt:**
- ❌ Dit kan ervoor zorgen dat de applicatie crasht bij startup
- Upload het CSV bestand via Kudu File Manager

## ✅ Stap 4: Check Bestanden in wwwroot

```cmd
cd C:\home\site\wwwroot
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

## ✅ Stap 5: Check .NET Runtime

```cmd
dotnet --version
```

**Verwacht:** `8.0.x` of hoger

**Als verkeerde versie:**
- Azure Portal → Configuration → .NET Version = 8.0
- Save en Restart

## ✅ Stap 6: Check web.config

```cmd
cd C:\home\site\wwwroot
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

## ✅ Stap 7: Check Dependencies

```cmd
cd C:\home\site\wwwroot
dir *.dll
```

**Verwacht:**
- `CarRecommender.Api.dll`
- `CarRecommender.Core.dll`
- `Microsoft.*.dll` (verschillende Microsoft dependencies)
- `System.*.dll` (verschillende System dependencies)

**Als veel DLLs ontbreken:**
- De deployment was niet compleet
- Deploy opnieuw

## 📋 Checklist

- [ ] **Handmatig starten getest** (`dotnet CarRecommender.Api.dll`) - **BELANGRIJKSTE!**
- [ ] Stdout logs gezocht op verschillende locaties
- [ ] Data files gecontroleerd (`data/` folder)
- [ ] Bestanden in wwwroot gecontroleerd
- [ ] .NET versie gecontroleerd (`dotnet --version`)
- [ ] web.config gecontroleerd
- [ ] Dependencies gecontroleerd (DLLs)

## 💡 Belangrijk

**Stap 1 (handmatig starten) is het belangrijkst!** Dit vertelt je precies wat er mis is. Deel de volledige output van deze test.

## 🚀 Volgende Stap

**Test handmatig starten:**
```cmd
cd C:\home\site\wwwroot
dotnet CarRecommender.Api.dll
```

**Deel de volledige output!** Dit is de sleutel om het probleem op te lossen.


