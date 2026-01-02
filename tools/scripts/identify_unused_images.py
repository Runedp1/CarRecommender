"""
Script om te identificeren welke images verwijderd kunnen worden
omdat de modellen niet meer in de dataset zitten.
"""
import csv
import os
import sys
from collections import defaultdict

script_dir = os.path.dirname(os.path.abspath(__file__))
project_root = os.path.dirname(os.path.dirname(script_dir))
csv_path = os.path.join(project_root, 'backend', 'data', 'df_master_v8_def.csv')
images_dir = os.path.join(project_root, 'backend', 'images')

def normalize_name(name):
    if not name:
        return ""
    return str(name).lower().strip().replace(' ', '').replace('-', '').replace('_', '')

print("=" * 80)
print("IDENTIFICEER ONGEBRUIKTE IMAGES")
print("=" * 80)
print(f"CSV pad: {csv_path}")
print(f"Images directory: {images_dir}")
print()

# STAP 1: Laad alle modellen uit de dataset (na deduplicatie)
print("STAP 1: Laden van dataset en identificeren gebruikte modellen...")
used_models = set()

try:
    with open(csv_path, 'r', encoding='utf-8') as f:
        reader = csv.DictReader(f)
        
        brand_col = 'merk'
        model_col = 'model'
        price_col = 'prijs'
        
        # Groepeer auto's op Brand|Model
        cars_by_key = defaultdict(list)
        
        for row in reader:
            brand = (row.get(brand_col, '') or '').strip()
            model = (row.get(model_col, '') or '').strip()
            price_str = (row.get(price_col, '') or '').strip()
            
            if not brand or not model:
                continue
            
            try:
                price_clean = ''.join(c for c in price_str if c.isdigit() or c in '.,')
                price_clean = price_clean.replace(',', '.')
                price = float(price_clean) if price_clean else 0
            except:
                price = 0
            
            unique_key = f"{brand.lower()}|{model.lower()}"
            cars_by_key[unique_key].append({
                'brand': brand,
                'model': model,
                'price': price
            })
        
        # Behoud hoogste prijs per combinatie (zoals deduplicatie doet)
        for unique_key, cars_list in cars_by_key.items():
            cars_with_prices = [c for c in cars_list if c['price'] > 0]
            if cars_with_prices:
                best_car = max(cars_with_prices, key=lambda x: x['price'])
            else:
                best_car = cars_list[0]
            
            brand_norm = normalize_name(best_car['brand'])
            model_norm = normalize_name(best_car['model'])
            used_models.add(f"{brand_norm}|{model_norm}")

    print(f"  Gebruikte modellen in dataset: {len(used_models)}")
    
except Exception as e:
    print(f"FOUT bij het laden van CSV: {e}")
    import traceback
    traceback.print_exc()
    sys.exit(1)

# STAP 2: Laad alle images en identificeer welke modellen ze hebben
print()
print("STAP 2: Analyseren van images...")
image_files = []
for file in os.listdir(images_dir):
    if file.lower().endswith('.jpg'):
        image_files.append(file)

print(f"  Totaal images gevonden: {len(image_files)}")

# Groepeer images per model
images_by_model = defaultdict(list)

for image_file in image_files:
    name_without_ext = os.path.splitext(image_file)[0]
    parts = name_without_ext.split('_')
    
    if len(parts) >= 2:
        brand = parts[0].strip()
        model = parts[1].strip()
        
        if brand and model:
            brand_norm = normalize_name(brand)
            model_norm = normalize_name(model)
            model_key = f"{brand_norm}|{model_norm}"
            
            images_by_model[model_key].append(image_file)

print(f"  Unieke modellen in images: {len(images_by_model)}")

# STAP 3: Identificeer ongebruikte modellen
print()
print("STAP 3: Identificeren van ongebruikte images...")
unused_models = set(images_by_model.keys()) - used_models
used_in_images = set(images_by_model.keys()) & used_models

print(f"  Modellen in images EN dataset: {len(used_in_images)}")
print(f"  Modellen in images MAAR NIET in dataset: {len(unused_models)}")

# Tel aantal images dat verwijderd kan worden
unused_images_count = 0
unused_images_list = []

for model_key in unused_models:
    images = images_by_model[model_key]
    unused_images_count += len(images)
    unused_images_list.extend(images)

print(f"  Totaal aantal ongebruikte images: {unused_images_count}")
print(f"  Aantal images dat behouden moet worden: {len(image_files) - unused_images_count}")

# STAP 4: Toon voorbeelden
print()
print("STAP 4: Voorbeelden van ongebruikte modellen:")
examples_shown = 0
for model_key in sorted(unused_models)[:20]:
    if examples_shown >= 20:
        break
    images = images_by_model[model_key]
    brand, model = model_key.split('|')
    print(f"  {brand:20s} {model:30s} -> {len(images)} images")
    examples_shown += 1

if len(unused_models) > 20:
    print(f"  ... en {len(unused_models) - 20} meer modellen")

# STAP 5: Schrijf lijst van te verwijderen bestanden
print()
print("STAP 5: Opslaan van lijst met te verwijderen images...")
delete_list_file = os.path.join(project_root, 'tools', 'scripts', 'images_to_delete.txt')
with open(delete_list_file, 'w', encoding='utf-8') as f:
    for image_file in sorted(unused_images_list):
        f.write(f"{image_file}\n")

print(f"  Lijst opgeslagen: {delete_list_file}")
print(f"  Totaal bestanden in lijst: {len(unused_images_list)}")

print()
print("=" * 80)
print("KLAAR!")
print("=" * 80)
print()
print(f"Er zijn {unused_images_count} images die verwijderd kunnen worden")
print(f"van {len(unused_models)} modellen die niet meer in de dataset zitten.")
print()
print(f"De lijst staat in: {delete_list_file}")
print()


