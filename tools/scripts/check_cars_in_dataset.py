"""
Script om te controleren hoeveel auto's en hoeveel per merk in de dataset zitten.
"""
import csv
import os
import sys
from collections import defaultdict, Counter

script_dir = os.path.dirname(os.path.abspath(__file__))
project_root = os.path.dirname(os.path.dirname(script_dir))
csv_path = os.path.join(project_root, 'backend', 'data', 'df_master_v8_def.csv')

print("=" * 80)
print("CONTROLE DATASET")
print("=" * 80)
print(f"CSV pad: {csv_path}")
print()

# Laad alle auto's uit CSV
all_cars = []
brand_count = Counter()

try:
    with open(csv_path, 'r', encoding='utf-8') as f:
        reader = csv.DictReader(f)
        
        brand_col = 'merk'
        model_col = 'model'
        price_col = 'prijs'
        
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
            
            all_cars.append({
                'brand': brand,
                'model': model,
                'price': price
            })
            brand_count[brand.lower()] += 1

    print(f"Totaal auto's in CSV (voor deduplicatie): {len(all_cars)}")
    print()
    
    # Deduplicatie (zoals CarRepository zou moeten doen)
    unique_cars_dict = {}
    for car in all_cars:
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
    print(f"Unieke auto's na deduplicatie (Brand+Model, hoogste prijs): {len(unique_cars)}")
    print()
    
    # Tel merken na deduplicatie
    brand_count_unique = Counter()
    for car in unique_cars:
        brand_count_unique[car['brand'].lower()] += 1
    
    print("Top 20 merken NA deduplicatie:")
    for brand, count in brand_count_unique.most_common(20):
        print(f"  {brand:20s}: {count:3d} modellen")
    
    print()
    print("Audi specifiek:")
    audi_cars = [c for c in unique_cars if c['brand'].lower() == 'audi']
    print(f"  Totaal Audi modellen: {len(audi_cars)}")
    for car in sorted(audi_cars, key=lambda x: x['model'].lower()):
        print(f"    - {car['brand']} {car['model']}")
    
except Exception as e:
    print(f"FOUT: {e}")
    import traceback
    traceback.print_exc()
    sys.exit(1)


