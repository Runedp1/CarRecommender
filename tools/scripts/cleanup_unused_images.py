#!/usr/bin/env python3
"""
Script om images te verwijderen die niet bij auto's in de database horen.
Leest de CSV database en verwijdert alle images die niet matchen met brand/model/year combinaties.
"""

import os
import csv
import sys
from pathlib import Path

def normalize_for_matching(name):
    """Normaliseert naam voor matching (zelfde logica als C# code)."""
    if not name:
        return ""
    
    normalized = name.lower().strip()
    normalized = normalized.replace(" ", "_").replace("-", "_")
    # Verwijder speciale tekens behalve underscores
    import re
    normalized = re.sub(r'[^a-z0-9_]', '', normalized)
    return normalized

def load_cars_from_csv(csv_path):
    """Laadt alle auto's uit CSV en retourneert set van (brand, model, year) tuples."""
    cars = set()
    
    try:
        with open(csv_path, 'r', encoding='utf-8') as f:
            reader = csv.DictReader(f)
            
            for row in reader:
                # Zoek brand kolom
                brand = None
                for key in ['merk', 'brand', 'company names']:
                    if key.lower() in [k.lower() for k in row.keys()]:
                        brand = row[key]
                        break
                
                # Zoek model kolom
                model = None
                for key in ['model', 'cars names']:
                    if key.lower() in [k.lower() for k in row.keys()]:
                        model = row[key]
                        break
                
                # Zoek year kolom
                year = None
                for key in ['bouwjaar', 'year', 'jaar']:
                    if key.lower() in [k.lower() for k in row.keys()]:
                        try:
                            year = int(float(row[key])) if row[key] else None
                        except (ValueError, TypeError):
                            year = None
                        break
                
                if brand and model:
                    brand_norm = normalize_for_matching(brand)
                    model_norm = normalize_for_matching(model)
                    
                    # Voeg toe met en zonder year (voor flexibele matching)
                    cars.add((brand_norm, model_norm, year))
                    cars.add((brand_norm, model_norm, None))  # Voor images zonder year match
                    
    except Exception as e:
        print(f"Fout bij lezen van CSV: {e}", file=sys.stderr)
        return set()
    
    return cars

def matches_car(image_brand, image_model, image_year, cars_set):
    """Controleert of image matcht met een auto in de database."""
    brand_norm = normalize_for_matching(image_brand)
    model_norm = normalize_for_matching(image_model)
    
    # Exact match met year
    if (brand_norm, model_norm, image_year) in cars_set:
        return True
    
    # Match zonder year (flexibel)
    if (brand_norm, model_norm, None) in cars_set:
        return True
    
    # Partial match (brand bevat image_brand of vice versa)
    for car_brand, car_model, car_year in cars_set:
        if car_year is None or car_year == image_year:
            # Check brand match
            brand_match = (brand_norm == car_brand or 
                         brand_norm in car_brand or 
                         car_brand in brand_norm)
            
            # Check model match
            model_match = (model_norm == car_model or 
                          model_norm in car_model or 
                          car_model in model_norm)
            
            if brand_match and model_match:
                return True
    
    return False

def cleanup_images(images_dir, csv_path):
    """Verwijdert images die niet bij auto's in de database horen."""
    print(f"[1/3] Laden van auto's uit CSV: {csv_path}")
    cars_set = load_cars_from_csv(csv_path)
    print(f"[OK] {len(cars_set)} unieke brand/model/year combinaties gevonden")
    
    if not cars_set:
        print("[Fout] Geen auto's gevonden in CSV. Script gestopt.", file=sys.stderr)
        return
    
    print(f"\n[2/3] Scannen van images in: {images_dir}")
    image_files = list(Path(images_dir).glob("*.jpg"))
    print(f"[OK] {len(image_files)} images gevonden")
    
    print(f"\n[3/3] Controleren en verwijderen van ongebruikte images...")
    deleted_count = 0
    kept_count = 0
    
    for image_file in image_files:
        # Parse image naam: Brand_Model_Year_...
        parts = image_file.stem.split('_')
        
        if len(parts) < 2:
            # Onbekend formaat, verwijder niet (veiligheid)
            kept_count += 1
            continue
        
        image_brand = parts[0]
        image_model = parts[1]
        image_year = None
        
        # Probeer year te parsen (derde deel)
        if len(parts) > 2:
            try:
                image_year = int(parts[2])
            except ValueError:
                pass
        
        # Check of image matcht met database
        if matches_car(image_brand, image_model, image_year, cars_set):
            kept_count += 1
        else:
            # Verwijder image
            try:
                image_file.unlink()
                deleted_count += 1
                if deleted_count % 100 == 0:
                    print(f"  Verwijderd: {deleted_count} images...")
            except Exception as e:
                print(f"  Fout bij verwijderen van {image_file.name}: {e}", file=sys.stderr)
    
    print(f"\n[KLAAR] Resultaat:")
    print(f"  - Behouden: {kept_count} images")
    print(f"  - Verwijderd: {deleted_count} images")
    print(f"  - Totaal: {len(image_files)} images")

if __name__ == "__main__":
    # Bepaal paden
    script_dir = Path(__file__).parent
    project_root = script_dir.parent.parent
    
    csv_path = project_root / "backend" / "data" / "Cleaned_Car_Data_For_App_Fully_Enriched.csv"
    images_dir = project_root / "backend" / "images"
    
    # Check of bestanden bestaan
    if not csv_path.exists():
        print(f"[Fout] CSV bestand niet gevonden: {csv_path}", file=sys.stderr)
        sys.exit(1)
    
    if not images_dir.exists():
        print(f"[Fout] Images directory niet gevonden: {images_dir}", file=sys.stderr)
        sys.exit(1)
    
    print("=" * 60)
    print("CLEANUP UNUSED IMAGES")
    print("=" * 60)
    print(f"CSV: {csv_path}")
    print(f"Images: {images_dir}")
    print("=" * 60)
    
    # Check voor --yes flag om interactie te overslaan
    if "--yes" not in sys.argv:
        print("\n[WAARSCHUWING] Dit script zal images verwijderen die niet in de database staan.")
        print("Gebruik --yes flag om automatisch door te gaan zonder bevestiging.")
        print("Voorbeeld: python cleanup_unused_images.py --yes")
        sys.exit(0)
    
    cleanup_images(images_dir, csv_path)

