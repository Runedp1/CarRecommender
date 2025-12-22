# Kaggle Auto Images - Quick Start

## Wat je nodig hebt

De Kaggle dataset `kshitij192/cars-image-dataset` bevat echte auto-afbeeldingen die je kunt gebruiken.

## Snelle Setup (3 stappen)

### 1. Kaggle Credentials

```bash
# Maak .kaggle directory (als deze niet bestaat)
mkdir ~/.kaggle  # Linux/Mac
# of
mkdir C:\Users\<username>\.kaggle  # Windows

# Download kaggle.json van https://www.kaggle.com/account
# Plaats het in de .kaggle directory
```

### 2. Installeer Kagglehub

```bash
pip install kagglehub
```

### 3. Download en Koppel Images

```bash
python scripts/setup_car_images.py
```

Dit script:
- ✅ Download de dataset van Kaggle
- ✅ Koppelt afbeeldingen aan auto's via `Image_table.csv`
- ✅ Kopieert afbeeldingen naar `images/{brand}/{model}/` structuur

## Hoe het Werkt

Na het runnen van het script:

1. **Afbeeldingen staan in `images/` directory**
   - Structuur: `images/{brand}/{model}/{genmodel_id}.jpg`
   - Bijvoorbeeld: `images/abarth/124_spider/2_1.jpg`

2. **Applicatie gebruikt automatisch lokale afbeeldingen**
   - Als afbeelding bestaat → gebruikt lokale afbeelding
   - Als afbeelding niet bestaat → gebruikt externe service (fallback)

3. **Geen code changes nodig!**
   - De applicatie detecteert automatisch of afbeeldingen bestaan
   - ImageUrl wordt automatisch gegenereerd

## Testen

1. Herstart de API: `cd CarRecommender.Api && dotnet run`
2. Herstart de Web app: `cd CarRecommender.Api/CarRecommender.Web && dotnet run`
3. Open `http://localhost:7000`
4. Zoek naar auto's - je zou nu echte afbeeldingen moeten zien!

## Troubleshooting

**"Kaggle credentials not found"**
- Check of `kaggle.json` in de juiste directory staat
- Verifieer dat de API token geldig is

**"No images copied"**
- Check of `Image_table.csv` correct wordt gelezen
- Verifieer de dataset structuur

**"Images not showing"**
- Check of afbeeldingen in `images/` directory staan
- Check browser console voor 404 errors
- Verifieer dat static files worden geserveerd

## Meer Info

Zie `docs/KAGGLE_IMAGES_SETUP.md` voor uitgebreide documentatie.




