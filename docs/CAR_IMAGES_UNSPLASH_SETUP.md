# Auto Foto's Instellen met Unsplash API

## Probleem
De applicatie gebruikt momenteel Picsum Photos voor placeholder afbeeldingen, maar deze zijn niet specifiek auto foto's.

## Oplossing: Unsplash API (Gratis)

Unsplash biedt een gratis API met duizenden professionele auto foto's. Dit is **100% gratis** en legaal.

### Stap 1: Maak een Unsplash Account

1. Ga naar https://unsplash.com/developers
2. Klik op "Register as a developer"
3. Maak een gratis account (of log in met bestaand account)

### Stap 2: Maak een Application

1. Ga naar https://unsplash.com/oauth/applications
2. Klik op "New Application"
3. Vul in:
   - **Application name**: Car Recommender (of wat je wilt)
   - **Description**: Car recommendation system
   - **Website URL**: http://localhost:7000 (of je productie URL)
4. Accepteer de terms
5. Klik "Create application"

### Stap 3: Kopieer je Access Key

1. Na het aanmaken zie je je **Access Key** en **Secret Key**
2. Je hebt alleen de **Access Key** nodig
3. Kopieer deze key

### Stap 4: Voeg toe aan appsettings.json

Open `frontend/CarRecommender.Web/appsettings.Development.json` en voeg toe:

```json
{
  "ImageSettings": {
    "UnsplashAccessKey": "jouw-access-key-hier"
  },
  "ApiSettings": {
    "BaseUrl": "http://localhost:5283"
  }
}
```

### Stap 5: Herstart de Applicatie

```powershell
cd frontend\CarRecommender.Web
dotnet run
```

## Hoe het Werkt

- De applicatie gebruikt nu Unsplash API om echte auto foto's op te halen
- Foto's worden gezocht op basis van merk en model (bijv. "Audi A4 car")
- Elke auto krijgt een unieke, relevante foto
- Als Unsplash niet beschikbaar is, valt het terug op de SVG icon fallback

## Rate Limits

Unsplash gratis tier heeft:
- **50 requests per uur** per Access Key
- Dit is ruim voldoende voor development en kleine productie gebruik
- Voor meer requests: upgrade naar betaalde tier (niet nodig voor dit project)

## Alternatieven (als Unsplash niet werkt)

### Optie 1: Pexels API (ook gratis)
1. Ga naar https://www.pexels.com/api/
2. Maak account en krijg API key
3. Voeg toe aan configuratie (code aanpassing nodig)

### Optie 2: Pixabay API (ook gratis)
1. Ga naar https://pixabay.com/api/docs/
2. Maak account en krijg API key
3. Voeg toe aan configuratie (code aanpassing nodig)

## Troubleshooting

**Probleem**: Foto's verschijnen niet
- Controleer of Access Key correct is in appsettings.json
- Controleer browser console (F12) voor errors
- Controleer of rate limit niet is bereikt

**Probleem**: Verkeerde foto's
- Unsplash zoekt op "merk model car"
- Sommige merken/modellen hebben mogelijk geen exacte match
- Dit is normaal - Unsplash geeft de beste match

## Code Aanpassingen Nodig

Na het toevoegen van de Access Key, moet de code worden aangepast om de Unsplash API te gebruiken. 
De huidige implementatie gebruikt Picsum Photos als fallback.

Zie `frontend/CarRecommender.Web/Pages/Index.cshtml.cs` voor de `GetCarImageUrl` methode.










