# Handmatige Deployment via Kudu - Stap voor Stap

## ✅ Voordelen van Handmatige Deployment
- Directe controle over wat er geüpload wordt
- Geen dependency op GitHub Actions deployment issues
- Sneller om te testen
- Werkt altijd, zelfs als automatische deployment faalt

---

## 📋 Stap 1: Download de Artifact van GitHub Actions

### Optie A: Via GitHub Web Interface

1. Ga naar je GitHub repository
2. Klik op **"Actions"** tab
3. Selecteer je laatste workflow run (die waar build job slaagde)
4. Klik op **"build"** job
5. Scroll naar beneden naar **"Upload artifact for deployment job"**
6. Klik op **".net-app"** link om te downloaden
7. Pak de ZIP uit op je computer

### Optie B: Via GitHub CLI (als geïnstalleerd)

```bash
gh run download <run-id> -n .net-app
```

---

## 📋 Stap 2: Maak een Nieuwe ZIP met Correcte Structuur

**BELANGRIJK**: De ZIP moet de bestanden direct in de root hebben, niet in een subfolder!

### Stap 2.1: Navigeer naar de Publish Folder

Na het uitpakken van de artifact, ga naar de `publish` folder:

```powershell
# Voorbeeld pad (pas aan naar jouw situatie)
cd C:\Users\runed\Downloads\.net-app\publish
# OF
cd C:\Users\runed\Downloads\.net-app
```

### Stap 2.2: Verifieer Bestanden

Controleer dat deze bestanden bestaan:
- `CarRecommender.Api.dll` (of `CarRecommender.Web.dll` voor frontend)
- `web.config`
- `appsettings.json`
- `data/` folder (met CSV bestand)
- Alle andere DLL files

```powershell
# List alle bestanden
dir
```

### Stap 2.3: Maak Nieuwe ZIP

**BELANGRIJK**: Maak de ZIP vanuit de publish folder zelf, zodat bestanden direct in root zitten!

```powershell
# Zorg dat je IN de publish folder bent
cd publish  # Als je nog niet in publish folder bent

# Maak ZIP met alle bestanden direct in root
Compress-Archive -Path * -DestinationPath ..\deploy.zip -Force

# Verifieer ZIP
dir ..\deploy.zip
```

**Structuur moet zijn:**
```
deploy.zip
├── CarRecommender.Api.dll
├── web.config
├── appsettings.json
├── data/
│   └── Cleaned_Car_Data_For_App_Fully_Enriched.csv
├── images/
└── ... (andere bestanden)
```

**NIET:**
```
deploy.zip
└── publish/
    ├── CarRecommender.Api.dll
    └── ...
```

---

## 📋 Stap 3: Upload via Kudu

### Stap 3.1: Open Kudu Console

1. Ga naar: `https://app-carrecommender-dev.scm.azurewebsites.net`
   - Vervang `app-carrecommender-dev` met jouw App Service naam
2. Je wordt gevraagd om in te loggen (gebruik je Azure credentials)
3. Klik op **"Debug console"** → **"CMD"** (of "PowerShell")

### Stap 3.2: Navigeer naar wwwroot

In de Kudu console:

```cmd
cd site\wwwroot
dir
```

Je ziet de huidige bestanden in wwwroot.

### Stap 3.3: Upload de ZIP

**Methode 1: Drag & Drop (Aanbevolen)**

1. In de Kudu console, zie je een bestandsbrowser aan de bovenkant
2. Sleep je `deploy.zip` bestand naar het browser venster
3. Wacht tot upload klaar is

**Methode 2: Via Kudu File Manager**

1. Klik op **"Browse"** of **"File Manager"** in Kudu
2. Navigeer naar `site/wwwroot`
3. Klik op **"Upload"** of sleep de ZIP naar het venster

### Stap 3.4: Unzip de Bestanden

Terug in de CMD console:

```cmd
cd site\wwwroot

# Unzip (Windows heeft ingebouwde unzip in PowerShell)
powershell Expand-Archive -Path deploy.zip -DestinationPath . -Force

# OF gebruik 7-Zip als geïnstalleerd:
# "C:\Program Files\7-Zip\7z.exe" x deploy.zip -y
```

### Stap 3.5: Verifieer Bestanden

```cmd
dir
```

Je zou moeten zien:
- `CarRecommender.Api.dll`
- `web.config`
- `appsettings.json`
- `data/` folder
- Alle andere bestanden

### Stap 3.6: Verwijder de ZIP (Optioneel)

```cmd
del deploy.zip
```

---

## 📋 Stap 4: Herstart de App Service

### Via Azure Portal:

1. Ga naar [Azure Portal](https://portal.azure.com)
2. Zoek je App Service: `app-carrecommender-dev`
3. Klik op **"Restart"** (of **"Herstarten"**)
4. Wacht tot herstart klaar is (~30 seconden)

### Via Kudu:

```cmd
# In Kudu console
cd site
restart
```

---

## 📋 Stap 5: Test de Applicatie

### Test Health Endpoint:

Open in je browser:
```
https://app-carrecommender-dev.azurewebsites.net/api/health
```

**Verwacht:** `{"status": "OK"}`

### Test Cars Endpoint:

```
https://app-carrecommender-dev.azurewebsites.net/api/cars?page=1&pageSize=5
```

**Verwacht:** JSON array met auto's

---

## 🔧 Troubleshooting

### Probleem: ZIP upload faalt

**Oplossing:**
- Check ZIP file size (moet < 100MB zijn voor Kudu)
- Probeer kleinere ZIP of split in delen
- Check internet verbinding

### Probleem: Unzip faalt

**Oplossing:**
- Gebruik PowerShell: `Expand-Archive` in plaats van CMD unzip
- Check of ZIP niet corrupt is
- Probeer opnieuw te uploaden

### Probleem: Bestanden zijn er maar applicatie werkt niet

**Oplossing:**
1. Check `web.config` bestaat en is correct
2. Herstart App Service
3. Check Application Logs via Azure Portal → Log stream

### Probleem: Data file niet gevonden

**Oplossing:**
1. Verifieer dat `data/Cleaned_Car_Data_For_App_Fully_Enriched.csv` bestaat in wwwroot
2. Check `appsettings.json` → `DataSettings:DataDirectory` = `data`
3. Check pad in `appsettings.json` → `DataSettings:CsvFileName`

---

## 📝 Snelle Checklist

- [ ] Artifact gedownload van GitHub Actions
- [ ] ZIP gemaakt vanuit publish folder (bestanden in root)
- [ ] ZIP geüpload naar Kudu
- [ ] ZIP uitgepakt in `site/wwwroot`
- [ ] Bestanden geverifieerd (DLL, web.config, data folder)
- [ ] App Service herstart
- [ ] Health endpoint getest en werkt

---

## 💡 Tips

1. **Bewaar de ZIP**: Bewaar `deploy.zip` voor snelle her-deployment
2. **Backup maken**: Voor belangrijke deployments, maak eerst backup van huidige wwwroot
3. **Test lokaal eerst**: Test de publish output lokaal voordat je uploadt
4. **Check logs**: Bekijk altijd Application Logs na deployment

---

## 🚀 Voor Frontend (CarRecommender.Web)

Dezelfde stappen, maar:
- Download artifact van **Web workflow** (niet API workflow)
- Zoek naar `CarRecommender.Web.dll` in plaats van `CarRecommender.Api.dll`
- Upload naar: `pp-carrecommender-web-dev.scm.azurewebsites.net`
- Test: `https://pp-carrecommender-web-dev.azurewebsites.net/`

---

**Succes met de deployment!** 🎉


