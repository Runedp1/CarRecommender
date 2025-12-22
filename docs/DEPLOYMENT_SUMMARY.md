# Deployment Samenvatting - API vs Frontend Scheiding

## 📊 Project Overzicht

### ✅ **CarRecommender.Api** (Web API - Backend)
- **Project Type:** ASP.NET Core Web API
- **Azure Web App:** `app-carrecommender-dev`
- **URL:** `https://app-carrecommender-dev-h0agdkbfbcb3fxg7.westeurope-01.azurewebsites.net`
- **Doel:** JSON API endpoints voor data en recommendations
- **Status:** ✅ Al gedeployed en werkend

### ✅ **CarRecommender.Web** (Razor Pages - Frontend)
- **Project Type:** ASP.NET Core Razor Pages
- **Azure Web App:** `app-carrecommender-web-dev2`
- **URL:** `https://app-carrecommender-web-dev2.azurewebsites.net` (of jouw specifieke URL)
- **Doel:** Website met UI voor docenten om auto's te zoeken
- **Status:** ⚠️ Moet opnieuw gedeployed worden met het juiste project

---

## 🔍 Waarom zie je nu JSON in plaats van de website?

Je hebt waarschijnlijk **CarRecommender.Api** gedeployed naar `app-carrecommender-web-dev2` in plaats van **CarRecommender.Web**.

**Bewijs:**
- De JSON response `{ "welkom": "Welkom bij de Car Recommender API!", ... }` komt van `CarRecommender.Api/Controllers/HomeController.cs`
- Deze controller heeft route `[Route("/")]` en geeft JSON terug
- **CarRecommender.Web** zou HTML/Razor Pages moeten tonen, geen JSON

---

## ✅ Oplossing: Deploy het Juiste Project

### Stap 1: Deploy CarRecommender.Web naar app-carrecommender-web-dev2

**In Visual Studio:**

1. **Rechtsklik** op **`CarRecommender.Web`** project (NIET CarRecommender.Api!)
   - Pad: `CarRecommender.Api/CarRecommender.Web/CarRecommender.Web.csproj`

2. Selecteer **"Publish"** of **"Publiceren"**

3. Kies **"Azure"** → **"Azure App Service (Windows)"**

4. Selecteer **"Select existing"** of **"Bestaande selecteren"**

5. Kies **`app-carrecommender-web-dev2`** uit de lijst

6. Klik **"Finish"** en dan **"Publish"**

### Stap 2: Verifieer Configuratie

Controleer dat de API URL correct is in:
- `CarRecommender.Api/CarRecommender.Web/appsettings.json`
- `CarRecommender.Api/CarRecommender.Web/appsettings.Production.json`

```json
{
  "ApiSettings": {
    "BaseUrl": "https://app-carrecommender-dev-h0agdkbfbcb3fxg7.westeurope-01.azurewebsites.net"
  }
}
```

---

## 📁 Project Structuur

```
CarRecommender.sln
├── CarRecommender.Api/                    ← API Project
│   ├── CarRecommender.Api.csproj          ← Deploy naar: app-carrecommender-dev
│   ├── Program.cs                         ← AddControllers(), MapControllers()
│   ├── Controllers/
│   │   ├── HomeController.cs             ← Geeft JSON terug op "/"
│   │   ├── CarsController.cs
│   │   └── RecommendationsController.cs
│   └── CarRecommender.Web/                ← Frontend Project (subfolder!)
│       ├── CarRecommender.Web.csproj      ← Deploy naar: app-carrecommender-web-dev2
│       ├── Program.cs                     ← AddRazorPages(), MapRazorPages()
│       ├── Pages/                         ← Razor Pages (HTML)
│       │   ├── Index.cshtml               ← Homepage met zoekfunctionaliteit
│       │   └── Cars.cshtml                ← Lijst van auto's
│       └── Services/
│           └── CarApiClient.cs            ← Maakt HTTP calls naar API
```

---

## 🔧 Technische Verschillen

### CarRecommender.Api (Web API)

**Program.cs:**
```csharp
builder.Services.AddControllers();  // ← API services
// ...
app.MapControllers();               // ← API routes
```

**HomeController.cs:**
```csharp
[Route("/")]
public IActionResult Get() {
    return Ok(new { welkom = "..." });  // ← JSON response
}
```

**Resultaat:** JSON responses op `/`, `/api/cars`, etc.

---

### CarRecommender.Web (Razor Pages)

**Program.cs:**
```csharp
builder.Services.AddRazorPages();  // ← Website services
// ...
app.MapRazorPages();               // ← Website routes
```

**Index.cshtml:**
```html
@page
<h1>Car Recommender</h1>
<!-- HTML/CSS/JavaScript voor UI -->
```

**Resultaat:** HTML website op `/`, `/Cars`, etc.

---

## 📋 Deployment Matrix

| Project | Azure Web App | URL | Type | Status |
|---------|--------------|-----|------|--------|
| **CarRecommender.Api** | `app-carrecommender-dev` | `https://app-carrecommender-dev-h0agdkbfbcb3fxg7.westeurope-01.azurewebsites.net` | API | ✅ Correct |
| **CarRecommender.Web** | `app-carrecommender-web-dev2` | `https://app-carrecommender-web-dev2.azurewebsites.net` | Website | ⚠️ Moet opnieuw deployed |

---

## ✅ Verificatie na Deployment

### Test 1: API URL
Open: `https://app-carrecommender-dev-h0agdkbfbcb3fxg7.westeurope-01.azurewebsites.net`

**Verwacht:** JSON response
```json
{
  "welkom": "Welkom bij de Car Recommender API!",
  "endpoints": { ... }
}
```

### Test 2: Frontend URL
Open: `https://app-carrecommender-web-dev2.azurewebsites.net`

**Verwacht:** HTML website met:
- Zoekbalk voor auto's
- Lijst van auto's
- Recommendations functionaliteit
- **GEEN JSON!**

---

## 🎯 Voor Docenten

Na correcte deployment kunnen docenten:

1. **Openen:** `https://app-carrecommender-web-dev2.azurewebsites.net`
2. **Zien:** Een mooie website met zoekfunctionaliteit
3. **Gebruiken:** Zoeken naar auto's en recommendations bekijken
4. **Geen technische kennis nodig:** Gewoon een normale website!

---

## 📝 Checklist

- [ ] **CarRecommender.Web** is gedeployed naar `app-carrecommender-web-dev2`
- [ ] Frontend URL toont website (niet JSON)
- [ ] API URL is correct geconfigureerd in appsettings.json
- [ ] Frontend kan succesvol API calls maken
- [ ] Test de zoekfunctionaliteit op de frontend
- [ ] Deel de frontend URL met docenten

---

**Laatste update:** $(date)
**Status:** ✅ Klaar voor deployment



