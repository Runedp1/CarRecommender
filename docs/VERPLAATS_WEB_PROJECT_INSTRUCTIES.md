# Instructies: CarRecommender.Web Verplaatsen

## ⚠️ Belangrijk: Sluit Visual Studio Eerst!

De folder is momenteel in gebruik. Volg deze stappen:

## 📋 Stappenplan

### Stap 1: Sluit Visual Studio
- Sluit Visual Studio volledig
- Sluit ook Visual Studio Code als die open is
- Sluit alle andere editors die de solution open hebben

### Stap 2: Verplaats de Folder

**Optie A: Via File Explorer (Aanbevolen)**
1. Open File Explorer
2. Ga naar: `C:\Users\runed\OneDrive - Thomas More\Recommendation System\CarRecommender.Api\`
3. **Rechtsklik** op de folder `CarRecommender.Web`
4. Kies **"Cut"** of **"Knippen"** (Ctrl+X)
5. Ga terug naar: `C:\Users\runed\OneDrive - Thomas More\Recommendation System\`
6. **Rechtsklik** in de lege ruimte
7. Kies **"Paste"** of **"Plakken"** (Ctrl+V)

**Optie B: Via PowerShell (Als File Explorer niet werkt)**
```powershell
cd "C:\Users\runed\OneDrive - Thomas More\Recommendation System"
Move-Item -Path "CarRecommender.Api\CarRecommender.Web" -Destination "CarRecommender.Web" -Force
```

### Stap 3: Open Visual Studio Opnieuw
- Open de solution: `CarRecommender.sln`
- Visual Studio zal automatisch de nieuwe locatie detecteren (solution file is al aangepast)

### Stap 4: Verifieer
- Controleer dat `CarRecommender.Web` nu op root level staat
- Controleer dat beide projecten nog steeds builden
- Test beide projecten lokaal

### Stap 5: Deploy CarRecommender.Web
- Rechtsklik op **`CarRecommender.Web`** (nu op root level)
- Kies **"Publish"**
- Selecteer **`app-carrecommender-web-dev2`**
- Deploy!

---

## ✅ Wat is Al Aangepast?

De volgende bestanden zijn al aangepast:
- ✅ `CarRecommender.sln` - Pad naar project is bijgewerkt
- ✅ `CarRecommender.Api/CarRecommender.Api.csproj` - Exclude verwijderd

Je hoeft alleen nog de folder te verplaatsen!

---

## 🎯 Resultaat

Na verplaatsing:

```
Recommendation System/
├── CarRecommender.Api/
│   ├── CarRecommender.Api.csproj
│   └── Program.cs
├── CarRecommender.Web/          ← Nu op root level!
│   ├── CarRecommender.Web.csproj
│   └── Program.cs
└── CarRecommender.sln
```

---

**Status:** ⏳ Wacht op verplaatsing van folder



