# Azure Blob Trace Listener Error Fix

## 🔴 Probleem
```
System.ApplicationException: The trace listener AzureBlobTraceListener is disabled.
System.InvalidOperationException: The SAS URL for the cloud storage account is not specified.
Use the environment variable 'DIAGNOSTICS_AZUREBLOBCONTAINERSASURL' to define it.
```

## ✅ Oplossing

Deze error is **niet kritiek** - het is alleen een logging configuratie issue. De applicatie zou nog steeds moeten werken.

**Maar:** Als de applicatie crasht, is dit waarschijnlijk **niet** de oorzaak. Er is waarschijnlijk een andere error.

## 🔍 Stap 1: Check voor Andere Errors

Deze error is waarschijnlijk niet de root cause. Check voor andere errors in de logs:

**In Kudu Console:**
```cmd
cd C:\home\LogFiles\Application
type 0f4d98-2996-logging-errors.txt
```

Zoek naar:
- `Unhandled exception`
- `System.IO.FileNotFoundException`
- `System.InvalidOperationException` (andere dan de blob trace listener)
- `Could not find file`
- `Missing dependency`

## 🔍 Stap 2: Test Handmatig Starten

Dit vertelt je of de applicatie kan starten:

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
Je ziet een andere error - deel deze exacte error!

## 🔍 Stap 3: Fix Azure Blob Trace Listener (Optioneel)

Als je de blob trace listener error wilt fixen (niet verplicht):

**Via Azure Portal:**
1. **App Service** → **Configuration** → **Application settings**
2. Voeg nieuwe setting toe:
   - **Name:** `DIAGNOSTICS_AZUREBLOBCONTAINERSASURL`
   - **Value:** (laat leeg of verwijder de setting)
3. **OF:** Verwijder de setting als deze bestaat
4. Klik **Save**
5. Herstart App Service

**Of via Kudu (Environment Variables):**
```cmd
set DIAGNOSTICS_AZUREBLOBCONTAINERSASURL=
```

**Maar:** Deze error zou de applicatie niet moeten crashen. Als de applicatie crasht, is er een andere oorzaak.

## 🔍 Stap 4: Check Data Files

```cmd
cd C:\home\site\wwwroot\data
dir
```

**Verwacht:**
- `df_master_v8_def.csv` (of vergelijkbaar CSV bestand)

**Als ontbreekt:** Dit kan ervoor zorgen dat de applicatie crasht bij startup.

## 🔍 Stap 5: Check web.config

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

## 💡 Belangrijk

**De Azure Blob Trace Listener error is NIET de oorzaak van de crash.** Dit is alleen een logging configuratie warning.

**De echte error staat waarschijnlijk:**
- In dezelfde `logging-errors.txt` file (verder naar beneden)
- Of in stdout logs (als die bestaan)
- Of verschijnt bij handmatig starten (`dotnet CarRecommender.Api.dll`)

## 📋 Checklist

- [ ] Volledige `logging-errors.txt` bestand bekeken (niet alleen eerste error)
- [ ] Handmatig starten getest (`dotnet CarRecommender.Api.dll`)
- [ ] Data files gecontroleerd
- [ ] web.config gecontroleerd
- [ ] Andere errors gezocht in logs

## 🚀 Volgende Stap

**Test handmatig starten (Stap 2)** - dit vertelt je precies wat er mis is. Deel de volledige output!


