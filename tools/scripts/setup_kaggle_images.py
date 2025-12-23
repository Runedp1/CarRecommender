"""
Eenvoudig script om Kaggle Car Connection Picture Dataset te downloaden.
Gebruik dit script om de images te downloaden en te organiseren.
"""

import kagglehub
import os
import shutil
from pathlib import Path

print("=" * 80)
print("KAGGLE IMAGES SETUP")
print("=" * 80)

# Download dataset
print("\n[1/3] Downloaden van Kaggle dataset...")
try:
    path = kagglehub.dataset_download("prondeau/the-car-connection-picture-dataset")
    print(f"[OK] Dataset gedownload naar: {path}")
except Exception as e:
    print(f"[ERROR] Fout: {e}")
    print("\nZorg dat je:")
    print("1. Kaggle API hebt geconfigureerd (~/.kaggle/kaggle.json)")
    print("2. kagglehub hebt geïnstalleerd: pip install kagglehub")
    exit(1)

# Vind images directory
print("\n[2/3] Zoeken naar images...")
images_dir = None
for root, dirs, files in os.walk(path):
    if 'images' in dirs:
        images_dir = os.path.join(root, 'images')
        break
    elif any(f.endswith(('.jpg', '.jpeg', '.png')) for f in files):
        images_dir = root
        break

if not images_dir:
    print(f"✗ Geen images gevonden. Check handmatig: {path}")
    exit(1)

print(f"[OK] Images gevonden in: {images_dir}")

# Tel images
image_count = sum(1 for root, dirs, files in os.walk(images_dir) 
                  for f in files if f.lower().endswith(('.jpg', '.jpeg', '.png')))
print(f"[OK] {image_count} images gevonden")

# Kopieer naar project
print("\n[3/3] Kopiëren naar project...")
project_root = Path(__file__).parent.parent.parent
target_dir = project_root / "backend" / "images"

# Maak directory aan
target_dir.mkdir(parents=True, exist_ok=True)

# Kopieer alle images (behoud structuur)
copied = 0
for root, dirs, files in os.walk(images_dir):
    for file in files:
        if file.lower().endswith(('.jpg', '.jpeg', '.png')):
            src = os.path.join(root, file)
            # Behoud relatieve structuur
            rel_path = os.path.relpath(src, images_dir)
            dst = target_dir / rel_path
            dst.parent.mkdir(parents=True, exist_ok=True)
            
            try:
                shutil.copy2(src, dst)
                copied += 1
                if copied % 100 == 0:
                    print(f"  {copied} images gekopieerd...")
            except Exception as e:
                print(f"  ⚠ Fout bij {file}: {e}")

print(f"[OK] {copied} images gekopieerd naar: {target_dir}")
print("\n" + "=" * 80)
print("KLAAR!")
print("=" * 80)
print(f"\nImages staan nu in: {target_dir}")
print("\nVolgende stappen:")
print("1. Run match_images_to_cars.py om images te koppelen aan auto's")
print("2. Herstart de backend API")
print("3. Test in de frontend")

