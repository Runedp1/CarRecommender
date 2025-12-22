"""
Script om te controleren hoeveel auto foto's uit de Kaggle dataset 
overeenkomen met auto's in onze database.

Dit script:
1. Download de Kaggle cars-image-dataset
2. Leest onze database CSV (Cleaned_Car_Data_For_App_Fully_Enriched.csv)
3. Leest Image_table.csv om te zien welke images beschikbaar zijn
4. Matcht images uit Kaggle dataset met auto's in database
5. Rapporteert hoeveel matches gevonden zijn
"""
import kagglehub
import pandas as pd
import os
from pathlib import Path
import re
from collections import defaultdict

def normalize_string(s):
    """Normaliseer string voor matching (lowercase, verwijder speciale tekens)"""
    if pd.isna(s) or s == '':
        return ''
    s = str(s).lower().strip()
    # Verwijder speciale tekens en extra spaties
    s = re.sub(r'[^\w\s]', '', s)
    s = re.sub(r'\s+', ' ', s)
    return s

def parse_image_name(image_name):
    """Parse Image_name format: Brand$$Model$$Year$$Color$$Genmodel_ID$$...$$image_X.jpg"""
    if pd.isna(image_name) or image_name == '':
        return None
    
    parts = str(image_name).split('$$')
    if len(parts) < 3:
        return None
    
    return {
        'brand': normalize_string(parts[0]),
        'model': normalize_string(parts[1]),
        'year': parts[2] if len(parts) > 2 else None
    }

def find_image_files(kaggle_path):
    """Vind alle image bestanden in de Kaggle dataset"""
    kaggle_path = Path(kaggle_path)
    image_files = []
    
    print(f"Zoeken naar image bestanden in: {kaggle_path}")
    
    # Zoek recursief naar alle .jpg en .png bestanden
    for ext in ['*.jpg', '*.jpeg', '*.png']:
        files = list(kaggle_path.rglob(ext))
        image_files.extend(files)
        print(f"  Gevonden {len(files)} {ext} bestanden")
    
    return image_files

