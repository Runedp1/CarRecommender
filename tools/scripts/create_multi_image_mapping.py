"""
Script om een mapping te maken waarbij elke auto ALLE images krijgt die matchen
(in plaats van alleen de eerste).

Het resultaat is een JSON bestand met:
{
  "car_id": ["/images/image1.jpg", "/images/image2.jpg", ...]
}

Dit script gebruikt dezelfde logica als match_images_to_new_dataset.py
om te matchen, maar groepeert alle matches per auto.
"""
import csv
import json
import os
import sys
import re
from collections import defaultdict

script_dir = os.path.dirname(os.path.abspath(__file__))
project_root = os.path.dirname(os.path.dirname(script_dir))
csv_path = os.path.join(project_root, 'backend', 'data', 'df_master_v8_def.csv')
images_dir = os.path.join(project_root, 'backend', 'images')
mapping_output = os.path.join(project_root, 'backend', 'data', 'car_image_mapping.json')

def normalize_name(name):
    if not name:
        return ""
    return str(name).lower().strip().replace(' ', '').replace('-', '').replace('_', '')

print("=" * 80)
print("MULTI-IMAGE MAPPING SCRIPT - ALLE IMAGES PER AUTO")
print("=" * 80)
print(f"CSV pad: {csv_path}")
print(f"Images directory: {images_dir}")
print(f"Mapping output: {mapping_output}")
print()

# STAP 1: Laad CSV en verwijder duplicaten (zelfde logica als CarRepository)
print("STAP 1: Laden van CSV en verwijderen duplicaten...")
try:
    unique_cars = []
    
    with open(csv_path, 'r', encoding='utf-8') as f:
        reader = csv.DictReader(f)
        
        # Zoek kolommen
        brand_col = 'merk'
        model_col = 'model'
        price_col = 'prijs'
        year_col = 'bouwjaar'
        
        # Groepeer auto's op Brand|Model
        cars_by_key = defaultdict(list)
        row_number = 0
        
        for row in reader:
            row_number += 1
            brand = (row.get(brand_col, '') or '').strip()
            model = (row.get(model_col, '') or '').strip()
            price_str = (row.get(price_col, '') or '').strip()
            year_str = (row.get(year_col, '') or '').strip()
            
            if not brand or not model:
                continue
            
            try:
                price_clean = ''.join(c for c in price_str if c.isdigit() or c in '.,')
                price_clean = price_clean.replace(',', '.')
                price = float(price_clean) if price_clean else 0
            except:
                price = 0
            
            try:
                year = int(year_str) if year_str else None
            except:
                year = None
            
            unique_key = f"{brand.lower()}|{model.lower()}"
            cars_by_key[unique_key].append({
                'brand': brand,
                'model': model,
                'price': price,
                'year': year,
                'row': row_number
            })
        
        print(f"  Totaal rijen verwerkt: {row_number}")
        print(f"  Unieke Brand|Model combinaties: {len(cars_by_key)}")
        
        # STAP 1B: Laad ALLE auto's eerst met IDs (zoals CarRepository doet)
        all_cars_with_ids = []
        for i in range(1, row_number + 1):
            # Zoek de auto die bij rij i hoort
            for unique_key, cars_list in cars_by_key.items():
                for car in cars_list:
                    if car['row'] == i:
                        car['id'] = i  # Gebruik rijnummer als ID (zoals CarRepository)
                        all_cars_with_ids.append(car)
                        break
        
        # STAP 1C: Verwijder duplicaten (behoud hoogste prijs) - zoals RemoveDuplicates doet
        unique_cars_dict = {}
        for car in all_cars_with_ids:
            unique_key = f"{car['brand'].lower()}|{car['model'].lower()}"
            
            if unique_key not in unique_cars_dict:
                unique_cars_dict[unique_key] = car
            else:
                existing_car = unique_cars_dict[unique_key]
                existing_price = existing_car.get('price') or 0
                new_price = car.get('price') or 0
                
                if new_price > existing_price:
                    unique_cars_dict[unique_key] = car
        
        unique_cars = list(unique_cars_dict.values())
        
        print(f"  Unieke auto's na duplicaten verwijdering: {len(unique_cars)}")
        print()
        
