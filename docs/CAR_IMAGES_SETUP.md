# Auto Afbeeldingen Setup

## Huidige Situatie

De applicatie gebruikt momenteel **Picsum Photos** voor afbeeldingen, wat willekeurige foto's geeft (geen specifieke auto's).

## Oplossing: Echte Auto Afbeeldingen

Om echte auto-afbeeldingen te krijgen, zijn er verschillende opties:

### Optie 1: Pexels API (Aanbevolen)

**Voordelen:**
- Gratis API key
- Grote collectie auto foto's
- Goede kwaliteit
- Eenvoudig te integreren

**Stappen:**

1. **Registreer bij Pexels:**
   - Ga naar: https://www.pexels.com/api/
   - Maak een gratis account
   - Krijg je API key

2. **Voeg API key toe aan appsettings.json:**
   ```json
   {
     "ImageSettings": {
       "PexelsApiKey": "jouw-api-key-hier"
     }
   }
   ```

3. **Maak CarImageService:**
   - Zie `src/Services/CarImageService.cs` (te implementeren)
   - Gebruik HttpClient om Pexels API aan te roepen
   - Cache resultaten om API calls te beperken

4. **Update CarRepository:**
   - Gebruik CarImageService in plaats van GenerateImageUrl
   - Roep Pexels API aan met search query: `{brand} {model} car`

### Optie 2: Pixabay API

**Voordelen:**
- Gratis API key
- Meer dan 70.000 auto foto's
- Goede kwaliteit

**Stappen:** Vergelijkbaar met Pexels

### Optie 3: Eigen Image Database

**Voordelen:**
- Volledige controle
- Geen API limits
- Specifieke auto's

**Stappen:**
- Upload auto afbeeldingen naar Azure Blob Storage
- Genereer URLs op basis van merk+model
- Update ImageUrl in database/CSV

## Implementatie Voorbeeld (Pexels)

```csharp
// In CarImageService.cs
public async Task<string> GetCarImageUrlAsync(Car car)
{
    if (string.IsNullOrWhiteSpace(car.Brand) || string.IsNullOrWhiteSpace(car.Model))
        return string.Empty;

    string searchQuery = $"{car.Brand} {car.Model} car";
    string apiKey = _configuration["ImageSettings:PexelsApiKey"];
    
    var response = await _httpClient.GetAsync(
        $"https://api.pexels.com/v1/search?query={searchQuery}&per_page=1",
        headers: { "Authorization" = apiKey }
    );
    
    var json = await response.Content.ReadFromJsonAsync<PexelsResponse>();
    return json?.Photos?.FirstOrDefault()?.Src?.Medium ?? string.Empty;
}
```

## Huidige Workaround

Totdat een echte auto image API is geïntegreerd, gebruikt de applicatie:
- **Picsum Photos** met deterministische seed (consistente afbeeldingen per auto)
- **Placeholder fallback** als afbeelding niet laadt

Dit geeft altijd een werkende afbeelding, maar niet specifiek auto's.




