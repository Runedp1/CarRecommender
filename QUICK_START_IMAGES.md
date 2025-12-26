# Quick Start: Kaggle Images Implementeren

Snelle gids om auto foto's van Kaggle te implementeren.

## Stap 1: Installeer Vereisten

```bash
pip install kagglehub pandas
```

## Stap 2: Configureer Kaggle API

1. Maak account op [kaggle.com](https://www.kaggle.com)
2. Ga naar Account → API → "Create New Token"
3. Download `kaggle.json`
4. Plaats in:
   - **Windows**: `C:\Users\<jouw_naam>\.kaggle\kaggle.json`
   - **Mac/Linux**: `~/.kaggle/kaggle.json`

## Stap 3: Download Images

```bash
cd tools/scripts
python setup_kaggle_images.py
```

Dit download en kopieert alle images naar `backend/images/`

## Stap 4: Match Images met Auto's

```bash
python match_images_to_cars.py
```

Dit maakt een mapping tussen images en auto's.

## Stap 5: Herstart Backend

De backend API moet worden herstart om de nieuwe images te laden.

```bash
cd backend/CarRecommender.Api
dotnet run
```

## Stap 6: Test

1. Start frontend: `cd frontend/CarRecommender.Web && dotnet run`
2. Ga naar `http://localhost:7000`
3. Zoek een auto - je zou nu echte foto's moeten zien!

## Troubleshooting

### "Kaggle API niet gevonden"
- Check of `kaggle.json` op de juiste locatie staat
- Verifieer credentials

### "Geen images gevonden"
- Check of dataset correct is gedownload
- Bekijk de output van `setup_kaggle_images.py`

### "Images worden niet getoond"
- Check of images in `backend/images/` staan
- Verifieer dat backend static files serveert (`app.UseStaticFiles()`)
- Check browser console voor errors

## Structuur

Na downloaden:
```
backend/
  images/
    {brand}/
      {model}/
        image1.jpg
        image2.jpg
```

De `CarRepository` zoekt automatisch in deze structuur.




