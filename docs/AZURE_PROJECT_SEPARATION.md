# Azure Project Scheiding - API vs Frontend

## 🔍 Probleem Analyse

Je hebt momenteel **twee verschillende projecten** in je solution:

### 1. **CarRecommender.Api** (Web API Project)
- **Type:** ASP.NET Core Web API
- **Doel:** Backend API die JSON responses teruggeeft
- **Kenmerken:**
  - Gebruikt `AddControllers()` en `MapControllers()`
  - Heeft een `HomeController` met route `[Route("/")]` die de JSON welkomstboodschap teruggeeft
  - Heeft Swagger UI voor API documentatie
  - **Dit is wat je nu ziet op `app-carrecommender-web-dev2`** ❌

### 2. **CarRecommender.Web** (Frontend Project)
- **Type:** ASP.NET Core Razor Pages
- **Doel:** Frontend website met UI voor gebruikers
- **Kenmerken:**
  - Gebruikt `AddRazorPages()` en `MapRazorPages()`
  - Heeft HTML/CSS/JavaScript voor de gebruikersinterface
  - Maakt HTTP calls naar de API
  - **Dit is wat je WIL zien op `app-carrecommender-web-dev2`** ✅

## ❌ Huidige Situatie

Je hebt waarschijnlijk **CarRecommender.Api** gedeployed naar `app-carrecommender-web-dev2`, waardoor je de JSON API response ziet in plaats van de website.

## ✅ Oplossing: Twee Aparte Web Apps

Je moet **twee aparte Azure Web Apps** hebben:

### Web App 1: API (Backend)
- **Naam:** `app-carrecommender-dev` (al bestaat)
- **URL:** `https://app-carrecommender-dev-h0agdkbfbcb3fxg7.westeurope-01.azurewebsites.net`
- **Project:** `CarRecommender.Api`
- **Doel:** JSON API endpoints

### Web App 2: Frontend (Website)
- **Naam:** `app-carrecommender-web-dev2` (al bestaat, maar verkeerd project)
- **URL:** `https://app-carrecommender-web-dev2.azurewebsites.net` (of jouw URL)
- **Project:** `CarRecommender.Web`
- **Doel:** Website met UI voor docenten

---

## 📋 Stappenplan

### Stap 1: Deploy CarRecommender.Web naar app-carrecommender-web-dev2

1. **Open Visual Studio**
2. **Rechtsklik** op project **`CarRecommender.Web`** (NIET CarRecommender.Api!)
3. Selecteer **"Publish"** of **"Publiceren"**
4. Kies **"Azure App Service"**
5. Selecteer **"Select existing"** of **"Bestaande selecteren"**
6. Kies **`app-carrecommender-web-dev2`** uit de lijst
7. Klik **"Finish"** en dan **"Publish"**

### Stap 2: Verifieer API URL Configuratie

Controleer dat `CarRecommender.Web/appsettings.json` de juiste API URL heeft:

```json
{
  "ApiSettings": {
    "BaseUrl": "https://app-carrecommender-dev-h0agdkbfbcb3fxg7.westeurope-01.azurewebsites.net"
  }
}
```

### Stap 3: Test Beide URLs

- **API URL:** `https://app-carrecommender-dev-h0agdkbfbcb3fxg7.westeurope-01.azurewebsites.net`
  - Moet JSON teruggeven (zoals nu)
  
- **Frontend URL:** `https://app-carrecommender-web-dev2.azurewebsites.net`
  - Moet de website met UI tonen (niet JSON!)

---

## 🔧 Technische Details

### Waarom zie je nu JSON in plaats van de website?

**CarRecommender.Api/Program.cs:**
```csharp
builder.Services.AddControllers();  // ← Dit maakt het een API
app.MapControllers();               // ← Dit mapt API routes
```

**CarRecommender.Api/Controllers/HomeController.cs:**
```csharp
[Route("/")]  // ← Dit vangt de root URL op
public IActionResult Get() {
    return Ok(new { welkom = "..." });  // ← Dit geeft JSON terug
}
```

**CarRecommender.Web/Program.cs:**
```csharp
builder.Services.AddRazorPages();  // ← Dit maakt het een website
app.MapRazorPages();               // ← Dit mapt website routes
```

### Project Type Verschil

| Feature | CarRecommender.Api | CarRecommender.Web |
|---------|-------------------|-------------------|
| **Project Type** | Web API | Razor Pages |
| **Services** | `AddControllers()` | `AddRazorPages()` |
| **Routing** | `MapControllers()` | `MapRazorPages()` |
| **Output** | JSON | HTML |
| **Voor** | Backend/API | Frontend/UI |

---

## 📝 Deployment Checklist

- [ ] **CarRecommender.Api** is gedeployed naar `app-carrecommender-dev`
- [ ] **CarRecommender.Web** is gedeployed naar `app-carrecommender-web-dev2`
- [ ] API URL is correct geconfigureerd in `CarRecommender.Web/appsettings.json`
- [ ] Frontend URL toont de website (niet JSON)
- [ ] API URL toont JSON endpoints
- [ ] Frontend kan succesvol API calls maken

---

## 🎯 Resultaat

Na correcte deployment:

- **Voor docenten:** Open `https://app-carrecommender-web-dev2.azurewebsites.net`
  - Zien een mooie website met zoekfunctionaliteit
  - Kunnen auto's zoeken en recommendations bekijken
  
- **Voor developers:** Open `https://app-carrecommender-dev-h0agdkbfbcb3fxg7.westeurope-01.azurewebsites.net`
  - Zien JSON API responses
  - Kunnen API endpoints testen

---

**Laatste update:** $(date)
**Status:** ✅ Klaar voor deployment