except Exception as e:
    print(f"FOUT bij het laden van CSV: {e}")
    import traceback
    traceback.print_exc()
    sys.exit(1)

# STAP 2: Laad alle images en indexeer
print("STAP 2: Indexeren van images...")
image_files = []
for file in os.listdir(images_dir):
    if file.lower().endswith('.jpg'):
        image_files.append(file)

print(f"  Totaal images gevonden: {len(image_files)}")

# Indexeer images op basis van Brand|Model en Brand|Model|Year
image_index_brand_model = defaultdict(list)  # Brand|Model index
image_index_with_year = defaultdict(list)    # Brand|Model|Year index

for image_file in image_files:
    name_without_ext = os.path.splitext(image_file)[0]
    parts = name_without_ext.split('_')
    
    if len(parts) >= 2:
        brand = parts[0].strip()
        model = parts[1].strip()
        
        if brand and model:
            brand_norm = normalize_name(brand)
            model_norm = normalize_name(model)
            index_key_bm = f"{brand_norm}|{model_norm}"
            
            image_index_brand_model[index_key_bm].append(image_file)
            
            # Probeer jaar te extraheren
            if len(parts) >= 3:
                try:
                    year_str = parts[2].strip()
                    if year_str.isdigit() and len(year_str) == 4:
                        year = int(year_str)
                        if 1990 <= year <= 2025:
                            index_key_bmy = f"{brand_norm}|{model_norm}|{year}"
                            image_index_with_year[index_key_bmy].append(image_file)
                except:
                    pass

print(f"  Unieke Brand|Model combinaties in images: {len(image_index_brand_model)}")
print(f"  Unieke Brand|Model|Year combinaties in images: {len(image_index_with_year)}")
print()

# STAP 3: Match auto's met images (ALLEEN images van 1 bouwjaar per auto)
print("STAP 3: Matchen van auto's met images (ALLEEN images van 1 bouwjaar per auto)...")
print("  Strategie: 1) Probeer Brand+Model+Year (exact jaar), 2) Zoek beste jaar match (meeste images)")
print("  ALTIJD: Gebruik alleen images van 1 bouwjaar (niet meerdere jaren door elkaar)")
mapping = {}  # car_id -> list van image URLs
matched_count = 0
matched_exact_year = 0
matched_best_year = 0
matched_no_year = 0
unmatched_count = 0
total_images_matched = 0

def extract_year_from_image(image_file):
    """Extract jaar uit image bestandsnaam"""
    name_without_ext = os.path.splitext(image_file)[0]
    parts = name_without_ext.split('_')
    if len(parts) >= 3:
        try:
            year_str = parts[2].strip()
            if year_str.isdigit() and len(year_str) == 4:
                year = int(year_str)
                if 1990 <= year <= 2025:
                    return year
        except:
            pass
    return None

def find_best_year_for_brand_model(brand_norm, model_norm, image_files):
    """Vind het jaar met de meeste images voor deze brand+model combinatie"""
    year_counts = defaultdict(int)
    year_images = defaultdict(list)
    
    for img_file in image_files:
        year = extract_year_from_image(img_file)
        if year:
            year_counts[year] += 1
            year_images[year].append(img_file)
    
    if not year_counts:
        return None, []
    
    # Vind jaar met meeste images
    best_year = max(year_counts.items(), key=lambda x: x[1])[0]
    return best_year, year_images[best_year]

