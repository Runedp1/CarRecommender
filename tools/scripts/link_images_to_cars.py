"""
Script om Kaggle auto-afbeeldingen te koppelen aan auto's in de CSV.
Dit script:
1. Leest de Image_table.csv om te zien welke auto's images hebben
2. Zoekt de corresponderende afbeeldingen in de Kaggle dataset
3. Kopieert/koppelt de afbeeldingen naar de images/ directory structuur
4. Update de CSV met de juiste Image_Path
"""
import pandas as pd
import shutil
from pathlib import Path
import os

def link_images_to_cars(kaggle_dataset_path, image_table_csv="data/Image_table.csv", output_dir="images"):
    """
    Koppel Kaggle auto-afbeeldingen aan auto's in de CSV.
    
    Args:
        kaggle_dataset_path: Pad naar de gedownloade Kaggle dataset
        image_table_csv: Pad naar Image_table.csv
        output_dir: Output directory voor georganiseerde afbeeldingen
    """
    print("Linking car images from Kaggle dataset to cars in CSV...")
    
    # Lees Image_table.csv
    print(f"Reading {image_table_csv}...")
    try:
        df_images = pd.read_csv(image_table_csv)
        print(f"Found {len(df_images)} image records")
        print(f"Columns: {df_images.columns.tolist()}")
        print(f"\nFirst few rows:")
        print(df_images.head())
    except Exception as e:
        print(f"Error reading Image_table.csv: {e}")
        return
    
    # Bekijk Kaggle dataset structuur
    kaggle_path = Path(kaggle_dataset_path)
    print(f"\nKaggle dataset structure:")
    for item in kaggle_path.iterdir():
        print(f"  - {item.name}")
        if item.is_dir():
            files = list(item.glob("*.jpg"))[:5] + list(item.glob("*.png"))[:5]
            print(f"    Found {len(files)} image files (showing first 5)")
            for file in files[:5]:
                print(f"      - {file.name}")
    
    # TODO: Implementeer logica om:
    # 1. Image_name uit Image_table.csv te matchen met bestanden in Kaggle dataset
    # 2. Kopieer afbeeldingen naar images/{brand}/{model}/{id}.jpg structuur
    # 3. Update Cleaned_Car_Data_For_App_Fully_Enriched.csv met Image_Path
    
    print("\nTODO: Implement image linking logic based on dataset structure")

if __name__ == "__main__":
    import sys
    if len(sys.argv) < 2:
        print("Usage: python link_images_to_cars.py <kaggle_dataset_path>")
        print("Example: python link_images_to_cars.py C:/Users/.../kshitij192-cars-image-dataset")
    else:
        link_images_to_cars(sys.argv[1])
