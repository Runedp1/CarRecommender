# Finale Project Structuur

## ✅ Opgeruimde Structuur

### Huidige Structuur (Schoon en Logisch):

```
Recommendation System/
├── backend/                       ← Backend projecten
│   └── CarRecommender.Api/       ← Web API
│       ├── CarRecommender.Api.csproj
│       ├── Program.cs
│       ├── Controllers/
│       └── appsettings.json
│
├── frontend/                      ← Frontend projecten
│   └── CarRecommender.Web/       ← Razor Pages website
│       ├── CarRecommender.Web.csproj
│       ├── Program.cs
│       ├── Pages/
│       └── wwwroot/
│
├── src/                           ← Shared business logic
│   ├── Car.cs
│   ├── CarRepository.cs
│   ├── RecommendationService.cs
│   └── ...
│
├── data/                          ← Data files
│   └── Cleaned_Car_Data_For_App_Fully_Enriched.csv
│
├── scripts/                       ← Python scripts
│
├── docs/                          ← Documentatie
│
├── CarRecommender.csproj          ← Shared library (root)
└── CarRecommender.sln             ← Solution file
```

---

## 🗑️ Verwijderd

- ✅ Oude `CarRecommender.Api/` folder op root level
- ✅ Lege `CarRecommender.Web/` folders in backend en root
- ✅ `CarRecommender/` folder (duplicate project)

---

## 📋 Project Overzicht

### Backend
- **Locatie:** `backend/CarRecommender.Api/`
- **Type:** ASP.NET Core Web API
- **Azure:** `app-carrecommender-dev`
- **Doel:** JSON API endpoints

### Frontend
- **Locatie:** `frontend/CarRecommender.Web/`
- **Type:** ASP.NET Core Razor Pages
- **Azure:** `app-carrecommender-web-dev2`
- **Doel:** Website met UI voor docenten

### Shared Library
- **Locatie:** `CarRecommender.csproj` (root)
- **Type:** Class Library
- **Doel:** Gedeelde business logic (Car, Repository, Services)

---

## ✅ Voordelen

1. **Duidelijke Scheiding:** Backend en frontend zijn volledig gescheiden
2. **Geen Duplicaten:** Alle oude/lege folders zijn verwijderd
3. **Logische Organisatie:** Alles staat op de juiste plek
4. **Eenvoudige Deployment:** Elk project kan apart gedeployed worden
5. **Overzichtelijk:** Solution folders maken het duidelijk

---

## 🚀 Deployment

### Backend Deployen:
```
backend/CarRecommender.Api → app-carrecommender-dev
```

### Frontend Deployen:
```
frontend/CarRecommender.Web → app-carrecommender-web-dev2
```

---

**Status:** ✅ Structuur Opgeruimd en Georganiseerd
**Datum:** $(date)


