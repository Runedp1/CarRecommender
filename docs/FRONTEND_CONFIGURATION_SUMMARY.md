# Frontend Configuratie Overzicht - CarRecommender.Web

## 📋 Samenvatting

Het frontend project **CarRecommender.Web** is geconfigureerd om te werken met de Azure API. Alle API calls gebruiken de remote base URL via configuratie, zonder hard-coded localhost referenties.

**API Base URL:** `https://app-carrecommender-dev-h0agdkbfbcb3fxg7.westeurope-01.azurewebsites.net`

---

## ✅ Gewijzigde Bestanden

### 1. `appsettings.Development.json`

**Status:** ✅ Bijgewerkt

```json
{
  "DetailedErrors": true,
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ApiSettings": {
    "BaseUrl": "https://app-carrecommender-dev-h0agdkbfbcb3fxg7.westeurope-01.azurewebsites.net"
  }
}
```

**Wijziging:** ApiSettings sectie toegevoegd met de Azure API URL.

---

### 2. `appsettings.Production.json`

**Status:** ✅ Nieuw aangemaakt

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ApiSettings": {
    "BaseUrl": "https://app-carrecommender-dev-h0agdkbfbcb3fxg7.westeurope-01.azurewebsites.net"
  }
}
```

**Doel:** Productie configuratie voor Azure deployment.

---

## ✅ Bestaande Configuratie (Gecontroleerd)

### 3. `appsettings.json`

**Status:** ✅ Al correct geconfigureerd

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ApiSettings": {
    "BaseUrl": "https://app-carrecommender-dev-h0agdkbfbcb3fxg7.westeurope-01.azurewebsites.net"
  }
}
```

**Notitie:** Deze was al correct geconfigureerd met de Azure API URL.

---

### 4. `Program.cs`

**Status:** ✅ Correct geconfigureerd (geen wijzigingen nodig)

```csharp
using CarRecommender.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Configure HttpClient voor CarApiClient
var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"] 
    ?? throw new InvalidOperationException("ApiSettings:BaseUrl is niet geconfigureerd in appsettings.json");

builder.Services.AddHttpClient<CarApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
```

**Belangrijke punten:**
- ✅ HttpClient wordt geconfigureerd met BaseAddress uit `appsettings.json`
- ✅ Geen hard-coded URLs
- ✅ Statische bestanden zijn correct geconfigureerd met `MapStaticAssets()`
- ✅ Razor Pages zijn correct gemapped

---

### 5. `Services/CarApiClient.cs`

**Status:** ✅ Correct geconfigureerd (geen wijzigingen nodig)

```csharp
using System.Net.Http.Json;
using System.Text.Json;
using CarRecommender.Web.Models;

namespace CarRecommender.Web.Services;

public class CarApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CarApiClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public CarApiClient(HttpClient httpClient, ILogger<CarApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<List<Car>?> GetAllCarsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/cars");
            response.EnsureSuccessStatusCode();
            
            var cars = await response.Content.ReadFromJsonAsync<List<Car>>(_jsonOptions);
            return cars;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Fout bij het ophalen van auto's van de API");
            throw;
        }
    }

    public async Task<Car?> GetCarByIdAsync(int id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/cars/{id}");
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Car>(_jsonOptions);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Fout bij het ophalen van auto {CarId} van de API", id);
            throw;
        }
    }

    public async Task<List<RecommendationResult>?> GetRecommendationsAsync(int carId, int top = 5)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/recommendations/{carId}?top={top}");
            response.EnsureSuccessStatusCode();
            
            return await response.Content.ReadFromJsonAsync<List<RecommendationResult>>(_jsonOptions);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Fout bij het ophalen van recommendations voor auto {CarId}", carId);
            throw;
        }
    }

    public async Task<List<RecommendationResult>?> GetRecommendationsFromTextAsync(string text, int top = 5)
    {
        try
        {
            var request = new RecommendationTextRequest
            {
                Text = text,
                Top = top
            };

            var response = await _httpClient.PostAsJsonAsync("/api/recommendations/text", request, _jsonOptions);
            response.EnsureSuccessStatusCode();
            
            return await response.Content.ReadFromJsonAsync<List<RecommendationResult>>(_jsonOptions);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Fout bij het ophalen van recommendations op basis van tekst: {Text}", text);
            throw;
        }
    }
}
```

**Belangrijke punten:**
- ✅ Gebruikt relatieve paths (bijv. `/api/cars`) in plaats van absolute URLs
- ✅ BaseAddress wordt automatisch toegevoegd door HttpClient configuratie
- ✅ Geen hard-coded localhost of API URLs
- ✅ Proper error handling en logging

---

### 6. `Pages/Index.cshtml.cs`

