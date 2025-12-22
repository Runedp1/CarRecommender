# CarRecommender.Web Verplaatsen naar Root Level

## 🔍 Probleem

`CarRecommender.Web` zit momenteel in `CarRecommender.Api/CarRecommender.Web/`, wat verwarring veroorzaakt bij deployment. Visual Studio kan het verkeerde project deployen.

## ✅ Oplossing: Verplaats naar Root Level

### Huidige Structuur (Verwarrend):
```
CarRecommender.Api/
├── CarRecommender.Api.csproj
├── Program.cs
└── CarRecommender.Web/          ← Zit IN de API folder!
    ├── CarRecommender.Web.csproj
    └── Program.cs
```

### Gewenste Structuur (Duidelijk):
```
CarRecommender.Api/
├── CarRecommender.Api.csproj
└── Program.cs

CarRecommender.Web/              ← Apart op root level
├── CarRecommender.Web.csproj
└── Program.cs
```

---

## 📋 Stappenplan

### Stap 1: Maak Backup (Optioneel maar Aanbevolen)
- Commit je huidige code naar Git
- Of maak een backup van de hele solution folder

### Stap 2: Verplaats de Folder

**In File Explorer:**
1. Ga naar: `CarRecommender.Api/CarRecommender.Web/`
2. **Cut** (Ctrl+X) de hele `CarRecommender.Web` folder
3. Ga naar de root: `Recommendation System/`
4. **Paste** (Ctrl+V) de folder daar

### Stap 3: Update Solution File

De solution file moet worden aangepast om het nieuwe pad te reflecteren.

### Stap 4: Update Project References

Controleer of er project references zijn die aangepast moeten worden.

### Stap 5: Test Build

Test of beide projecten nog steeds correct builden.

---

## ⚠️ Let Op

Na het verplaatsen moet je mogelijk:
- Visual Studio opnieuw openen
- Project references controleren
- Publish profiles opnieuw aanmaken

---

**Status:** ⏳ Te implementeren



