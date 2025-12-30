"""
Script om te tellen hoeveel unieke auto's er zijn in de dataset
op basis van Brand (merk) en Model.
"""
import csv
import os
import sys

# Bepaal data directory
script_dir = os.path.dirname(os.path.abspath(__file__))
project_root = os.path.dirname(os.path.dirname(script_dir))
csv_path = os.path.join(project_root, 'backend', 'data', 'df_master_v8_def.csv')

print(f"CSV pad: {csv_path}")
print("=" * 60)

if not os.path.exists(csv_path):
    print(f"FOUT: CSV bestand niet gevonden: {csv_path}")
    sys.exit(1)

# Lees CSV en verzamel unieke combinaties
unique_combinations = set()
total_rows = 0
rows_with_brand_model = 0

try:
    with open(csv_path, 'r', encoding='utf-8') as f:
        reader = csv.DictReader(f)
        
        # Zoek de juiste kolom namen (case-insensitive)
        fieldnames = reader.fieldnames
        print(f"Kolommen in CSV: {', '.join(fieldnames)}")
        print()
        
        # Zoek brand en model kolommen (flexibel, zoals in C# code)
        brand_col = None
        model_col = None
        
        for col in fieldnames:
            col_lower = col.lower()
            if brand_col is None and any(x in col_lower for x in ['merk', 'brand', 'company names']):
                brand_col = col
            if model_col is None and any(x in col_lower for x in ['model', 'cars names']):
                model_col = col
        
        if not brand_col or not model_col:
            print(f"FOUT: Kon brand of model kolom niet vinden!")
            print(f"Brand kolom: {brand_col}")
            print(f"Model kolom: {model_col}")
            sys.exit(1)
        
        print(f"Gebruikte kolommen:")
        print(f"  Brand: '{brand_col}'")
        print(f"  Model: '{model_col}'")
        print()
        
        # Verwerk alle rijen
        for row in reader:
            total_rows += 1
            
            brand = (row.get(brand_col) or '').strip()
            model = (row.get(model_col) or '').strip()
            
            # Alleen toevoegen als beide niet leeg zijn
            if brand and model:
                rows_with_brand_model += 1
                # Maak unieke key: Brand|Model (case-insensitive)
                unique_key = f"{brand.lower()}|{model.lower()}"
                unique_combinations.add(unique_key)
        
        print("=" * 60)
        print("RESULTATEN:")
        print(f"  Totaal aantal rijen in CSV: {total_rows}")
        print(f"  Rijen met brand en model: {rows_with_brand_model}")
        print(f"  Unieke combinaties (Brand|Model): {len(unique_combinations)}")
        print(f"  Duplicaten verwijderd: {rows_with_brand_model - len(unique_combinations)}")
        print("=" * 60)
        
except Exception as e:
    print(f"FOUT bij het lezen van CSV: {e}")
    import traceback
    traceback.print_exc()
    sys.exit(1)

