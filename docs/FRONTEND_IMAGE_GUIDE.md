# Frontend Image Integration Guide

## Overzicht
De auto database bevat nu image informatie gekoppeld voor **52.6% van de auto's** (10,927 van 20,755 auto's). Dit document legt uit hoe je deze afbeeldingen kan gebruiken in je frontend.

## Database Kolommen
De volgende kolommen zijn toegevoegd aan `Cleaned_Car_Data_For_App_Fully_Enriched.csv`:

- **Image_ID**: Unieke identifier voor de afbeelding (bijv. "2_1$$4$$1")
- **Image_name**: Bestandsnaam van de afbeelding (bijv. "Abarth$$124 Spider$$2017$$Blue$$2_1$$4$$image_1.jpg")
- **Genmodel_ID**: Generieke model ID (bijv. "2_1")
- **Predicted_viewpoint**: Hoek van de foto in graden (0, 45, 90, 135, 180, 225, 270, 315, 360)
- **Image_Path**: Volledige pad naar de afbeelding (voor direct gebruik)

## Image Locatie Structuur

### Optie 1: Lokale Images Folder
Als je de afbeeldingen lokaal hebt opgeslagen:

```
/images/
  /cars/
    /{Genmodel_ID}/
      {Image_name}
```

Voorbeeld:
- Genmodel_ID: `2_1`
- Image_name: `Abarth$$124 Spider$$2017$$Blue$$2_1$$4$$image_1.jpg`
- Volledig pad: `/images/cars/2_1/Abarth$$124 Spider$$2017$$Blue$$2_1$$4$$image_1.jpg`

### Optie 2: Cloud Storage (CDN)
Als je afbeeldingen op een CDN staan (AWS S3, Azure Blob, etc.):

```
https://your-cdn.com/images/cars/{Genmodel_ID}/{Image_name}
```

## Frontend Implementatie

### HTML/React Voorbeeld
```html
<!-- Eenvoudige HTML -->
<img 
  src={car.Image_Path ? `/images/cars/${car.Genmodel_ID}/${car.Image_name}` : '/images/default-car.jpg'} 
  alt={`${car.merk} ${car.model} ${car.bouwjaar}`}
  onError={(e) => { e.target.src = '/images/default-car.jpg'; }}
/>
```

```jsx
// React Component
function CarCard({ car }) {
  const imageUrl = car.Image_Path 
    ? `/images/cars/${car.Genmodel_ID}/${car.Image_name}`
    : '/images/default-car.jpg';
  
  return (
    <div className="car-card">
      <img 
        src={imageUrl}
        alt={`${car.merk} ${car.model} ${car.bouwjaar}`}
        onError={(e) => e.target.src = '/images/default-car.jpg'}
      />
      <h3>{car.merk} {car.model}</h3>
      <p>Jaar: {car.bouwjaar}</p>
      <p>Prijs: €{car.prijs.toLocaleString()}</p>
    </div>
  );
}
```

### C# API Voorbeeld (voor Image Path generatie)
```csharp
public class CarWithImage
{
    public Car Car { get; set; }
    public string ImageUrl { get; set; }
    
    public static string GetImageUrl(Car car, string baseUrl = "/images/cars/")
    {
        if (string.IsNullOrEmpty(car.Genmodel_ID) || string.IsNullOrEmpty(car.Image_name))
            return "/images/default-car.jpg";
            
        return $"{baseUrl}{car.Genmodel_ID}/{car.Image_name}";
    }
}
```

## Recommendations Display

Wanneer je recommendations toont, kan je de afbeeldingen als volgt gebruiken:

```jsx
function RecommendationList({ recommendations }) {
  return (
    <div className="recommendations">
      <h2>Aanbevolen Auto's</h2>
      {recommendations.map((rec, index) => (
        <div key={index} className="recommendation-card">
          <img 
            src={rec.Car.Image_Path || '/images/default-car.jpg'}
            alt={`${rec.Car.Brand} ${rec.Car.Model}`}
          />
          <div className="car-info">
            <h3>{rec.Car.Brand} {rec.Car.Model}</h3>
            <p>Similarity: {(rec.SimilarityScore * 100).toFixed(1)}%</p>
            <p>Prijs: €{rec.Car.Budget.toLocaleString()}</p>
          </div>
        </div>
      ))}
    </div>
  );
}
```

## Fallback Handling

Aangezien 47.4% van de auto's geen afbeelding heeft, is het belangrijk om:

1. **Default Image**: Gebruik een standaard placeholder afbeelding
2. **Error Handling**: Gebruik `onError` events om te fallbacken
3. **Null Checks**: Check altijd of `Image_Path`, `Genmodel_ID`, of `Image_name` bestaat

```javascript
function getCarImage(car) {
  // Prioriteit 1: Image_Path (als beschikbaar)
  if (car.Image_Path && car.Image_Path !== 'None') {
    return `/images/cars/${car.Genmodel_ID}/${car.Image_name}`;
  }
  
  // Prioriteit 2: Genmodel_ID + Image_name
  if (car.Genmodel_ID && car.Image_name) {
    return `/images/cars/${car.Genmodel_ID}/${car.Image_name}`;
  }
  
  // Fallback: Default image
  return '/images/default-car.jpg';
}
```

## Image Optimalisatie Tips

1. **Lazy Loading**: Gebruik lazy loading voor betere performance
   ```html
   <img loading="lazy" src={imageUrl} />
   ```

2. **Responsive Images**: Gebruik verschillende sizes voor verschillende schermen
   ```html
   <img 
     srcSet={`${imageUrl}?w=300 300w, ${imageUrl}?w=600 600w`}
     sizes="(max-width: 600px) 300px, 600px"
     src={imageUrl}
   />
   ```

3. **Caching**: Zet cache headers op je image server voor snellere laadtijden

## Statistieken

- **Totaal auto's**: 20,755
- **Met afbeelding**: 10,927 (52.6%)
- **Zonder afbeelding**: 9,828 (47.4%)
- **Unieke Genmodel_IDs**: 865 verschillende modellen

## Volgende Stappen

1. Download of organiseer de afbeeldingen in de juiste folder structuur
2. Implementeer fallback images voor auto's zonder afbeelding
3. Test de image loading in je frontend
4. Overweeg image compression/optimization voor betere performance

## Notities

- Image_name bevat soms `$$` scheidingstekens - deze moeten in het pad behouden blijven
- De afbeeldingen zijn geselecteerd op basis van kwaliteit (Quality_check) en viewpoint (45° heeft prioriteit)
- Voor auto's zonder exacte match (merk+model+jaar) wordt een match zonder jaar gebruikt