**Status:** ✅ Correct geconfigureerd (geen wijzigingen nodig)

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CarRecommender.Web.Services;
using CarRecommender.Web.Models;

namespace CarRecommender.Web.Pages;

public class IndexModel : PageModel
{
    private readonly CarApiClient _apiClient;
    private readonly ILogger<IndexModel> _logger;

    [BindProperty]
    public string? SearchText { get; set; }

    public List<RecommendationResult>? Recommendations { get; set; }
    public string? ErrorMessage { get; set; }

    public IndexModel(CarApiClient apiClient, ILogger<IndexModel> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public void OnGet()
    {
        // Lege pagina bij GET request
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            ErrorMessage = "Voer alstublieft uw wensen in.";
            return Page();
        }

        try
        {
            Recommendations = await _apiClient.GetRecommendationsFromTextAsync(SearchText, top: 5);
            
            if (Recommendations == null || Recommendations.Count == 0)
            {
                ErrorMessage = "Geen auto's gevonden die voldoen aan uw criteria. Probeer het opnieuw met andere wensen.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fout bij het ophalen van recommendations");
            ErrorMessage = "Er is een fout opgetreden bij het verbinden met de API. Controleer of de API bereikbaar is.";
        }

        return Page();
    }
}
```

**Belangrijke punten:**
- ✅ Gebruikt dependency injection voor `CarApiClient`
- ✅ Geen directe API calls, alles via de service
- ✅ Proper error handling

---

### 7. `Pages/Cars.cshtml.cs`

**Status:** ✅ Correct geconfigureerd (geen wijzigingen nodig)

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CarRecommender.Web.Services;
using CarRecommender.Web.Models;

namespace CarRecommender.Web.Pages;

public class CarsModel : PageModel
{
    private readonly CarApiClient _apiClient;
    private readonly ILogger<CarsModel> _logger;

    public List<Car>? AllCars { get; set; }
    public string? ErrorMessage { get; set; }

    public CarsModel(CarApiClient apiClient, ILogger<CarsModel> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task OnGetAsync()
    {
        try
        {
            AllCars = await _apiClient.GetAllCarsAsync();
            
            if (AllCars == null || AllCars.Count == 0)
            {
                ErrorMessage = "Geen auto's gevonden.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fout bij het ophalen van auto's");
            ErrorMessage = "Er is een fout opgetreden bij het verbinden met de API. Controleer of de API bereikbaar is.";
        }
    }
}
```

**Belangrijke punten:**
- ✅ Gebruikt dependency injection voor `CarApiClient`
- ✅ Geen directe API calls
- ✅ Proper error handling

---

### 8. `Properties/launchSettings.json`

**Status:** ✅ Correct (geen wijzigingen nodig)

```json
{
  "$schema": "https://json.schemastore.org/launchsettings.json",
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "applicationUrl": "http://localhost:7000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
    "https": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "applicationUrl": "https://localhost:7001;http://localhost:7000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

**Notitie:** 
- ✅ Deze localhost URLs zijn alleen voor het lokaal draaien van de **frontend** zelf
- ✅ Ze hebben geen invloed op de API calls (die gebruiken de configuratie uit appsettings.json)
- ✅ Dit is correct en hoeft niet aangepast te worden

---

## 🔍 Verificatie: Geen Hard-coded Localhost Referenties

Gezocht naar localhost referenties in de code:

```bash
# Gevonden in:
- launchSettings.json (OK - alleen voor frontend zelf)
- README.md (documentatie)
- START.md (documentatie)
```

**Conclusie:** ✅ Geen hard-coded localhost referenties in de daadwerkelijke code voor API calls.

---

## 📁 Project Structuur

```
CarRecommender.Api/
└── CarRecommender.Web/
    ├── appsettings.json                    ✅ Azure API URL
    ├── appsettings.Development.json       ✅ Azure API URL (bijgewerkt)
    ├── appsettings.Production.json        ✅ Azure API URL (nieuw)
    ├── Program.cs                          ✅ HttpClient configuratie
    ├── Services/
    │   └── CarApiClient.cs                 ✅ Relatieve API paths
    ├── Pages/
    │   ├── Index.cshtml.cs                 ✅ Gebruikt CarApiClient
    │   └── Cars.cshtml.cs                  ✅ Gebruikt CarApiClient
    ├── Properties/
    │   └── launchSettings.json             ✅ Alleen voor frontend
    └── wwwroot/                            ✅ Statische bestanden aanwezig
```

---

## ✅ Deployment Klaar

Het project is nu klaar om te deployen naar Azure App Service. Zie `docs/AZURE_FRONTEND_DEPLOYMENT.md` voor het volledige stappenplan.

---

**Laatste update:** $(date)
**Status:** ✅ Klaar voor Azure Deployment



