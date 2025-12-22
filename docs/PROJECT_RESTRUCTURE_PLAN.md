# Project Herstructurering Plan

## 🎯 Doel: Logische Scheiding Backend en Frontend

### Huidige Structuur (Verwarrend):
```
Recommendation System/
├── CarRecommender.csproj          ← Shared library (root)
├── CarRecommender/                 ← Duplicate/oude project?
├── CarRecommender.Api/             ← Backend API
│   └── CarRecommender.Web/         ← Frontend (zit IN backend!)
├── src/                            ← Shared source code
└── data/                           ← Data files
```

### Gewenste Structuur (Logisch):
```
Recommendation System/
├── src/                            ← Shared business logic
│   ├── CarRecommender.Core/        ← Core domain models & interfaces
│   └── CarRecommender.Services/   ← Business services
├── backend/                        ← Backend projecten
│   └── CarRecommender.Api/        ← Web API
├── frontend/                       ← Frontend projecten
│   └── CarRecommender.Web/        ← Razor Pages website
├── data/                           ← Data files
└── scripts/                        ← Python scripts
```

---

## 📋 Stappenplan

### Stap 1: Verplaats CarRecommender.Web naar frontend/
### Stap 2: Verplaats CarRecommender.Api naar backend/
### Stap 3: Organiseer shared code in src/
### Stap 4: Update solution file
### Stap 5: Update project references
### Stap 6: Test build

---

**Status:** ⏳ Te implementeren



