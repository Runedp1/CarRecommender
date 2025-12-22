# AZURE DEPLOYMENT PLAN
**Doel**: Deployment van Car Recommendation System naar Azure Cloud

---

## 🎯 AZURE ARCHITECTUUR OVERZICHT

### Aanbevolen Azure Services

```
┌─────────────────────────────────────────────────┐
│         Azure App Service (Web API)             │  ← REST API voor recommendations
│         .NET 9.0 ASP.NET Core                   │
└─────────────────────────────────────────────────┘
                      │
         ┌────────────┴────────────┐
         │                         │
┌────────────────┐      ┌──────────────────┐
│  Azure SQL DB  │      │ Azure Blob Store │
│  (Car Data)    │      │  (Images)        │
└────────────────┘      └──────────────────┘
```

---

## 📋 BENODIGDE AANPASSINGEN

### 1. **Backend: Console → REST API** 🔄

**Huidige situatie:**
- Console applicatie (`Program.cs`)
- Direct CSV file access
- Synchronous execution

**Azure aanpassingen:**
- ✅ **ASP.NET Core Web API** project
- ✅ **REST endpoints** voor recommendations
- ✅ **Async/await** patterns
- ✅ **Dependency Injection** voor Azure services

**Nieuwe structuur:**
```
src/
├── Controllers/
│   └── RecommendationsController.cs    # REST API endpoints
├── Services/
│   ├── CarRepository.cs                # Aangepast voor Azure SQL
│   ├── RecommendationService.cs        # (Bestaat al)
│   └── RecommendationEngine.cs         # (Bestaat al)
├── Models/
│   └── Car.cs                          # (Bestaat al)
└── Program.cs                          # Startup configuratie
```

### 2. **Data Layer: CSV → Azure SQL Database** 🔄

**Huidige situatie:**
- CSV bestand lokaal inlezen
- `File.ReadAllLines()` voor data toegang
- Geen database

**Azure aanpassingen:**
- ✅ **Azure SQL Database** setup
- ✅ **Entity Framework Core** of **ADO.NET** voor data toegang
- ✅ **Connection strings** via Azure Key Vault / App Configuration
- ✅ **Migration scripts** voor CSV → SQL import

**Data migratie:**
```sql
-- Nieuwe tabel structuur
CREATE TABLE Cars (
    Id INT PRIMARY KEY,
    Brand NVARCHAR(100),
    Model NVARCHAR(200),
    Power DECIMAL(10,2),
    Fuel NVARCHAR(50),
    Budget DECIMAL(10,2),
    Year INT,
    ImagePath NVARCHAR(500),
    -- Extra kolommen uit enriched dataset
    CO2_wltp DECIMAL(10,2),
    Electric_range_km INT,
    Engine_cm3 INT,
    ...
)
```

### 3. **Images: Lokale map → Azure Blob Storage** 🔄

**Huidige situatie:**
- Image paths: `images/{brand}/{model}/{id}.jpg`
- Verwijst naar lokale bestanden

**Azure aanpassingen:**
- ✅ **Azure Blob Storage Container** voor images
- ✅ **SAS tokens** of **CDN** voor image delivery
- ✅ Image paths updaten naar blob URLs
- ✅ Image upload API endpoint (optioneel)

**Nieuwe image path structuur:**
```
https://{storageaccount}.blob.core.windows.net/images/{brand}/{model}/{id}.jpg
```

### 4. **Configuration Management** ✨

**Huidige situatie:**
- Hardcoded paden
- Lokale configuratie

**Azure aanpassingen:**
- ✅ **Azure App Configuration** of **Key Vault**
- ✅ **appsettings.json** voor lokale dev
- ✅ **appsettings.Production.json** voor Azure
- ✅ Connection strings en secrets beveiligd

---

## 🛠️ IMPLEMENTATIE STAPPEN

### Fase 1: Azure Setup & Database Migratie

#### 1.1 Azure Resources Aanmaken
```bash
# Azure CLI commands (optioneel, kan ook via portal)
az group create --name rg-car-recommender --location westeurope

# Azure SQL Database
az sql server create \
  --name car-recommender-sql \
  --resource-group rg-car-recommender \
  --location westeurope \
  --admin-user adminuser \
  --admin-password <secure-password>

az sql db create \
  --resource-group rg-car-recommender \
  --server car-recommender-sql \
  --name CarDatabase \
  --service-objective S0

# Storage Account voor images
az storage account create \
  --name carrecommenderstorage \
  --resource-group rg-car-recommender \
  --location westeurope \
  --sku Standard_LRS

# App Service Plan
az appservice plan create \
  --name asp-car-recommender \
  --resource-group rg-car-recommender \
  --sku B1 \
  --is-linux

# Web App
az webapp create \
  --name car-recommender-api \
  --resource-group rg-car-recommender \
  --plan asp-car-recommender \
  --runtime "DOTNET|9.0"
```

