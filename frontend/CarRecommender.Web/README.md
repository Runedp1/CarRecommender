# Car Recommender Web Frontend

ASP.NET Core Razor Pages frontend voor de CarRecommender Web API.

## Vereisten

- .NET 8.0 SDK
- Werkende CarRecommender API (standaard: `https://app-carrecommender-dev-h0agdkbfbcb3fxg7.westeurope-01.azurewebsites.net`)

## Configuratie

De API base URL staat geconfigureerd in `appsettings.json`:

```json
{
  "ApiSettings": {
    "BaseUrl": "https://app-carrecommender-dev-h0agdkbfbcb3fxg7.westeurope-01.azurewebsites.net"
  }
}
```

Om een andere API URL te gebruiken, pas deze waarde aan in `appsettings.json` of `appsettings.Development.json`.

## Lokaal Starten

1. **Navigeer naar het project:**
   ```bash
   cd CarRecommender.Api/CarRecommender.Web
   ```

2. **Restore packages (indien nodig):**
   ```bash
   dotnet restore
   ```

3. **Start de applicatie:**
   ```bash
   dotnet run
   ```

4. **Open in browser:**
   - De applicatie draait standaard op: `https://localhost:5001` of `http://localhost:5000`
   - Check de terminal output voor de exacte URL

## Features

### Home Pagina ("Zoek auto")
- Groot tekstveld voor het beschrijven van wensen (budget, brandstof, transmissie, etc.)
- POST naar `/api/recommendations/text` met de tekst
- Toont aanbevolen auto's in kaart-layout met:
  - Merk, model, prijs, brandstof, bouwjaar, vermogen
  - Match percentage
  - Uitleg waarom deze auto past

### Blader door auto's Pagina
- Haalt alle auto's op via GET `/api/cars`
- Toont auto's in kaart-layout
- Client-side filters:
  - Maximaal budget (slider)
  - Brandstof (dropdown)
  - Minimum bouwjaar (slider)
- Real-time filtering zonder page reload

## Project Structuur

```
CarRecommender.Web/
├── Models/                    # Data models
│   ├── Car.cs                 # Car model
│   ├── RecommendationResult.cs # Recommendation result model
│   └── RecommendationTextRequest.cs # Request model
├── Services/                  # API client service
│   └── CarApiClient.cs        # HttpClient wrapper voor API calls
├── Pages/                     # Razor Pages
│   ├── Index.cshtml           # Home pagina (zoek auto)
│   ├── Index.cshtml.cs        # Code-behind voor Index
│   ├── Cars.cshtml            # Blader door auto's pagina
│   └── Cars.cshtml.cs        # Code-behind voor Cars
├── Program.cs                 # Startup configuratie
└── appsettings.json           # Configuratie (API URL)
```

## Technische Details

### Dependency Injection
- `CarApiClient` is geregistreerd als HttpClient service in `Program.cs`
- Base URL wordt geconfigureerd vanuit `appsettings.json`
- Timeout ingesteld op 30 seconden

### Foutafhandeling
- API errors worden opgevangen en getoond als error alerts
- Duidelijke Nederlandse foutmeldingen
- Logging naar console voor debugging

### Styling
- Bootstrap 5 voor responsive layout
- Card-based layout voor auto's
- Responsive grid (col-md-6 col-lg-4)

## Troubleshooting

### API niet bereikbaar
- Controleer of de API URL correct is in `appsettings.json`
- Controleer of de API draait en bereikbaar is
- Check de browser console voor CORS errors (als API CORS niet correct is geconfigureerd)

### Geen auto's getoond
- Controleer de API logs voor errors
- Check of de API endpoints correct werken (test met Postman/curl)
- Controleer de browser console voor JavaScript errors

### Build errors
- Zorg dat .NET 8.0 SDK geïnstalleerd is
- Run `dotnet restore` om packages te herstellen
- Check of alle bestanden correct zijn aangemaakt

## API Endpoints Gebruikt

- `GET /api/cars` - Haalt alle auto's op
- `GET /api/cars/{id}` - Haalt specifieke auto op (niet gebruikt in huidige implementatie)
- `GET /api/recommendations/{id}?top=5` - Haalt recommendations voor auto (niet gebruikt in huidige implementatie)
- `POST /api/recommendations/text` - Haalt recommendations op basis van tekst input

## Volgende Stappen (Optioneel)

- Detail pagina voor individuele auto's
- Paginatie voor auto's lijst
- Caching van API responses
- Loading indicators tijdens API calls
- Betere error handling met retry logic


