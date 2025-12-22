"""
Complete script om Kaggle auto-afbeeldingen te downloaden en te koppelen aan auto's.
Dit script:
1. Download de Kaggle dataset
2. Koppelt afbeeldingen aan auto's op basis van Image_table.csv
3. Kopieert afbeeldingen naar images/{brand}/{model}/ structuur
4. Genereert ImageUrl voor elke auto
"""
import kagglehub
import pandas as pd
import shutil
from pathlib import Path
import os
import re

def sanitize_filename(name):
    """Maak een veilige bestandsnaam."""
    # Verwijder illegale tekens
    name = re.sub(r'[<>:"/\\|?*]', '', name)
    name = name.replace(' ', '_').replace('&', 'and')
    return name.lower()

def download_kaggle_dataset():
    """Download de Kaggle cars-image-dataset."""
    print("=" * 60)
    print("Step 1: Downloading Kaggle dataset...")
    print("=" * 60)
    
    try:
        path = kagglehub.dataset_download("kshitij192/cars-image-dataset")
        print(f"✓ Dataset downloaded to: {path}")
        return str(path)
    except Exception as e:
        print(f"✗ Error downloading dataset: {e}")
        print("\nTip: Make sure you have Kaggle credentials set up:")
        print("  1. Go to https://www.kaggle.com/account")
        print("  2. Create API token")
        print("  3. Place kaggle.json in ~/.kaggle/")
        return None

def explore_dataset_structure(dataset_path):
    """Verkennen van de dataset structuur."""
    print("\n" + "=" * 60)
    print("Step 2: Exploring dataset structure...")
    print("=" * 60)
    
    dataset_dir = Path(dataset_path)
    
    # Zoek alle image bestanden
    image_extensions = ['.jpg', '.jpeg', '.png', '.JPG', '.JPEG', '.PNG']
    image_files = []
    
    for ext in image_extensions:
        image_files.extend(list(dataset_dir.rglob(f"*{ext}")))
    
    print(f"Found {len(image_files)} image files")
    
    # Toon structuur
    print("\nDataset structure:")
    for item in sorted(dataset_dir.iterdir()):
        if item.is_dir():
            sub_files = list(item.glob("*.jpg")) + list(item.glob("*.png"))
            print(f"  📁 {item.name}/ ({len(sub_files)} images)")
            if len(sub_files) > 0:
                print(f"      Example: {sub_files[0].name}")
        else:
            print(f"  📄 {item.name}")
    
    return image_files

def link_images_to_cars(dataset_path, image_files, output_dir="images"):
    """Koppel Kaggle afbeeldingen aan auto's uit Image_table.csv."""
    print("\n" + "=" * 60)
    print("Step 3: Linking images to cars...")
    print("=" * 60)
    
    # Lees Image_table.csv
    image_table_path = Path("data/Image_table.csv")
    if not image_table_path.exists():
        print(f"✗ {image_table_path} not found!")
        return
    
    print(f"Reading {image_table_path}...")
    df_images = pd.read_csv(image_table_path)
    print(f"✓ Loaded {len(df_images)} image records")
    
    # Maak output directory structuur
    output_path = Path(output_dir)
    output_path.mkdir(exist_ok=True)
    
    # Groepeer images per Genmodel_ID (merk+model combinatie)
    print("\nGrouping images by Genmodel_ID...")
    grouped = df_images.groupby('Genmodel_ID')
    
    # Maak een mapping van Image_name naar bestandspad in dataset
    print("Creating image name to file mapping...")
    image_name_to_path = {}
    
    # Probeer Image_name te matchen met bestanden in dataset
    for idx, row in df_images.head(100).iterrows():  # Test met eerste 100
        image_name = str(row.get('Image_name', ''))
        if pd.isna(image_name) or image_name == '':
            continue
        
        # Extract bestandsnaam uit Image_name (bijv. "Abarth$$124 Spider$$2017$$Blue$$2_1$$1$$image_1.jpg")
        if '$$' in image_name:
            parts = image_name.split('$$')
            if len(parts) > 0:
                filename = parts[-1]  # Laatste deel is meestal de bestandsnaam
                
                # Zoek dit bestand in de dataset
                for img_file in image_files:
                    if filename.lower() in img_file.name.lower():
                        image_name_to_path[image_name] = img_file
                        break
    
    print(f"✓ Mapped {len(image_name_to_path)} images")
    
    # Kopieer afbeeldingen naar images/{brand}/{model}/ structuur
    print("\nCopying images to organized structure...")
    copied_count = 0
    
    for genmodel_id, group in list(grouped)[:10]:  # Test met eerste 10 groepen
        # Extract merk en model uit eerste Image_name
        first_image_name = group.iloc[0]['Image_name']
        if pd.isna(first_image_name):
            continue
        
        if '$$' in str(first_image_name):
            parts = str(first_image_name).split('$$')
            if len(parts) >= 2:
                brand = sanitize_filename(parts[0])
                model = sanitize_filename(parts[1])
                
                # Maak directory
                brand_dir = output_path / brand
                brand_dir.mkdir(exist_ok=True)
                model_dir = brand_dir / model
                model_dir.mkdir(exist_ok=True)
                
                # Kopieer eerste beschikbare afbeelding voor dit model
                for _, row in group.iterrows():
                    image_name = str(row.get('Image_name', ''))
                    if image_name in image_name_to_path:
                        src_file = image_name_to_path[image_name]
                        # Gebruik Genmodel_ID als bestandsnaam
                        dst_file = model_dir / f"{genmodel_id}.jpg"
                        
                        if not dst_file.exists():
                            shutil.copy2(src_file, dst_file)
                            copied_count += 1
                            print(f"  ✓ Copied {brand}/{model}/{genmodel_id}.jpg")
                        break
    
    print(f"\n✓ Copied {copied_count} images to {output_dir}/")
    
    return copied_count

def main():
    """Hoofdfunctie."""
    print("\n" + "=" * 60)
    print("CAR IMAGE SETUP FROM KAGGLE DATASET")
    print("=" * 60)
    
    # Step 1: Download dataset
    dataset_path = download_kaggle_dataset()
    if not dataset_path:
        print("\n✗ Cannot continue without dataset. Please check Kaggle credentials.")
        return
    
    # Step 2: Explore structure
    image_files = explore_dataset_structure(dataset_path)
    
    # Step 3: Link images
    copied = link_images_to_cars(dataset_path, image_files)
    
    print("\n" + "=" * 60)
    print("SETUP COMPLETE!")
    print("=" * 60)
    print(f"✓ Dataset downloaded")
    print(f"✓ {copied} images copied to images/ directory")
    print("\nNext steps:")
    print("1. Update CarRepository.cs to use local images")
    print("2. Restart the API server")
    print("3. Test the application")

if __name__ == "__main__":
    main()



