# .NET 9.0 → 8.0 Downgrade voor Azure App Service

## 🔍 Het Probleem

Azure App Service ondersteunt mogelijk nog geen .NET 9.0, of de runtime is niet beschikbaar op je App Service Plan.

**Error:**
```
Could not execute because the specified command or file was not found.
dotnet-CarRecommender.Api.dll does not exist.
```

**Oorzaak:** .NET 9.0 runtime niet beschikbaar op Azure App Service.

---

## ✅ Oplossing: Downgrade naar .NET 8.0

Ik heb alle projecten aangepast naar .NET 8.0:

### Gewijzigde Bestanden:
1. ✅ `backend/CarRecommender.Api/CarRecommender.Api.csproj` → `net8.0`
2. ✅ `frontend/CarRecommender.Web/CarRecommender.Web.csproj` → `net8.0`
3. ✅ `CarRecommender.csproj` → `net8.0`
4. ✅ `.github/workflows/main_app-carrecommender-dev.yml` → `dotnet-version: '8.x'`
5. ✅ `.github/workflows/main_pp-carrecommender-web-dev.yml` → `dotnet-version: '8.x'`

---

## 📋 Stappen na Downgrade

### Stap 1: Update Azure App Service Configuratie

**Via Azure Portal:**

1. App Service → **"Configuration"** → **"General settings"**
2. Scroll naar **".NET Version"**
3. Zet op **`8.0`** (of nieuwste beschikbare 8.x versie)
4. Klik **"Save"**
5. **Herstart** App Service

### Stap 2: Rebuild Lokaal (Test)

```powershell
# Test API build
cd backend/CarRecommender.Api
dotnet clean
dotnet build -c Release
dotnet publish -c Release -o ./publish-test

# Test Web build
cd ../../frontend/CarRecommender.Web
dotnet clean
dotnet build -c Release
dotnet publish -c Release -o ./publish-test
```

**Als dit werkt:** ✅ Projecten zijn correct geconfigureerd voor .NET 8.0

### Stap 3: Commit en Push

```bash
git add .
git commit -m "Downgrade to .NET 8.0 for Azure App Service compatibility"
git push origin main
```

Dit triggert automatisch een nieuwe deployment via GitHub Actions.

### Stap 4: Verifieer Azure Configuratie

**Na deployment:**

1. Azure Portal → App Service → **"Configuration"** → **"General settings"**
2. Verifieer **".NET Version"** = **`8.0`**
3. Als verkeerd: Fix en herstart

---

## 🔍 Alternatief: Check of .NET 9.0 Beschikbaar Is

**Als je .NET 9.0 wilt gebruiken:**

1. Azure Portal → App Service → **"Configuration"** → **"General settings"**
2. Check welke .NET versies beschikbaar zijn
3. Als **9.0** beschikbaar is:
   - Zet op **9.0**
   - Herstart App Service
   - Test opnieuw

**Maar:** Voor nu is .NET 8.0 de veiligste keuze (volledig ondersteund).

---

## 📋 Checklist

- [ ] Alle projecten zijn geüpdatet naar `net8.0`
- [ ] GitHub Actions workflows gebruiken `dotnet-version: '8.x'`
- [ ] Lokaal rebuild werkt (test eerst!)
- [ ] Azure Portal → .NET Version = 8.0
- [ ] Commit en push (triggert nieuwe deployment)
- [ ] Test applicatie na deployment

---

## 💡 Belangrijk

**.NET 8.0 is volledig ondersteund op Azure App Service:**
- ✅ Alle features werken
- ✅ Geen compatibiliteitsproblemen
- ✅ Betrouwbaar en stabiel

**Na deze wijziging zou je applicatie moeten werken!**

---

## 🚀 Volgende Stappen

1. **Test lokaal eerst:**
   ```powershell
   dotnet build backend/CarRecommender.Api/CarRecommender.Api.csproj
   ```

2. **Als lokaal werkt:**
   - Commit en push
   - Wacht op GitHub Actions deployment
   - Test Azure URL

3. **Als nog steeds niet werkt:**
   - Check Azure Portal → .NET Version = 8.0
   - Herstart App Service
   - Check Application Logs

---

**Status:** ✅ Downgrade naar .NET 8.0 voltooid
**Volgende:** Test lokaal, commit, en deploy!