def match_images_to_cars(kaggle_dataset_path, database_csv="data/Cleaned_Car_Data_For_App_Fully_Enriched.csv", 
                        image_table_csv="data/Image_table.csv"):
    """
    Match Kaggle auto-afbeeldingen aan auto's in de database.
    
    Returns:
        dict met statistieken over matches
    """
    print("=" * 80)
    print("MATCHING KAGGLE IMAGES MET DATABASE")
    print("=" * 80)
    
    # 1. Download Kaggle dataset
    print("\n1. Downloaden Kaggle dataset...")
    try:
        path = kagglehub.dataset_download("kshitij192/cars-image-dataset")
        print(f"   Dataset gedownload naar: {path}")
        kaggle_path = Path(path)
    except Exception as e:
        print(f"   FOUT bij downloaden: {e}")
        return None
    
    # 2. Lees database CSV
    print("\n2. Lezen database CSV...")
    try:
        df_cars = pd.read_csv(database_csv)
        print(f"   Gevonden {len(df_cars)} auto's in database")
        print(f"   Kolommen: {list(df_cars.columns)}")
    except Exception as e:
        print(f"   FOUT bij lezen database CSV: {e}")
        return None
    
    # 3. Lees Image_table.csv
    print("\n3. Lezen Image_table.csv...")
    try:
        df_images = pd.read_csv(image_table_csv)
        print(f"   Gevonden {len(df_images)} image records")
        print(f"   Kolommen: {list(df_images.columns)}")
    except Exception as e:
        print(f"   FOUT bij lezen Image_table.csv: {e}")
        return None
    
    # 4. Parse image names uit Image_table
    print("\n4. Parsen image namen...")
    image_info = []
    for idx, row in df_images.iterrows():
        image_name = row.get('Image_name', '')
        parsed = parse_image_name(image_name)
        if parsed:
            parsed['image_id'] = row.get('Image_ID', '')
            parsed['genmodel_id'] = row.get('Genmodel_ID', '')
            parsed['original_name'] = image_name
            image_info.append(parsed)
    
    print(f"   {len(image_info)} image namen succesvol geparsed")
    
    # 5. Normaliseer database auto's voor matching
    print("\n5. Normaliseren database auto's...")
    cars_normalized = []
    for idx, row in df_cars.iterrows():
        car_info = {
            'id': row.get('merk', '') if 'merk' in df_cars.columns else idx,
            'brand': normalize_string(row.get('merk', '')),
            'model': normalize_string(row.get('model', '')),
            'year': str(row.get('bouwjaar', '')) if pd.notna(row.get('bouwjaar', '')) else None,
            'image_id': str(row.get('Image_ID', '')) if pd.notna(row.get('Image_ID', '')) else None,
            'genmodel_id': str(row.get('Genmodel_ID', '')) if pd.notna(row.get('Genmodel_ID', '')) else None,
            'original_brand': str(row.get('merk', '')),
            'original_model': str(row.get('model', ''))
        }
        cars_normalized.append(car_info)
    
    print(f"   {len(cars_normalized)} auto's genormaliseerd")
    
    # 6. Match images met auto's
    print("\n6. Matchen images met auto's...")
    matches = []
    match_stats = {
        'exact_matches': 0,  # Brand + Model + Year match
        'brand_model_matches': 0,  # Brand + Model match (zonder year)
        'brand_only_matches': 0,  # Alleen Brand match
        'no_matches': 0,
        'cars_with_images': set(),
        'images_matched': set()
    }
    
    for car in cars_normalized:
        car_matched = False
        
        for img in image_info:
            # Exact match: brand + model + year
            if (car['brand'] == img['brand'] and 
                car['model'] == img['model'] and 
                car['year'] == img['year']):
                matches.append({
                    'car_id': car['id'],
                    'car_brand': car['original_brand'],
                    'car_model': car['original_model'],
                    'car_year': car['year'],
                    'image_id': img['image_id'],
                    'genmodel_id': img['genmodel_id'],
                    'match_type': 'exact',
                    'image_name': img['original_name']
                })
                match_stats['exact_matches'] += 1
                match_stats['cars_with_images'].add(car['id'])
                match_stats['images_matched'].add(img['image_id'])
                car_matched = True
                break
        
        if not car_matched:
            # Brand + Model match (zonder year)
            for img in image_info:
                if (car['brand'] == img['brand'] and 
                    car['model'] == img['model']):
                    matches.append({
                        'car_id': car['id'],
                        'car_brand': car['original_brand'],
                        'car_model': car['original_model'],
                        'car_year': car['year'],
                        'image_id': img['image_id'],
                        'genmodel_id': img['genmodel_id'],
                        'match_type': 'brand_model',
                        'image_name': img['original_name']
                    })
                    match_stats['brand_model_matches'] += 1
                    match_stats['cars_with_images'].add(car['id'])
                    match_stats['images_matched'].add(img['image_id'])
                    car_matched = True
                    break
        
        if not car_matched:
            # Alleen Brand match
            for img in image_info:
                if car['brand'] == img['brand']:
                    matches.append({
                        'car_id': car['id'],
                        'car_brand': car['original_brand'],
                        'car_model': car['original_model'],
                        'car_year': car['year'],
                        'image_id': img['image_id'],
                        'genmodel_id': img['genmodel_id'],
                        'match_type': 'brand_only',
                        'image_name': img['original_name']
                    })
                    match_stats['brand_only_matches'] += 1
                    match_stats['cars_with_images'].add(car['id'])
                    match_stats['images_matched'].add(img['image_id'])
                    car_matched = True
                    break
        
        if not car_matched:
            match_stats['no_matches'] += 1
    
    # 7. Check of image bestanden daadwerkelijk bestaan in Kaggle dataset
    print("\n7. Controleren of image bestanden bestaan in Kaggle dataset...")
    image_files = find_image_files(kaggle_path)
    image_filenames = {f.name.lower() for f in image_files}
    
    existing_images = 0
    missing_images = 0
    for match in matches:
        image_name = match['image_name']
        if image_name:
            filename = image_name.split('$$')[-1] if '$$' in image_name else image_name
            if filename.lower() in image_filenames:
                existing_images += 1
            else:
                missing_images += 1
    
    # 8. Print resultaten
    print("\n" + "=" * 80)
    print("RESULTATEN")
    print("=" * 80)
    print(f"\nTotaal auto's in database: {len(cars_normalized)}")
    print(f"Totaal images in Image_table: {len(image_info)}")
    print(f"Totaal image bestanden in Kaggle dataset: {len(image_files)}")
    
    print(f"\n--- MATCHES ---")
    print(f"Exacte matches (Brand + Model + Year): {match_stats['exact_matches']}")
    print(f"Brand + Model matches (zonder Year): {match_stats['brand_model_matches']}")
    print(f"Alleen Brand matches: {match_stats['brand_only_matches']}")
    print(f"Geen matches: {match_stats['no_matches']}")
    print(f"\nTotaal auto's met matches: {len(match_stats['cars_with_images'])}")
    print(f"Totaal unieke images gematcht: {len(match_stats['images_matched'])}")
    
    print(f"\n--- IMAGE BESTANDEN ---")
    print(f"Matched images die bestaan in dataset: {existing_images}")
    print(f"Matched images die NIET bestaan in dataset: {missing_images}")
    
    # Percentage berekenen
    total_cars = len(cars_normalized)
    cars_with_matches = len(match_stats['cars_with_images'])
    percentage = (cars_with_matches / total_cars * 100) if total_cars > 0 else 0
    
    print(f"\n--- SAMENVATTING ---")
    print(f"Percentage auto's met image match: {percentage:.2f}% ({cars_with_matches}/{total_cars})")
    
    # Toon voorbeelden van matches
    if matches:
        print(f"\n--- VOORBEELDEN VAN MATCHES ---")
        for i, match in enumerate(matches[:10]):
            print(f"{i+1}. {match['car_brand']} {match['car_model']} ({match['car_year']}) "
                  f"-> {match['image_name']} [{match['match_type']}]")
        if len(matches) > 10:
            print(f"... en {len(matches) - 10} meer matches")
    
    # Toon voorbeelden van auto's zonder matches
    if match_stats['no_matches'] > 0:
        print(f"\n--- VOORBEELDEN VAN AUTO'S ZONDER MATCHES ---")
        no_match_cars = [c for c in cars_normalized if c['id'] not in match_stats['cars_with_images']]
        for i, car in enumerate(no_match_cars[:10]):
            print(f"{i+1}. {car['original_brand']} {car['original_model']} ({car['year']})")
        if len(no_match_cars) > 10:
            print(f"... en {len(no_match_cars) - 10} meer auto's zonder matches")
    
    return {
        'matches': matches,
        'stats': match_stats,
        'total_cars': total_cars,
        'total_images': len(image_info),
        'total_image_files': len(image_files),
        'existing_images': existing_images,
        'missing_images': missing_images,
        'percentage_matched': percentage
    }

if __name__ == "__main__":
    result = match_images_to_cars(None)  # None omdat we downloaden in de functie
    if result:
        print("\n✓ Analyse voltooid!")
    else:
        print("\n✗ Analyse mislukt!")

