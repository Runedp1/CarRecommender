"""
Script om Kaggle images te matchen met auto's uit de CSV.
Dit script analyseert de image bestandsnamen en maakt een mapping naar auto's.
"""

import pandas as pd
import os
from pathlib import Path
import json
import re

print("=" * 80)
print("IMAGE TO CAR MATCHING SCRIPT")
print("=" * 80)

# Configuratie
project_root = Path(__file__).parent.parent.parent
csv_file = project_root / "backend" / "data" / "Cleaned_Car_Data_For_App_Fully_Enriched.csv"
images_dir = project_root / "backend" / "images"
output_mapping = project_root / "tools" / "image_car_mapping.json"

# Stap 1: Laad CSV data
print("\n[Stap 1] Laden van CSV data...")
if not csv_file.exists():
    print(f"✗ CSV bestand niet gevonden: {csv_file}")
    exit(1)

try:
    df = pd.read_csv(csv_file, low_memory=False)
    print(f"✓ {len(df)} auto's geladen uit CSV")
    print(f"  Kolommen: {', '.join(df.columns.tolist()[:10])}...")
except Exception as e:
    print(f"✗ Fout bij laden CSV: {e}")
    exit(1)

# Stap 2: Vind alle images
print("\n[Stap 2] Scannen van images directory...")
if not images_dir.exists():
    print(f"✗ Images directory niet gevonden: {images_dir}")
    print("  Run eerst download_kaggle_images.py")
    exit(1)

image_files = []
for root, dirs, files in os.walk(images_dir):
    for file in files:
        if file.lower().endswith(('.jpg', '.jpeg', '.png')):
            full_path = Path(root) / file
            rel_path = full_path.relative_to(images_dir)
            image_files.append({
                'filename': file,
                'relative_path': str(rel_path),
                'full_path': str(full_path)
            })

print(f"✓ {len(image_files)} images gevonden")

# Stap 3: Analyseer image bestandsnamen
print("\n[Stap 3] Analyseren van image bestandsnamen...")
print("Voorbeelden van image namen:")
for img in image_files[:5]:
    print(f"  - {img['filename']}")

# Probeer patronen te vinden in bestandsnamen
# Veel Kaggle datasets gebruiken formaten zoals:
# - Brand_Model_Year.jpg
# - Brand$$Model$$Year.jpg
# - brand-model-year.jpg
# etc.

def extract_car_info_from_filename(filename):
    """Probeer merk, model en jaar uit bestandsnaam te halen."""
    # Verwijder extensie
    name = Path(filename).stem
    
    # Probeer verschillende scheidingstekens
    separators = ['$$', '_', '-', ' ']
    parts = None
    
    for sep in separators:
        if sep in name:
            parts = name.split(sep)
            break
    
    if not parts:
        parts = [name]
    
    # Normaliseer
    parts = [p.strip() for p in parts if p.strip()]
    
    # Probeer jaar te vinden (4 cijfers tussen 1900-2030)
    year = None
    brand = None
    model = None
    
    for part in parts:
        # Check voor jaar
        year_match = re.search(r'\b(19|20)\d{2}\b', part)
        if year_match:
            year = int(year_match.group())
            continue
        
        # Eerste niet-numerieke part is waarschijnlijk merk
        if not brand and part and not part.isdigit():
            brand = part
        # Tweede niet-numerieke part is waarschijnlijk model
        elif brand and not model and part and not part.isdigit():
            model = part
    
    return {
        'brand': brand,
        'model': model,
        'year': year,
        'parts': parts
    }

# Analyseer alle images
image_info = []
for img in image_files:
    info = extract_car_info_from_filename(img['filename'])
    info['filename'] = img['filename']
    info['relative_path'] = img['relative_path']
    image_info.append(info)

print(f"\n✓ {len(image_info)} images geanalyseerd")
print("\nVoorbeelden van geëxtraheerde info:")
for info in image_info[:5]:
    print(f"  {info['filename']}")
    print(f"    -> Brand: {info['brand']}, Model: {info['model']}, Year: {info['year']}")

# Stap 4: Match images met auto's
print("\n[Stap 4] Matchen van images met auto's...")

# Normaliseer merk en model namen voor matching
def normalize_name(name):
    """Normaliseer naam voor matching."""
    if pd.isna(name) or not name:
        return None
    return str(name).lower().strip().replace(' ', '').replace('-', '').replace('_', '')

# Maak mapping
mappings = []
matched_count = 0

for idx, car in df.iterrows():
    car_brand = normalize_name(car.get('merk') or car.get('Brand'))
    car_model = normalize_name(car.get('model') or car.get('Model'))
    car_year = car.get('bouwjaar') or car.get('Year')
    
    if not car_brand or not car_model:
        continue
    
    # Zoek matching images
    for img_info in image_info:
        img_brand = normalize_name(img_info['brand'])
        img_model = normalize_name(img_info['model'])
        img_year = img_info['year']
        
        # Match op merk en model (jaar is optioneel)
        brand_match = img_brand and car_brand and img_brand == car_brand
        model_match = img_model and car_model and img_model == car_model
        year_match = not img_year or not car_year or img_year == car_year
        
        if brand_match and model_match:
            mappings.append({
                'car_id': car.get('id') or car.get('Id') or idx,
                'car_brand': car.get('merk') or car.get('Brand'),
                'car_model': car.get('model') or car.get('Model'),
                'car_year': car_year,
                'image_path': img_info['relative_path'],
                'image_filename': img_info['filename'],
                'match_confidence': 'high' if year_match else 'medium'
            })
            matched_count += 1
            break  # Neem eerste match

print(f"✓ {matched_count} auto's gematcht met images")

# Stap 5: Sla mapping op
print("\n[Stap 5] Opslaan van mapping...")
mapping_data = {
    'total_cars': len(df),
    'total_images': len(image_files),
    'matched_count': matched_count,
    'match_rate': f"{(matched_count/len(df)*100):.1f}%",
    'mappings': mappings
}

with open(output_mapping, 'w', encoding='utf-8') as f:
    json.dump(mapping_data, f, indent=2, ensure_ascii=False)

print(f"✓ Mapping opgeslagen in: {output_mapping}")

# Stap 6: Samenvatting
print("\n" + "=" * 80)
print("SAMENVATTING")
print("=" * 80)
print(f"Totaal auto's: {len(df)}")
print(f"Totaal images: {len(image_files)}")
print(f"Gematcht: {matched_count} ({(matched_count/len(df)*100):.1f}%)")
print(f"\nMapping bestand: {output_mapping}")
print("\nVolgende stappen:")
print("1. Review de mapping om te zien of matches correct zijn")
print("2. Pas CarRepository aan om deze mapping te gebruiken")
print("3. Test image loading in frontend")
print("=" * 80)









