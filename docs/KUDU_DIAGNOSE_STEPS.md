# Kudu Diagnose Stappen - Backend 503 Error

## 🔍 Stap 1: Bekijk Logging Errors

Je hebt al `logging-errors.txt` bestanden gevonden. Bekijk deze:

**In Kudu Console:**
```cmd
cd LogFiles\Application
dir /O-D
type 0f4d98-2996-logging-errors.txt
```

Of bekijk het nieuwste bestand:
```cmd
type 0f4d98-7832-logging-errors.txt
```

**Deel de inhoud van deze bestanden!** Deze bevatten waarschijnlijk de startup errors.

## 🔍 Stap 2: Check Stdout Logs Locatie

Stdout logs kunnen op een andere locatie staan. Probeer:

```cmd
cd C:\home\LogFiles
dir /S stdout*.log
```

Of check in de wwwroot logs folder:
```cmd
cd C:\home\site\wwwroot\logs
dir
```

## 🔍 Stap 3: Test Handmatig Starten (BELANGRIJK!)

Dit vertelt je precies wat er mis is:

```cmd
cd C:\home\site\wwwroot
dotnet CarRecommender.Api.dll
```

**Als het werkt:**
```
Now listening on: http://localhost:5000
Application started. Press Ctrl+C to shut down.
```
✅ **Applicatie kan starten!** Het probleem is IIS configuratie.

**Als het crasht:**
Je ziet een error - deel deze exacte error!

**Stop met Ctrl+C** na testen.

## 🔍 Stap 4: Check Bestanden in wwwroot

```cmd
cd C:\home\site\wwwroot
dir
```

**Verwacht:**
- ✅ `CarRecommender.Api.dll`
- ✅ `web.config`
- ✅ `appsettings.json`
- ✅ `CarRecommender.Core.dll`
- ✅ `data/` folder

**Als iets ontbreekt:** De deployment was mogelijk niet compleet.

## 🔍 Stap 5: Check Data Files

```cmd
cd C:\home\site\wwwroot\data
dir
```

**Verwacht:**
- `df_master_v8_def.csv` (of vergelijkbaar CSV bestand)

**Als ontbreekt:** Dit kan ervoor zorgen dat de applicatie crasht bij startup.

## 🔍 Stap 6: Check web.config

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

## 🔍 Stap 7: Check .NET Runtime

```cmd
dotnet --version
```

**Verwacht:** `8.0.x` of hoger

**Als verkeerde versie:** Azure Portal → Configuration → .NET Version = 8.0

## 🔍 Stap 8: Check Event Logs

```cmd
cd C:\home\LogFiles
dir /S eventlog.xml
```

Of check Windows Event Logs:
```cmd
wevtutil qe Application /c:10 /rd:true /f:text
```

## 📋 Checklist

- [ ] Logging errors bestanden bekeken (`type 0f4d98-*.txt`)
- [ ] Stdout logs gezocht op verschillende locaties
- [ ] Handmatig starten getest (`dotnet CarRecommender.Api.dll`)
- [ ] Bestanden in wwwroot gecontroleerd
- [ ] Data files gecontroleerd
- [ ] web.config gecontroleerd
- [ ] .NET versie gecontroleerd (`dotnet --version`)

## 💡 Belangrijk

**Begin met Stap 1** - bekijk de `logging-errors.txt` bestanden. Deze bevatten waarschijnlijk de exacte error die de applicatie crash veroorzaakt.