#### 1.2 CSV → Azure SQL Migratie Script
```python
# scripts/migrate_csv_to_azure_sql.py
import pandas as pd
import pyodbc
from sqlalchemy import create_engine

# CSV inlezen
df = pd.read_csv('data/Cleaned_Car_Data_For_App_Fully_Enriched.csv')

# Connection string
server = 'car-recommender-sql.database.windows.net'
database = 'CarDatabase'
username = 'adminuser'
password = '<password>'
driver = '{ODBC Driver 17 for SQL Server}'

connection_string = f'mssql+pyodbc://{username}:{password}@{server}/{database}?driver={driver}'

engine = create_engine(connection_string)

# Naar Azure SQL schrijven
df.to_sql('Cars', engine, if_exists='replace', index=False)
```

### Fase 2: C# Code Aanpassingen

#### 2.1 Nieuw ASP.NET Core Project
```bash
cd "Recommendation System"
dotnet new webapi -n CarRecommender.Api -f net9.0
```

#### 2.2 Entity Framework Core Setup
```bash
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Tools
```

#### 2.3 Nieuwe CarRepository (Azure SQL)
```csharp
// src/Services/CarRepository.cs (aangepast)
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CarRecommender.Services
{
    public class CarRepository : ICarRepository
    {
        private readonly CarDbContext _context;

        public CarRepository(CarDbContext context)
        {
            _context = context;
        }

        public async Task<List<Car>> GetAllCarsAsync()
        {
            return await _context.Cars.ToListAsync();
        }

        public async Task<Car?> GetCarByIdAsync(int id)
        {
            return await _context.Cars.FindAsync(id);
        }

        // ... andere methodes
    }
}
```

#### 2.4 REST API Controller
```csharp
// src/Controllers/RecommendationsController.cs
using Microsoft.AspNetCore.Mvc;
using CarRecommender.Services;

namespace CarRecommender.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RecommendationsController : ControllerBase
    {
        private readonly RecommendationService _recommendationService;

        public RecommendationsController(RecommendationService recommendationService)
        {
            _recommendationService = recommendationService;
        }

        [HttpGet("{carId}/recommendations")]
        public async Task<IActionResult> GetRecommendations(int carId, [FromQuery] int count = 5)
        {
            var recommendations = await _recommendationService
                .RecommendSimilarCarsAsync(carId, count);
            
            return Ok(recommendations);
        }

        [HttpGet("cars")]
        public async Task<IActionResult> GetAllCars()
        {
            var cars = await _repository.GetAllCarsAsync();
            return Ok(cars);
        }
    }
}
```

#### 2.5 Dependency Injection Setup
```csharp
// src/Program.cs
using Microsoft.EntityFrameworkCore;
using CarRecommender.Services;

var builder = WebApplication.CreateBuilder(args);

// Services toevoegen
builder.Services.AddControllers();
builder.Services.AddDbContext<CarDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ICarRepository, CarRepository>();
builder.Services.AddScoped<RecommendationEngine>();
builder.Services.AddScoped<RecommendationService>();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

### Fase 3: Image Storage Setup

#### 3.1 Azure Blob Storage Integration
```csharp
// src/Services/ImageService.cs
using Azure.Storage.Blobs;

public class ImageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private const string ContainerName = "images";

    public ImageService(string connectionString)
    {
        _blobServiceClient = new BlobServiceClient(connectionString);
    }

    public string GetImageUrl(string brand, string model, int carId)
    {
        var blobName = $"{brand}/{model}/{carId}.jpg";
        var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
        var blobClient = containerClient.GetBlobClient(blobName);
        
        return blobClient.Uri.ToString();
    }
}
```

### Fase 4: Deployment

#### 4.1 Deployment via Azure CLI
```bash
# Build project
dotnet publish -c Release -o ./publish

# Deploy naar Azure
az webapp deployment source config-zip \
  --resource-group rg-car-recommender \
  --name car-recommender-api \
  --src ./publish.zip
```

#### 4.2 CI/CD Pipeline (optioneel)
```yaml
# .github/workflows/azure-deploy.yml
name: Deploy to Azure

on:
  push:
    branches: [ main ]

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v2
      - name: Setup .NET
        uses: actions/setup-dotnet@v1
        with:
          dotnet-version: '9.0.x'
      - name: Build
        run: dotnet build --configuration Release
      - name: Publish
        run: dotnet publish -c Release -o ./publish
      - name: Deploy to Azure
        uses: azure/webapps-deploy@v2
        with:
          app-name: 'car-recommender-api'
          publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
          package: ./publish
