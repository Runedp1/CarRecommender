# Finale Project Organisatie

## ✅ Opgeruimde Structuur

### Nieuwe Structuur (Alles Georganiseerd):

```
Recommendation System/
├── backend/                       ← Backend projecten
│   ├── CarRecommender.Api/       ← Web API
│   ├── data/                     ← CSV bestanden (verplaatst)
│   └── images/                   ← Auto afbeeldingen (verplaatst)
│
├── frontend/                      ← Frontend projecten
│   └── CarRecommender.Web/       ← Razor Pages website
│
├── tools/                         ← Development tooling (nieuw)
│   ├── scripts/                  ← Python scripts (verplaatst)
│   └── notebooks/                ← Jupyter notebooks (verplaatst)
│
├── src/                           ← Shared business logic (blijft op root)
├── docs/                          ← Documentatie (blijft op root)
├── CarRecommender.csproj          ← Shared library (blijft op root)
└── CarRecommender.sln             ← Solution file (blijft op root)
```

---

## 📋 Wat is Verplaatst?

### ✅ Verplaatst naar `backend/`:
- **`data/`** → `backend/data/` (gebruikt door backend API)
- **`images/`** → `backend/images/` (gebruikt door backend voor static files)

### ✅ Verplaatst naar `tools/`:
- **`scripts/`** → `tools/scripts/` (development tooling)
- **`notebooks/`** → `tools/notebooks/` (development/analysis)

### ✅ Blijft op Root:
- **`src/`** - Shared business logic (gebruikt door CarRecommender.csproj)
- **`docs/`** - Documentatie (standaard locatie)
- **`CarRecommender.csproj`** - Shared library (project references)
- **`CarRecommender.sln`** - Solution file

---

## 🎯 Voordelen

1. **Duidelijker:** Alles wat bij backend hoort staat in `backend/`
2. **Overzichtelijker:** Development tools in `tools/`
3. **Logischer:** Data en images bij de backend die ze gebruikt
4. **Schoner:** Minder folders op root level (alleen essentiële)

---

## 📝 Configuratie

De `data` directory configuratie blijft werken omdat:
- In `appsettings.json`: `"DataDirectory": "data"` (relatief pad)
- CarRepository zoekt relatief ten opzichte van de executable
- Bij deployment wordt `data/` folder gekopieerd naar output directory

---

## ✅ Resultaat

- **Root level:** Alleen essentiële folders (src, docs, solution, shared library)
- **Backend:** Alles wat backend nodig heeft (API, data, images)
- **Frontend:** Frontend project
- **Tools:** Development tooling apart georganiseerd

---

**Status:** ✅ Project Volledig Georganiseerd
**Datum:** $(date)


