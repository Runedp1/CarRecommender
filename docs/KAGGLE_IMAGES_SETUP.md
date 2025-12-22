# Kaggle Auto Images Setup

## Overzicht

Deze guide legt uit hoe je de Kaggle `cars-image-dataset` gebruikt om echte auto-afbeeldingen toe te voegen aan de applicatie.

## Stap 1: Kaggle Credentials Instellen

1. Ga naar https://www.kaggle.com/account
2. Scroll naar "API" sectie
3. Klik op "Create New Token"
4. Download `kaggle.json`
5. Plaats het bestand in:
   - **Windows**: `C:\Users\<username>\.kaggle\kaggle.json`
   - **Linux/Mac**: `~/.kaggle/kaggle.json`

## Stap 2: Kagglehub Installeren

```bash
pip install kagglehub
```

## Stap 3: Dataset Downloaden en Koppelen

Run het setup script:

```bash
python scripts/setup_car_images.py
```

Dit script doet het volgende:
1. ✅ Download de Kaggle dataset (`kshitij192/cars-image-dataset`)
2. ✅ Verkent de dataset structuur
3. ✅ Koppelt afbeeldingen aan auto's op basis van `Image_table.csv`
4. ✅ Kopieert afbeeldingen naar `images/{brand}/{model}/` structuur

## Stap 4: Verificatie

Controleer of afbeeldingen zijn gekopieerd:

```bash
# Windows PowerShell
Get-ChildItem images -Recurse -Filter *.jpg | Measure-Object | Select-Object Count

# Linux/Mac
find images -name "*.jpg" | wc -l
```

## Stap 5: Applicatie Herstarten

Na het kopiëren van afbeeldingen:

1. Herstart de API server
2. Herstart de Web app
3. Test de applicatie - je zou nu echte auto-afbeeldingen moeten zien!

## Hoe het Werkt

### Image Koppeling

De applicatie gebruikt `Image_table.csv` om afbeeldingen te koppelen:

- **Genmodel_ID**: Unieke ID voor merk+model combinatie (bijv. `2_1` voor Abarth 124 Spider)
- **Image_name**: Bestandsnaam in format `Brand$$Model$$Year$$Color$$Genmodel_ID$$...$$image_X.jpg`
- **Image_Path**: Pad naar afbeelding in `images/{brand}/{model}/{genmodel_id}.jpg`

### ImageUrl Generatie

De applicatie probeert in deze volgorde:

1. **Lokale afbeelding** (als `images/{brand}/{model}/{id}.jpg` bestaat)
   - URL: `/images/{brand}/{model}/{id}.jpg`
   
2. **Externe service** (fallback)
   - Auto-Data.net of Picsum Photos

## Handmatige Setup (Alternatief)

Als het script niet werkt, kun je handmatig:

1. Download de dataset van Kaggle
2. Extract de afbeeldingen
3. Kopieer naar `images/{brand}/{model}/` structuur
4. Gebruik `Genmodel_ID` als bestandsnaam

## Troubleshooting

### "Kaggle credentials not found"
- Zorg dat `kaggle.json` in de juiste directory staat
- Check of de API token geldig is

### "No images found"
- Check of de dataset correct is gedownload
- Verifieer de structuur van de dataset
- Check of `Image_table.csv` correct wordt gelezen

### "Images not showing in app"
- Check of afbeeldingen in `images/` directory staan
- Verifieer dat de Web app static files serveert (`app.UseStaticFiles()`)
- Check browser console voor 404 errors

## Dataset Info

- **Dataset**: `kshitij192/cars-image-dataset`
- **Source**: Kaggle
- **License**: Check Kaggle dataset page voor licentie details
- **Images**: Auto-afbeeldingen georganiseerd per merk/model

## Next Steps

Na setup:
- ✅ Afbeeldingen worden automatisch gebruikt als ze bestaan
- ✅ Fallback naar externe services als lokale afbeeldingen niet bestaan
- ✅ Geen code changes nodig - alles werkt automatisch!