for car in unique_cars:
    car_id = car['id']
    car_brand = car['brand']
    car_model = car['model']
    car_year = car.get('year')
    
    brand_norm = normalize_name(car_brand)
    model_norm = normalize_name(car_model)
    index_key_bm = f"{brand_norm}|{model_norm}"
    
    matched_images = []
    matched = False
    
    # STAP 1: Eerst proberen: exacte match met jaar (als jaar beschikbaar is)
    if car_year and car_year >= 1990 and car_year <= 2025:
        index_key_bmy = f"{brand_norm}|{model_norm}|{car_year}"
        if index_key_bmy in image_index_with_year:
            # Exact jaar match gevonden - gebruik alleen images van dit jaar
            matched_images = image_index_with_year[index_key_bmy]
            matched_exact_year += 1
            matched = True
    
    # STAP 2: Als geen exact jaar match: zoek beste jaar voor Brand+Model
    if not matched_images and index_key_bm in image_index_brand_model:
        all_matching_images = image_index_brand_model[index_key_bm]
        best_year, best_year_images = find_best_year_for_brand_model(brand_norm, model_norm, all_matching_images)
        if best_year_images:
            matched_images = best_year_images
            matched_best_year += 1
            matched = True
    
    # STAP 3: Als nog steeds geen match: probeer gedeeltelijke match en kies beste jaar
    if not matched_images:
        model_norm_no_digits = re.sub(r'\d+', '', model_norm)
        if len(model_norm_no_digits) >= 1:
            for img_key_bm, img_files in image_index_brand_model.items():
                img_brand, img_model = img_key_bm.split('|')
                
                if img_brand == brand_norm:
                    img_model_no_digits = re.sub(r'\d+', '', img_model)
                    
                    if (model_norm.startswith(img_model) or
                        img_model.startswith(model_norm) or
                        (model_norm_no_digits and img_model.startswith(model_norm_no_digits)) or
                        (img_model_no_digits and model_norm.startswith(img_model_no_digits)) or
                        (len(model_norm) >= 2 and img_model.startswith(model_norm[:2])) or
                        (len(img_model) >= 2 and model_norm.startswith(img_model[:2]))):
                        # Gedeeltelijke match gevonden - kies beste jaar
                        best_year, best_year_images = find_best_year_for_brand_model(brand_norm, model_norm, img_files)
                        if best_year_images:
                            matched_images = best_year_images
                            matched_no_year += 1
                            matched = True
                            break
    
    if matched_images:
        # Converteer naar image URLs
        image_urls = [f"/images/{img}" for img in matched_images]
        mapping[str(car_id)] = image_urls
        matched_count += 1
        total_images_matched += len(image_urls)
    else:
        unmatched_count += 1

print(f"  Auto's met images: {matched_count}")
print(f"    - Exacte jaar match: {matched_exact_year}")
print(f"    - Beste jaar match (geen exact jaar): {matched_best_year}")
print(f"    - Gedeeltelijke match (geen exact jaar): {matched_no_year}")
print(f"  Auto's zonder images: {unmatched_count}")
print(f"  Totaal aantal images toegewezen: {total_images_matched}")
print(f"  Gemiddeld aantal images per auto: {(total_images_matched/matched_count):.1f}" if matched_count > 0 else "  Gemiddeld aantal images per auto: 0")
print()

# STAP 4: Opslaan van mapping
print("STAP 4: Opslaan van mapping...")
with open(mapping_output, 'w', encoding='utf-8') as f:
    json.dump(mapping, f, indent=2, ensure_ascii=False)

print(f"  Mapping opgeslagen: {mapping_output}")
print(f"  Totaal mappings: {len(mapping)}")
print()

# STAP 5: Voorbeelden
print("STAP 5: Voorbeelden van matches...")
examples_shown = 0
for car_id_str, image_urls in list(mapping.items())[:10]:
    if examples_shown >= 10:
        break
    car_id = int(car_id_str)
    car = next((c for c in unique_cars if c['id'] == car_id), None)
    if car:
        print(f"  ID {car_id:5d}: {car['brand']:20s} {car['model']:30s} -> {len(image_urls)} images")
        for img_url in image_urls[:3]:  # Toon eerste 3 images
            print(f"      - {img_url}")
        if len(image_urls) > 3:
            print(f"      ... en {len(image_urls) - 3} meer")
        examples_shown += 1

print()
print("=" * 80)
print("KLAAR!")
print("=" * 80)
print()
print(f"De mapping is opgeslagen in:")
print(f"  {mapping_output}")
print()
print("Bij de volgende start van de backend API zal deze mapping automatisch")
print("worden geladen en gebruikt voor de image URLs van de auto's.")
print()

