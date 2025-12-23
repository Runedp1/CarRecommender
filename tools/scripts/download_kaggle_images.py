"""
Script om Kaggle Car Connection Picture Dataset te downloaden en te organiseren.
Dit script download de dataset en organiseert de afbeeldingen zodat ze gekoppeld kunnen worden aan auto's.
"""

import kagglehub
import os
import shutil
import pandas as pd
from pathlib import Path
import json

print("=" * 80)
print("KAGGLE CAR IMAGES DATASET DOWNLOADER")
print("=" * 80)

# Stap 1: Download dataset
print("\n[Stap 1] Downloaden van Kaggle dataset...")
try:
    path = kagglehub.dataset_download("prondeau/the-car-connection-picture-dataset")
    print(f"✓ Dataset gedownload naar: {path}")
except Exception as e:
    print(f"✗ Fout bij downloaden: {e}")
    print("\nZorg dat je:")
    print("1. Kaggle API credentials hebt ingesteld (~/.kaggle/kaggle.json)")
    print("2. De dataset naam correct is: 'prondeau/the-car-connection-picture-dataset'")
    exit(1)

# Stap 2: Vind de images directory
print("\n[Stap 2] Zoeken naar images directory...")
images_dir = None
for root, dirs, files in os.walk(path):
    if 'images' in dirs or any(f.endswith(('.jpg', '.jpeg', '.png')) for f in files):
        # Check of dit de images directory is
        if 'images' in dirs:
            images_dir = os.path.join(root, 'images')
            break
        elif any(f.endswith(('.jpg', '.jpeg', '.png')) for f in files):
            images_dir = root
            break

if not images_dir:
    print("✗ Geen images directory gevonden. Zoek handmatig in:")
    print(f"  {path}")
    exit(1)

print(f"✓ Images directory gevonden: {images_dir}")

# Stap 3: Analyseer de structuur
print("\n[Stap 3] Analyseren van dataset structuur...")
image_files = []
for root, dirs, files in os.walk(images_dir):
    for file in files:
        if file.lower().endswith(('.jpg', '.jpeg', '.png')):
            full_path = os.path.join(root, file)
            rel_path = os.path.relpath(full_path, images_dir)
            image_files.append({
                'filename': file,
                'relative_path': rel_path,
                'full_path': full_path
            })

print(f"✓ {len(image_files)} afbeeldingen gevonden")

# Stap 4: Probeer metadata te vinden (als beschikbaar)
print("\n[Stap 4] Zoeken naar metadata...")
metadata_files = []
for root, dirs, files in os.walk(path):
    for file in files:
        if file.endswith(('.csv', '.json', '.xlsx')):
            metadata_files.append(os.path.join(root, file))

if metadata_files:
    print(f"✓ {len(metadata_files)} metadata bestanden gevonden:")
    for mf in metadata_files:
        print(f"  - {mf}")
else:
    print("⚠ Geen metadata bestanden gevonden")

# Stap 5: Kopieer images naar project structuur
print("\n[Stap 5] Kopiëren van images naar project structuur...")
project_root = Path(__file__).parent.parent.parent
target_images_dir = project_root / "backend" / "images"

# Maak target directory aan
target_images_dir.mkdir(parents=True, exist_ok=True)

# Kopieer alle images (behoud structuur)
copied_count = 0
for img in image_files[:100]:  # Limiteer voor nu tot 100 images voor test
    try:
        # Behoud de relatieve structuur
        target_path = target_images_dir / img['relative_path']
        target_path.parent.mkdir(parents=True, exist_ok=True)
        
        shutil.copy2(img['full_path'], target_path)
        copied_count += 1
        if copied_count % 10 == 0:
            print(f"  {copied_count} images gekopieerd...")
    except Exception as e:
        print(f"  ⚠ Fout bij kopiëren {img['filename']}: {e}")

print(f"✓ {copied_count} images gekopieerd naar {target_images_dir}")

# Stap 6: Maak mapping bestand
print("\n[Stap 6] Maken van mapping bestand...")
mapping = {
    'dataset_path': str(path),
    'images_dir': str(images_dir),
    'target_dir': str(target_images_dir),
    'total_images': len(image_files),
    'copied_images': copied_count,
    'image_structure': {}
}

# Analyseer de structuur
structure = {}
for img in image_files:
    parts = Path(img['relative_path']).parts
    if len(parts) > 0:
        key = parts[0] if len(parts) > 1 else 'root'
        if key not in structure:
            structure[key] = []
        structure[key].append(img['relative_path'])

mapping['image_structure'] = {k: len(v) for k, v in structure.items()}

# Sla mapping op
mapping_file = project_root / "tools" / "kaggle_images_mapping.json"
with open(mapping_file, 'w', encoding='utf-8') as f:
    json.dump(mapping, f, indent=2, ensure_ascii=False)

print(f"✓ Mapping opgeslagen in: {mapping_file}")

# Stap 7: Maak instructies voor matching
print("\n[Stap 7] Samenvatting:")
print("=" * 80)
print(f"Dataset locatie: {path}")
print(f"Images directory: {images_dir}")
print(f"Target directory: {target_images_dir}")
print(f"Totaal images: {len(image_files)}")
print(f"Gekopieerd: {copied_count}")
print("\nVolgende stappen:")
print("1. Analyseer de image bestandsnamen om te zien hoe ze gekoppeld kunnen worden aan auto's")
print("2. Pas CarRepository aan om images te vinden op basis van merk/model")
print("3. Test de image loading in de frontend")
print("=" * 80)

