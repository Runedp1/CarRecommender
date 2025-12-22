# Project Organisatie Plan

## 📊 Huidige Root Level Folders

### Folders die mogelijk verplaatst kunnen worden:

1. **`data/`** - CSV bestanden
   - **Gebruikt door:** Backend (CarRepository laadt CSV files)
   - **Voorstel:** Verplaats naar `backend/data/`

2. **`images/`** - Auto afbeeldingen
   - **Gebruikt door:** Backend (static files via Program.cs)
   - **Voorstel:** Verplaats naar `backend/images/`

3. **`src/`** - Shared business logic
   - **Gebruikt door:** CarRecommender.csproj (shared library)
   - **Voorstel:** Blijft op root (wordt gebruikt door shared library)

4. **`scripts/`** - Python scripts voor data processing
   - **Gebruikt door:** Development tooling
   - **Voorstel:** Verplaats naar `tools/scripts/` of blijf op root

5. **`notebooks/`** - Jupyter notebooks
   - **Gebruikt door:** Development/analysis
   - **Voorstel:** Verplaats naar `tools/notebooks/` of blijf op root

6. **`docs/`** - Documentatie
   - **Voorstel:** Blijft op root (standaard locatie)

### Folders die MOETEN op root blijven:

- **`CarRecommender.csproj`** - Shared library (project references)
- **`CarRecommender.sln`** - Solution file
- **`backend/`** - Backend projecten
- **`frontend/`** - Frontend projecten

---

## ✅ Aanbevolen Structuur

```
Recommendation System/
├── backend/
│   ├── CarRecommender.Api/       ← API project
│   ├── data/                      ← CSV files (verplaatst)
│   └── images/                    ← Auto afbeeldingen (verplaatst)
│
├── frontend/
│   └── CarRecommender.Web/       ← Website project
│
├── tools/                         ← Development tooling (nieuw)
│   ├── scripts/                   ← Python scripts (verplaatst)
│   └── notebooks/                 ← Jupyter notebooks (verplaatst)
│
├── src/                           ← Shared business logic (blijft)
├── docs/                          ← Documentatie (blijft)
├── CarRecommender.csproj          ← Shared library (blijft)
└── CarRecommender.sln             ← Solution file (blijft)
```

---

## 🎯 Voordelen

1. **Duidelijker:** Alles wat bij backend hoort staat in `backend/`
2. **Overzichtelijker:** Development tools in `tools/`
3. **Logischer:** Data en images bij de backend die ze gebruikt
4. **Schoner:** Minder folders op root level

---

**Status:** ⏳ Te implementeren


