# Fix: Data File Ontbreekt in Kudu

## 🔴 Het Probleem

De `data/` folder in Kudu wwwroot is **leeg** - het CSV bestand ontbreekt!

**Gevolg:**
- ❌ CarRepository kan CSV niet vinden bij startup
- ❌ Applicatie crasht of werkt niet
- ❌ Geen auto's geladen = geen data

**Dit verklaart waarom je URLs niet werken!**

---

## ✅ Oplossing: Upload CSV Bestand naar Kudu

### Stap 1: Download CSV Bestand

Het CSV bestand staat in je lokale project:
```
backend/data/Cleaned_Car_Data_For_App_Fully_Enriched.csv
```

**Download dit bestand** naar je computer (of gebruik het direct).

### Stap 2: Upload via Kudu File Browser

1. **Ga naar Kudu:**
   - `https://app-carrecommender-dev.scm.azurewebsites.net`
   - Login met Azure credentials

2. **Navigeer naar data folder:**
   - Klik op **"Browse"** of **"File Manager"** (bovenaan)
   - Navigeer naar: `site/wwwroot/data`
   - OF klik op de `data` folder in de file browser

3. **Upload CSV bestand:**
   - **Sleep** het CSV bestand (`Cleaned_Car_Data_For_App_Fully_Enriched.csv`) naar het browser venster
   - Wacht tot upload klaar is

4. **Verifieer:**
   ```cmd
   cd data
   dir
   ```
   
   Je zou moeten zien:
   ```
   Cleaned_Car_Data_For_App_Fully_Enriched.csv
   ```

### Stap 3: Herstart App Service

**Via Azure Portal:**
1. Azure Portal → App Service → **"Restart"**
2. Wacht 30-60 seconden

### Stap 4: Test

```
https://app-carrecommender-dev.azurewebsites.net/api/health
```

**Verwacht:** `{"status": "OK"}` ✅

---

## 🔧 Alternatief: Upload via Kudu Console (PowerShell)

Als drag & drop niet werkt:

1. **Open Kudu Console:**
   - Kudu → **"Debug console"** → **"PowerShell"**

2. **Navigeer naar data folder:**
   ```powershell
   cd site\wwwroot\data
   ```

3. **Upload via PowerShell:**
   ```powershell
   # Je moet het bestand eerst uploaden via de file browser
   # OF gebruik een base64 encoded string (complex)
   # OF gebruik FTP (zie volgende sectie)
   ```

**Aanbevolen:** Gebruik de file browser (drag & drop) - veel makkelijker!

---

## 🔧 Alternatief: Via FTP

Als Kudu file browser niet werkt:

### Stap 1: Download FTP Credentials

1. Azure Portal → App Service → **"Deployment Center"**
2. Klik op **"FTP"** tab
3. Klik op **"FTPS credentials"**
4. Noteer:
   - **FTPS endpoint** (bijv. `ftp://waws-prod-xxx.ftp.azurewebsites.windows.net`)
   - **Username** (bijv. `app-carrecommender-dev\$app-carrecommender-dev`)
   - **Password**

### Stap 2: Upload via FTP Client

**Met Windows File Explorer:**
1. Open File Explorer
2. Typ in adresbalk: `ftp://waws-prod-xxx.ftp.azurewebsites.windows.net`
3. Login met credentials
4. Navigeer naar: `site/wwwroot/data`
5. Sleep CSV bestand naar deze folder

**Met FTP Client (FileZilla, WinSCP, etc.):**
1. Connect met FTP credentials
2. Navigeer naar `site/wwwroot/data`
3. Upload CSV bestand

---

## 🔧 Alternatief: Via Azure Portal Advanced Tools

1. Azure Portal → App Service → **"Advanced Tools"** → **"Go"** (opent Kudu)
2. Klik op **"Debug console"** → **"CMD"**
3. Navigeer naar: `site/wwwroot/data`
4. Gebruik de file browser bovenaan om CSV te uploaden

---

## ✅ Verificatie

Na upload, check in Kudu:

```cmd
cd site\wwwroot\data
dir
```

**Verwacht:**
```
Cleaned_Car_Data_For_App_Fully_Enriched.csv
```

**Als je dit ziet:** ✅ Data file is geüpload!

---

## 🚀 Test Applicatie

Na upload en herstart:

1. **Test health endpoint:**
   ```
   https://app-carrecommender-dev.azurewebsites.net/api/health
   ```
   Verwacht: `{"status": "OK"}`

2. **Test cars endpoint:**
   ```
   https://app-carrecommender-dev.azurewebsites.net/api/cars?page=1&pageSize=5
   ```
   Verwacht: JSON array met auto's

---

## 🔍 Waarom Ontbrak het Bestand?

Het CSV bestand wordt mogelijk niet meegenomen tijdens GitHub Actions deployment omdat:

1. **csproj configuratie:** Check of `Content Include` correct is
2. **Publish output:** Het bestand wordt mogelijk niet gekopieerd naar publish folder
3. **ZIP structuur:** Het bestand wordt mogelijk niet meegenomen in de ZIP

**Na fix:** Zorg dat toekomstige deployments het CSV bestand wel meenemen (zie volgende sectie).

---

## 🔧 Zorg dat Toekomstige Deployments Data File Meenemen

### Check csproj Configuratie

Het bestand `backend/CarRecommender.Api/CarRecommender.Api.csproj` moet bevatten:

```xml
<ItemGroup>
  <Content Include="..\data\Cleaned_Car_Data_For_App_Fully_Enriched.csv">
    <Link>data\Cleaned_Car_Data_For_App_Fully_Enriched.csv</Link>
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>
</ItemGroup>
```

**Als dit al bestaat:** Het probleem is dat het bestand niet in de publish output komt.

**Fix:** Zorg dat de publish output de data folder bevat.

---

## 📋 Checklist

- [ ] CSV bestand gedownload van lokale project
- [ ] CSV bestand geüpload naar Kudu `data/` folder
- [ ] Verificatie: `dir data` toont CSV bestand
- [ ] App Service herstart
- [ ] Health endpoint getest en werkt
- [ ] Cars endpoint getest en geeft data terug

---

## 💡 Belangrijk

**Na deze fix zou je applicatie moeten werken!**

Het CSV bestand is essentieel - zonder dit kan de applicatie geen auto's laden en werkt niets.

**Upload het CSV bestand en test opnieuw!**










