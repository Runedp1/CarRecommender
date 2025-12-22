# Snelle Fix voor Echte Auto Afbeeldingen

## Probleem
De applicatie gebruikt momenteel Picsum Photos, wat willekeurige foto's geeft (geen echte auto's).

## Snelle Oplossing: Pexels API Integreren

### Stap 1: Krijg Pexels API Key (Gratis)
1. Ga naar: https://www.pexels.com/api/
2. Klik op "Get Started"
3. Maak een gratis account
4. Kopieer je API key

### Stap 2: Voeg API Key toe aan Configuratie

**appsettings.json:**
```json
{
  "ImageSettings": {
    "PexelsApiKey": "jouw-api-key-hier"
  }
}
```

**appsettings.Production.json:**
```json
{
  "ImageSettings": {
    "PexelsApiKey": "jouw-api-key-hier"
  }
}
```

### Stap 3: Update CarRepository om CarImageService te gebruiken

Zie `src/Services/CarImageService.cs` voor de implementatie.

### Stap 4: Test

Na het toevoegen van de API key zouden alle auto's echte auto-afbeeldingen moeten tonen!

## Alternatief: Gebruik Bestaande Image URLs

Als je al image URLs hebt in je CSV data:
- Zorg dat de kolom `image_path` of `image_url` correct wordt gelezen
- De ImageUrl wordt automatisch gebruikt als deze in de CSV staat

## Huidige Status

- ✅ ImageUrl veld toegevoegd aan Car model
- ✅ GenerateImageUrl methode genereert URLs voor alle auto's
- ✅ Frontend toont afbeeldingen met fallback
- ⚠️ Gebruikt momenteel Picsum Photos (geen echte auto's)
- 📝 Pexels API integratie klaar voor implementatie (zie CarImageService.cs)