```

---

## 📊 KOSTEN SCHATTING (Maandelijks)

| Service | Tier | Geschatte Kosten |
|---------|------|------------------|
| Azure SQL Database | S0 (Basic, 10 DTU) | ~€5-10 |
| App Service | B1 (Basic) | ~€10-15 |
| Blob Storage | Standard LRS, ~5 GB | ~€0.10 |
| Data Transfer | Eerste 5 GB gratis | €0 |
| **TOTAAL** | | **~€15-25/maand** |

**Kostenoptimalisatie:**
- Start met Basic tiers voor development
- Scale up naar Standard/Production tiers indien nodig
- Gebruik Azure Dev/Test pricing als beschikbaar

---

## 🔒 BEVEILIGING & BEST PRACTICES

### 1. Connection Strings
- ✅ Gebruik **Azure Key Vault** voor secrets
- ✅ Nooit hardcoded credentials
- ✅ Managed Identity waar mogelijk

### 2. API Beveiliging
- ✅ **HTTPS** verplicht (App Service default)
- ✅ **API Keys** of **Azure AD** authentication
- ✅ **Rate limiting** implementeren
- ✅ **CORS** correct configureren

### 3. Database
- ✅ **Firewall rules** configureren
- ✅ **Encryption at rest** (standaard aan)
- ✅ **SQL injection** prevention (EF Core gebruikt parameterized queries)

---

## 🚀 API ENDPOINTS OVERZICHT

### Geplande REST API Endpoints

```
GET  /api/cars                          # Lijst alle auto's
GET  /api/cars/{id}                     # Specifieke auto details
GET  /api/recommendations/{carId}       # Recommendations voor auto
GET  /api/recommendations/{carId}?count=10  # Aantal recommendations aanpassen

POST /api/recommendations/search        # Custom recommendation query
GET  /api/cars/search?brand=BMW         # Zoeken/filteren auto's
GET  /api/health                        # Health check
```

### Response Voorbeeld
```json
{
  "targetCar": {
    "id": 1,
    "brand": "BMW",
    "model": "3 Series",
    "power": 180,
    "fuel": "Petrol",
    "budget": 35000,
    "year": 2020
  },
  "recommendations": [
    {
      "car": { ... },
      "similarityScore": 0.92
    },
    ...
  ]
}
```

---

## 📝 MIGRATIE CHECKLIST

### Pre-Migration
- [ ] Azure account en resource group aangemaakt
- [ ] Azure SQL Database aangemaakt
- [ ] Storage Account aangemaakt
- [ ] App Service aangemaakt

### Data Migration
- [ ] CSV naar Azure SQL migratie script uitgevoerd
- [ ] Data gecontroleerd en gevalideerd
- [ ] Images naar Blob Storage geüpload (indien beschikbaar)

### Code Migration
- [ ] ASP.NET Core Web API project aangemaakt
- [ ] Entity Framework Core geconfigureerd
- [ ] CarRepository aangepast voor Azure SQL
- [ ] REST API controllers geïmplementeerd
- [ ] ImageService geïmplementeerd voor Blob Storage
- [ ] Dependency Injection geconfigureerd
- [ ] appsettings.json aangepast voor Azure

### Testing
- [ ] Lokale tests uitgevoerd
- [ ] API endpoints getest
- [ ] Database connectiviteit getest
- [ ] Image URLs getest

### Deployment
- [ ] Application gepubliceerd naar Azure
- [ ] Connection strings geconfigureerd in App Service
- [ ] Environment variables ingesteld
- [ ] Health checks werkend

---

## 🔄 VOLGENDE STAPPEN

### Prioriteit 1: Essentieel
1. **Azure resources aanmaken** (SQL DB, App Service, Storage)
2. **CSV → Azure SQL migratie** script maken en uitvoeren
3. **ASP.NET Core Web API** project opzetten
4. **Basic REST endpoints** implementeren

### Prioriteit 2: Belangrijk
5. **Entity Framework Core** implementeren
6. **Image Blob Storage** integratie
7. **Deployment pipeline** opzetten

### Prioriteit 3: Optimalisatie
8. **API authentication** toevoegen
9. **Caching** implementeren (Redis)
10. **Monitoring** toevoegen (Application Insights)
11. **Auto-scaling** configureren

---

## 📚 AANBEVOLEN LEESMATERIAAL

- [Azure App Service Documentation](https://docs.microsoft.com/azure/app-service/)
- [Azure SQL Database Documentation](https://docs.microsoft.com/azure/azure-sql/)
- [ASP.NET Core Web API Tutorial](https://docs.microsoft.com/aspnet/core/tutorials/first-web-api)
- [Entity Framework Core Documentation](https://docs.microsoft.com/ef/core/)

---

## ⚠️ AANDACHTSPUNTEN

1. **Data Volume**: 20.755 auto's passen goed in Azure SQL Database
2. **Performance**: Overweeg caching voor veelgebruikte queries
3. **Costs**: Start met Basic tiers, scale up indien nodig
4. **Backup**: Azure SQL heeft automatische backups (configureer retentie)
5. **Monitoring**: Gebruik Application Insights voor performance monitoring

---

**Status**: 📋 **PLAN KLAAR** - Klaar voor implementatie
**Geschatte implementatietijd**: 2-4 dagen (afhankelijk van ervaring met Azure)


