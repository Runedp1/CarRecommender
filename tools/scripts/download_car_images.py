"""
Script om auto-afbeeldingen te downloaden van Kaggle dataset.
Deze dataset bevat echte auto-afbeeldingen die we kunnen gebruiken in de applicatie.
"""
import kagglehub
import os
from pathlib import Path

def download_car_images():
    """Download de cars-image-dataset van Kaggle."""
    print("Downloading car images dataset from Kaggle...")
    
    try:
        # Download latest version
        path = kagglehub.dataset_download("kshitij192/cars-image-dataset")
        print(f"Path to dataset files: {path}")
        
        # Bekijk de structuur van de dataset
        dataset_path = Path(path)
        print(f"\nDataset structure:")
        for item in dataset_path.iterdir():
            print(f"  - {item.name}")
            if item.is_dir():
                # Toon eerste paar bestanden in subdirectories
                files = list(item.iterdir())[:5]
                for file in files:
                    print(f"    - {file.name}")
                if len(list(item.iterdir())) > 5:
                    print(f"    ... ({len(list(item.iterdir())) - 5} more files)")
        
        return str(path)
    except Exception as e:
        print(f"Error downloading dataset: {e}")
        return None

if __name__ == "__main__":
    download_car_images()



