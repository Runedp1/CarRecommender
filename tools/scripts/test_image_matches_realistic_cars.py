"""
Test hoeveel auto's met alle hoofdvariabelen compleet een foto match hebben.
"""
import pandas as pd
import os
from pathlib import Path

script_dir = os.path.dirname(os.path.abspath(__file__))
project_root = os.path.dirname(os.path.dirname(script_dir))

csv_path = os.path.join(project_root, 'backend', 'data', 'Cleaned_Car_Data_For_App_Fully_Enriched.csv')
images_dir = os.path.join(project_root, 'backend', 'images')

print("=" * 80)
print("TEST: HOEVEEL REALISTISCHE AUTO'S HEBBEN EEN FOTO?")
print("=" * 80)

# C# filter logica (gekopieerd uit IsCarRealistic)
MIN_REALISTIC_PRICE = 300
MAX_REALISTIC_PRICE = 500000
MIN_REALISTIC_POWER = 20
MAX_REALISTIC_POWER = 800
MIN_REALISTIC_YEAR = 1990
MAX_REALISTIC_YEAR = 2025

# Lees CSV
df = pd.read_csv(csv_path)
print(f"\nTotaal auto's in CSV: {len(df):,}")

# Parse zoals C# doet
df['prijs_clean'] = pd.to_numeric(df['prijs'], errors='coerce')
df['vermogen_clean'] = df['vermogen'].astype(str).str.replace(r'[^\d]', '', regex=True)
df['vermogen_clean'] = pd.to_numeric(df['vermogen_clean'], errors='coerce')
df['bouwjaar_clean'] = pd.to_numeric(df['bouwjaar'], errors='coerce')

# Apply filters (zoals IsCarRealistic in C#)
realistic_mask = (
    (df['prijs_clean'] >= MIN_REALISTIC_PRICE) & (df['prijs_clean'] <= MAX_REALISTIC_PRICE) &
    (df['vermogen_clean'] >= MIN_REALISTIC_POWER) & (df['vermogen_clean'] <= MAX_REALISTIC_POWER) &
    (df['bouwjaar_clean'] >= MIN_REALISTIC_YEAR) & (df['bouwjaar_clean'] <= MAX_REALISTIC_YEAR) &
    (df['merk'].notna()) & (df['merk'].astype(str).str.strip() != '') &
    (df['model'].notna()) & (df['model'].astype(str).str.strip() != '')
)

realistic_cars = df[realistic_mask].copy()
print(f"\nAuto's met alle hoofdvariabelen compleet (na IsCarRealistic filter): {len(realistic_cars):,}")

# Normaliseer functie (zoals in C#)
def normalize(name):
    if pd.isna(name) or name == '':
        return ''
    normalized = str(name).lower().strip()
    normalized = normalized.replace(' ', '_').replace('-', '_')
    import re
    normalized = re.sub(r'[^a-z0-9_]', '', normalized)
    return normalized

# Lees alle image bestanden
if os.path.exists(images_dir):
    image_files = list(Path(images_dir).glob('*.jpg'))
    print(f"\nFoto's beschikbaar in backend/images/: {len(image_files):,}")
    
    # Maak lookup dictionary voor snelle matching
    image_lookup = {}
    for img_file in image_files:
        filename = img_file.stem  # zonder extensie
        parts = filename.split('_')
        if len(parts) >= 2:
            brand_norm = normalize(parts[0])
            model_norm = normalize(parts[1])
            key = f"{brand_norm}_{model_norm}"
            if key not in image_lookup:
                image_lookup[key] = []
            image_lookup[key].append(img_file.name)
    
    print(f"Unieke brand_model combinaties in foto's: {len(image_lookup):,}")
    
    # Test matching
    matched = 0
    sample_matches = []
    
    for idx, row in realistic_cars.iterrows():
        brand = str(row['merk']).strip()
        model = str(row['model']).strip()
        
        brand_norm = normalize(brand)
        model_norm = normalize(model)
        
        # Zoek exacte match
        key = f"{brand_norm}_{model_norm}"
        if key in image_lookup:
            matched += 1
            if len(sample_matches) < 5:
                sample_matches.append({
                    'merk': brand,
                    'model': model,
                    'foto': image_lookup[key][0]
                })
        else:
            # Probeer partial match
            found = False
            for img_key in image_lookup.keys():
                if brand_norm in img_key or img_key.startswith(brand_norm + '_'):
                    # Check model match
                    img_model = img_key.split('_', 1)[1] if '_' in img_key else ''
                    if model_norm in img_model or img_model in model_norm:
                        matched += 1
                        found = True
                        if len(sample_matches) < 5:
                            sample_matches.append({
                                'merk': brand,
                                'model': model,
                                'foto': image_lookup[img_key][0]
                            })
                        break
    
    print(f"\n{'='*80}")
    print("RESULTATEN:")
    print(f"{'='*80}")
    print(f"Realistische auto's (alle variabelen compleet): {len(realistic_cars):,}")
    print(f"Auto's met foto match: {matched:,}")
    print(f"Percentage met foto: {(matched * 100.0 / len(realistic_cars)):.1f}%")
    print(f"Auto's zonder foto: {len(realistic_cars) - matched:,} ({(len(realistic_cars) - matched) * 100.0 / len(realistic_cars):.1f}%)")
    
    if sample_matches:
        print(f"\nVoorbeelden van matches:")
        for match in sample_matches:
            print(f"  - {match['merk']} {match['model']} -> {match['foto']}")
else:
    print(f"\nFOUT: Images directory niet gevonden: {images_dir}")



