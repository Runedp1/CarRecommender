# Kaggle Images Dataset Setup Guide

Deze gids helpt je om de Kaggle Car Connection Picture Dataset te integreren in je applicatie.

## Vereisten

1. **Kaggle Account**: Maak een account op [kaggle.com](https://www.kaggle.com)
2. **Kaggle API**: Installeer en configureer Kaggle API credentials
3. **Python**: Python 3.7+ met pip

## Stap 1: Kaggle API Setup

### 1.1 Installeer Kaggle Hub

```bash
pip install kagglehub
```

### 1.2 Configureer Kaggle Credentials

1. Ga naar je Kaggle account settings: https://www.kaggle.com/account
2. Scroll naar "API" sectie
3. Klik op "Create New Token" - dit download `kaggle.json`
4. Plaats dit bestand in:
   - **Windows**: `C:\Users\<username>\.kaggle\kaggle.json`
   - **Linux/Mac**: `~/.kaggle/kaggle.json`

### 1.3 Verifieer Setup

```python
import kagglehub
path = kagglehub.dataset_download("prondeau/the-car-connection-picture-dataset")
print(f"Dataset gedownload naar: {path}")
```

## Stap 2: Download Images

### Optie A: Via Python Script (Aanbevolen)

```bash
cd tools/scripts
python download_kaggle_images.py
```

Dit script:
- Download de dataset van Kaggle
- Vindt de images directory
- Kopieert images naar `backend/images/`
- Maakt een mapping bestand

### Optie B: Handmatig

1. Download de dataset van Kaggle:
   - Ga naar: https://www.kaggle.com/datasets/prondeau/the-car-connection-picture-dataset
   - Klik op "Download"
   - Extract het ZIP bestand

2. Kopieer images naar project:
   ```bash
   # Vind de images directory in de gedownloade dataset
   # Kopieer naar backend/images/
   ```

## Stap 3: Match Images met Auto's

Run het matching script:

```bash
cd tools/scripts
python match_images_to_cars.py
```

Dit script:
- Laadt de CSV data
- Analyseert image bestandsnamen
- Matcht images met auto's op basis van merk/model/jaar
- Maakt een mapping JSON bestand

## Stap 4: Update CarRepository

De `CarRepository` moet worden aangepast om de mapping te gebruiken. 

**Huidige implementatie:**
- Zoekt images in `images/{brand}/{model}/{id}.jpg`
- Gebruikt fallback naar externe URLs

**Nieuwe implementatie:**
- Gebruikt de mapping JSON om juiste image te vinden
- Valideert dat image bestaat
- Gebruikt fallback als image niet gevonden

## Stap 5: Configureer Static Files

Zorg dat de backend static files serveert:

**In `backend/CarRecommender.Api/Program.cs`:**
```csharp
app.UseStaticFiles(); // Al aanwezig
```

**In `appsettings.json`:**
```json
{
  "StaticFiles": {
    "RequestPath": "/images",
    "PhysicalPath": "./images"
  }
}
```

## Stap 6: Test

1. Start de backend API
2. Test een endpoint die images retourneert
3. Check of images correct worden geladen in frontend

## Image Structuur

De Kaggle dataset heeft waarschijnlijk een structuur zoals:

```
images/
  {brand}/
    {model}/
      {year}/
        image_1.jpg
        image_2.jpg
```

Of:

```
images/
  {genmodel_id}/
    {image_name}.jpg
```

Het matching script analyseert de structuur automatisch.

## Troubleshooting

### Probleem: Kaggle API werkt niet
**Oplossing:**
- Check of `kaggle.json` op de juiste locatie staat
- Verifieer dat credentials correct zijn
- Check internet verbinding

### Probleem: Images worden niet gevonden
**Oplossing:**
- Check of images in `backend/images/` staan
- Verifieer dat static files correct geconfigureerd zijn
- Check de mapping JSON voor correcte paths

### Probleem: Geen matches gevonden
**Oplossing:**
- Review de image bestandsnamen
- Pas het matching algoritme aan in `match_images_to_cars.py`
- Check of merk/model namen consistent zijn tussen CSV en images

## Volgende Stappen

1. ✅ Download Kaggle dataset
2. ✅ Match images met auto's
3. ⏳ Update CarRepository om mapping te gebruiken
4. ⏳ Test image loading
5. ⏳ Optimaliseer image paths voor productie

## Notities

- De dataset kan groot zijn (>1GB) - zorg voor voldoende schijfruimte
- Images kunnen verschillende formaten hebben (jpg, png)
- Overweeg image compression voor productie
- Cache images voor betere performance
