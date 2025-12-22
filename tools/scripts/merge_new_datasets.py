"""
Script om 3 nieuwe datasets te mergen met Cleaned_Car_Data_For_App_Fully_Enriched.csv:
1. 2023 Car Dataset.csv - Moderne auto's met veel detail
2. Car_Models.csv - Luxe auto modellen
3. cars_dataset.csv - UK auto dataset met veel praktische info

Voegt nieuwe kolommen toe en vult ontbrekende waarden aan.
"""

import pandas as pd
import numpy as np
import re
import os

# Bepaal data directory (relatief ten opzichte van script locatie)
script_dir = os.path.dirname(os.path.abspath(__file__))
project_root = os.path.dirname(script_dir)
data_dir = os.path.join(project_root, 'data')

print("=" * 80)
print("MERGE NIEUWE DATASETS MET FULLY ENRICHED DATASET")
print("=" * 80)
print(f"Data directory: {data_dir}")

# Bepaal data directory (relatief ten opzichte van script locatie)
import os
script_dir = os.path.dirname(os.path.abspath(__file__))
project_root = os.path.dirname(script_dir)
data_dir = os.path.join(project_root, 'data')

# Step 1: Laad de basis dataset en nieuwe datasets
print("\n[Step 1] Laden van datasets...")
print(f"  Data directory: {data_dir}")
df_base = pd.read_csv(os.path.join(data_dir, 'Cleaned_Car_Data_For_App_Fully_Enriched.csv'))
print(f"  [OK] Cleaned_Car_Data_For_App_Fully_Enriched.csv: {len(df_base)} rijen")

print("  Laden 2023 Car Dataset.csv...")
df_2023 = pd.read_csv(os.path.join(data_dir, '2023 Car Dataset.csv'))
print(f"  [OK] 2023 Car Dataset.csv: {len(df_2023)} rijen")

print("  Laden Car_Models.csv...")
df_models = pd.read_csv(os.path.join(data_dir, 'Car_Models.csv'))
print(f"  [OK] Car_Models.csv: {len(df_models)} rijen")

print("  Laden cars_dataset.csv...")
df_cars = pd.read_csv(os.path.join(data_dir, 'cars_dataset.csv'))
print(f"  [OK] cars_dataset.csv: {len(df_cars)} rijen")

# Step 2: Normaliseer brandnamen
print("\n[Step 2] Normaliseren van brandnamen...")

brand_mapping = {
    'HYUNDAI': 'hyundai', 'DAIMLER AG': 'mercedes-benz', 'MERCEDES-BENZ': 'mercedes-benz',
    'MERCEDES': 'mercedes-benz', 'SMART': 'smart', 'RENAULT': 'renault', 'BMW': 'bmw',
    'AUDI': 'audi', 'VOLKSWAGEN': 'volkswagen', 'FORD': 'ford', 'OPEL': 'opel',
    'PEUGEOT': 'peugeot', 'CITROEN': 'citroen', 'TOYOTA': 'toyota', 'HONDA': 'honda',
    'NISSAN': 'nissan', 'MAZDA': 'mazda', 'VOLVO': 'volvo', 'SKODA': 'skoda',
    'SEAT': 'seat', 'FIAT': 'fiat', 'ALFA ROMEO': 'alfa romeo', 'JAGUAR': 'jaguar',
    'LAND ROVER': 'land rover', 'MINI': 'mini', 'PORSCHE': 'porsche', 'TESLA': 'tesla',
    'KIA': 'kia', 'GENESIS': 'genesis', 'LEXUS': 'lexus', 'INFINITI': 'infiniti',
    'ACURA': 'acura', 'CHEVROLET': 'chevrolet', 'CADILLAC': 'cadillac', 'BUICK': 'buick',
    'CHRYSLER': 'chrysler', 'DODGE': 'dodge', 'JEEP': 'jeep', 'FERRARI': 'ferrari',
    'ROLLS ROYCE': 'rolls-royce', 'ASTON MARTIN': 'aston martin', 'BENTLEY': 'bentley',
    'LAMBORGHINI': 'lamborghini', 'CHEVROLET': 'chevrolet'
}

def normalize_brand(brand):
    """Normaliseer brand naam"""
    if pd.isna(brand):
        return ""
    brand_str = str(brand).strip().upper()
    return brand_mapping.get(brand_str, str(brand).lower().strip())

def normalize_model_name(name):
    """Normaliseer model naam voor matching"""
    if pd.isna(name):
        return ""
    name = str(name).lower().strip()
    name = re.sub(r'[^\w\s-]', '', name)
    name = re.sub(r'\s+', ' ', name)
    return name

# Normaliseer in alle datasets
df_base['merk_normalized'] = df_base['merk'].apply(normalize_brand)
df_base['model_normalized'] = df_base['model'].apply(normalize_model_name)

# 2023 Car Dataset
df_2023['Car Make'] = df_2023[' Car Make '].str.strip()
df_2023['Car Model'] = df_2023[' Car Model   '].str.strip()
df_2023['Year'] = df_2023[' Year '].astype(str).str.strip().replace('nan', np.nan)
df_2023['Year'] = pd.to_numeric(df_2023['Year'], errors='coerce')
df_2023['Brand_normalized'] = df_2023['Car Make'].apply(normalize_brand)
df_2023['Model_normalized'] = df_2023['Car Model'].apply(normalize_model_name)

# Car_Models
df_models['Brand_normalized'] = df_models['Company'].apply(normalize_brand)
df_models['Model_normalized'] = df_models['Model'].apply(normalize_model_name)
# Extract year from Model Year Range (bijv. "2018 - Present" -> 2018)
def extract_year_from_range(year_range):
    if pd.isna(year_range):
        return np.nan
    match = re.search(r'(\d{4})', str(year_range))
    return int(match.group(1)) if match else np.nan
df_models['Year'] = df_models['Model Year Range'].apply(extract_year_from_range)

# cars_dataset
df_cars['Brand_normalized'] = df_cars['Make'].apply(normalize_brand)
df_cars['Model_normalized'] = df_cars['model'].apply(normalize_model_name)
df_cars['Year'] = pd.to_numeric(df_cars['year'], errors='coerce')

print(f"  [OK] Brandnamen genormaliseerd")

# Step 3: Bereid 2023 Car Dataset voor
print("\n[Step 3] Bereiden van 2023 Car Dataset...")

def extract_horsepower(hp_str):
    """Extraheer numerieke horsepower waarde"""
    if pd.isna(hp_str):
        return np.nan
    hp_str = str(hp_str).lower().replace('hp', '').replace('(est.)', '').strip()
    if '-' in hp_str:
        parts = hp_str.split('-')
        try:
            return (float(parts[0]) + float(parts[1])) / 2
        except:
            return np.nan
    try:
        return float(hp_str)
    except:
        return np.nan

def extract_torque(torque_str):
    """Extraheer numerieke torque waarde (Nm)"""
    if pd.isna(torque_str):
        return np.nan
    torque_str = str(torque_str).lower().replace('nm', '').replace('(est.)', '').strip()
    if '-' in torque_str:
        parts = torque_str.split('-')
        try:
            return (float(parts[0]) + float(parts[1])) / 2
        except:
            return np.nan
    try:
        return float(torque_str)
    except:
        return np.nan

def extract_acceleration(acc_str):
    """Extraheer acceleration waarde (0-60 mph in seconden)"""
    if pd.isna(acc_str):
        return np.nan
    acc_str = str(acc_str).lower().replace('seconds', '').replace('sec', '').strip()
    try:
        return float(acc_str)
    except:
        return np.nan

def extract_price(price_str):
    """Extraheer prijs waarde (verwijder $ en komma's)"""
    if pd.isna(price_str):
        return np.nan
    price_str = str(price_str).replace('$', '').replace(',', '').strip()
    try:
        return float(price_str)
    except:
        return np.nan

def extract_engine_size(eng_str):
    """Extraheer engine size in liters"""
    if pd.isna(eng_str):
        return np.nan
    eng_str = str(eng_str).lower().replace('l', '').strip()
    if eng_str == 'n/a':
        return np.nan
    try:
        return float(eng_str)
    except:
        return np.nan

# Extraheer numerieke waarden uit 2023 dataset
df_2023['Horsepower_numeric'] = df_2023[' Horsepower '].apply(extract_horsepower)
df_2023['Torque_Nm_numeric'] = df_2023[' Torque (Nm) '].apply(extract_torque)
df_2023['Acceleration_numeric'] = df_2023[' Acceleration (0-60 mph) '].apply(extract_acceleration)
df_2023['Price_numeric'] = df_2023[' Price ($) '].apply(extract_price)
df_2023['Engine_Size_L_numeric'] = df_2023[' Engine Size (L) '].apply(extract_engine_size)
df_2023['Top_Speed_mph'] = pd.to_numeric(df_2023[' Top Speed (mph) '].astype(str).str.replace('(est.)', '').str.strip(), errors='coerce')
df_2023['Mileage_MPG'] = pd.to_numeric(df_2023[' Mileage (MPG) '].astype(str).str.replace('(est.)', '').str.strip(), errors='coerce')

# Converteer horsepower naar KW (1 HP ≈ 0.7457 KW)
df_2023['Power_KW'] = df_2023['Horsepower_numeric'] * 0.7457

# Groepeer per brand, model, year
df_2023_grouped = df_2023.groupby(['Brand_normalized', 'Model_normalized', 'Year']).agg({
    'Power_KW': 'median',
    'Torque_Nm_numeric': 'median',
    'Acceleration_numeric': 'median',
    'Price_numeric': 'median',
    'Engine_Size_L_numeric': 'median',
    'Top_Speed_mph': 'median',
    'Mileage_MPG': 'median',
    ' Transmission Type ': lambda x: x.mode()[0] if len(x.mode()) > 0 else np.nan,
    ' Fuel Type ': lambda x: x.mode()[0] if len(x.mode()) > 0 else np.nan,
    ' Body Type ': lambda x: x.mode()[0] if len(x.mode()) > 0 else np.nan,
}).reset_index()

# Ook zonder year voor bredere matching
df_2023_no_year = df_2023.groupby(['Brand_normalized', 'Model_normalized']).agg({
    'Power_KW': 'median',
    'Torque_Nm_numeric': 'median',
    'Acceleration_numeric': 'median',
    'Price_numeric': 'median',
    'Engine_Size_L_numeric': 'median',
    'Top_Speed_mph': 'median',
    'Mileage_MPG': 'median',
    ' Transmission Type ': lambda x: x.mode()[0] if len(x.mode()) > 0 else np.nan,
    ' Fuel Type ': lambda x: x.mode()[0] if len(x.mode()) > 0 else np.nan,
    ' Body Type ': lambda x: x.mode()[0] if len(x.mode()) > 0 else np.nan,
}).reset_index()

print(f"  [OK] 2023 dataset voorbereid: {len(df_2023_grouped)} met year, {len(df_2023_no_year)} zonder year")

# Step 4: Bereid Car_Models voor
print("\n[Step 4] Bereiden van Car_Models dataset...")

def extract_hp_car_models(hp_str):
    """Extraheer horsepower uit Car_Models format"""
    if pd.isna(hp_str):
        return np.nan
    hp_str = str(hp_str).lower().replace('hp', '').strip()
    try:
        return float(hp_str)
    except:
        return np.nan

def extract_torque_car_models(torque_str):
    """Extraheer torque (kan lb-ft of Nm zijn)"""
    if pd.isna(torque_str):
        return np.nan
    torque_str = str(torque_str).lower()
    # Check if lb-ft
    if 'lb-ft' in torque_str:
        torque_str = torque_str.replace('lb-ft', '').strip()
        try:
            lb_ft = float(torque_str)
            return lb_ft * 1.35582  # Convert lb-ft to Nm
        except:
            return np.nan
    elif 'nm' in torque_str:
        torque_str = torque_str.replace('nm', '').strip()
        try:
            return float(torque_str)
        except:
            return np.nan
    else:
        try:
            return float(torque_str)
        except:
            return np.nan

def extract_price_car_models(price_str):
    """Extraheer prijs uit Car_Models format"""
    if pd.isna(price_str):
        return np.nan
    price_str = str(price_str).replace('$', '').replace(',', '').strip()
    try:
        return float(price_str)
    except:
        return np.nan

def extract_engine_size_car_models(eng_str):
    """Extraheer engine size (bijv. '3.9L V8' -> 3.9)"""
    if pd.isna(eng_str):
        return np.nan
    match = re.search(r'(\d+\.?\d*)\s*L', str(eng_str), re.IGNORECASE)
    if match:
        try:
            return float(match.group(1))
        except:
            return np.nan
    return np.nan

df_models['Horsepower_numeric'] = df_models['Horsepower'].apply(extract_hp_car_models)
df_models['Torque_Nm_numeric'] = df_models['Torque'].apply(extract_torque_car_models)
df_models['Price_numeric'] = df_models['Price'].apply(extract_price_car_models)
df_models['Engine_Size_L_numeric'] = df_models['Engine Type'].apply(extract_engine_size_car_models)
df_models['Power_KW'] = df_models['Horsepower_numeric'] * 0.7457

# Converteer Number of Cylinders en Number of Doors naar numeriek
df_models['Number_of_Cylinders_numeric'] = pd.to_numeric(df_models['Number of Cylinders'], errors='coerce')
df_models['Number_of_Doors_numeric'] = pd.to_numeric(df_models['Number of Doors'], errors='coerce')

# Groepeer per brand, model, year
df_models_grouped = df_models.groupby(['Brand_normalized', 'Model_normalized', 'Year']).agg({
    'Power_KW': 'median',
    'Torque_Nm_numeric': 'median',
    'Price_numeric': 'median',
    'Engine_Size_L_numeric': 'median',
    'Number_of_Cylinders_numeric': 'median',
    'Number_of_Doors_numeric': 'median',
    'Transmission Type': lambda x: x.mode()[0] if len(x.mode()) > 0 else np.nan,
    'Body Type': lambda x: x.mode()[0] if len(x.mode()) > 0 else np.nan,
}).reset_index()

# Ook zonder year
df_models_no_year = df_models.groupby(['Brand_normalized', 'Model_normalized']).agg({
    'Power_KW': 'median',
    'Torque_Nm_numeric': 'median',
    'Price_numeric': 'median',
    'Engine_Size_L_numeric': 'median',
    'Number_of_Cylinders_numeric': 'median',
    'Number_of_Doors_numeric': 'median',
    'Transmission Type': lambda x: x.mode()[0] if len(x.mode()) > 0 else np.nan,
    'Body Type': lambda x: x.mode()[0] if len(x.mode()) > 0 else np.nan,
}).reset_index()

print(f"  [OK] Car_Models dataset voorbereid: {len(df_models_grouped)} met year, {len(df_models_no_year)} zonder year")

# Step 5: Bereid cars_dataset voor
print("\n[Step 5] Bereiden van cars_dataset...")

# Normaliseer fuel types
fuel_mapping = {
    'petrol': 'petrol',
    'diesel': 'diesel',
    'electric': 'electric',
    'hybrid': 'hybrid',
    'plug in hybrid': 'hybrid',
    'other': 'other'
}

def normalize_fuel(fuel):
    if pd.isna(fuel):
        return np.nan
    fuel_lower = str(fuel).lower().strip()
    return fuel_mapping.get(fuel_lower, fuel_lower)

df_cars['fuelType_normalized'] = df_cars['fuelType'].apply(normalize_fuel)

# Converteer engineSize naar liters (als het in liters is)
df_cars['Engine_Size_L_numeric'] = pd.to_numeric(df_cars['engineSize'], errors='coerce')

# Groepeer per brand, model, year
df_cars_grouped = df_cars.groupby(['Brand_normalized', 'Model_normalized', 'Year']).agg({
    'price': 'median',
    'mileage': 'median',
    'mpg': 'median',
    'tax': 'median',
    'Engine_Size_L_numeric': 'median',
    'transmission': lambda x: x.mode()[0] if len(x.mode()) > 0 else np.nan,
    'fuelType_normalized': lambda x: x.mode()[0] if len(x.mode()) > 0 else np.nan,
}).reset_index()

# Ook zonder year
df_cars_no_year = df_cars.groupby(['Brand_normalized', 'Model_normalized']).agg({
    'price': 'median',
    'mileage': 'median',
    'mpg': 'median',
    'tax': 'median',
    'Engine_Size_L_numeric': 'median',
    'transmission': lambda x: x.mode()[0] if len(x.mode()) > 0 else np.nan,
    'fuelType_normalized': lambda x: x.mode()[0] if len(x.mode()) > 0 else np.nan,
}).reset_index()

print(f"  [OK] cars_dataset voorbereid: {len(df_cars_grouped)} met year, {len(df_cars_no_year)} zonder year")

# Step 6: Merge alle datasets
print("\n[Step 6] Mergen van datasets...")
df_final = df_base.copy()

# Voeg nieuwe kolommen toe als ze nog niet bestaan
new_columns = {
    'Top_Speed_mph': np.nan,
    'Mileage_MPG': np.nan,
    'Acceleration_0_60_sec': np.nan,
    'Number_of_Cylinders': np.nan,
    'Number_of_Doors': np.nan,
    'Tax': np.nan,
    'MPG': np.nan,
}

for col, default_val in new_columns.items():
    if col not in df_final.columns:
        df_final[col] = default_val

# Merge 1: 2023 Car Dataset met year
print("  Mergen 2023 Car Dataset (met year)...")
df_final = df_final.reset_index(drop=True)
merged_2023 = df_final.merge(
    df_2023_grouped,
    left_on=['merk_normalized', 'model_normalized', 'bouwjaar'],
    right_on=['Brand_normalized', 'Model_normalized', 'Year'],
    how='left',
    suffixes=('', '_2023'),
    indicator=True
)

mask_2023 = merged_2023['_merge'] == 'both'

df_final.loc[mask_2023, 'Top_Speed_mph'] = merged_2023.loc[mask_2023, 'Top_Speed_mph'].values
df_final.loc[mask_2023, 'Mileage_MPG'] = merged_2023.loc[mask_2023, 'Mileage_MPG'].values
df_final.loc[mask_2023, 'Acceleration_0_60_sec'] = merged_2023.loc[mask_2023, 'Acceleration_numeric'].values

# Update vermogen als nog leeg
vermogen_mask = ((df_final['vermogen'].isna()) | (df_final['vermogen'] == 0)) & mask_2023
df_final.loc[vermogen_mask, 'vermogen'] = merged_2023.loc[vermogen_mask, 'Power_KW'].values

# Update Torque_Nm als nog leeg
torque_mask = df_final['Torque_Nm'].isna() & merged_2023['Torque_Nm_numeric'].notna() & mask_2023
df_final.loc[torque_mask, 'Torque_Nm'] = merged_2023.loc[torque_mask, 'Torque_Nm_numeric'].values

# Update Transmission als nog leeg (als kolom bestaat)
if ' Transmission Type ' in merged_2023.columns:
    trans_mask = df_final['Transmission'].isna() & merged_2023[' Transmission Type '].notna() & mask_2023
    df_final.loc[trans_mask, 'Transmission'] = merged_2023.loc[trans_mask, ' Transmission Type '].astype(str).str.strip().values

# Update Engine_Size_L als nog leeg
eng_mask = df_final['Engine_Size_L'].isna() & merged_2023['Engine_Size_L_numeric'].notna() & mask_2023
df_final.loc[eng_mask, 'Engine_Size_L'] = merged_2023.loc[eng_mask, 'Engine_Size_L_numeric'].values

print(f"    Matched {mask_2023.sum()} rijen met year")

# Merge 2: 2023 Car Dataset zonder year (voor nog niet gematchte)
print("  Mergen 2023 Car Dataset (zonder year)...")
unmatched_2023 = df_final['Top_Speed_mph'].isna()
if unmatched_2023.sum() > 0:
    merged_2023_no_year = df_final.merge(
        df_2023_no_year,
        left_on=['merk_normalized', 'model_normalized'],
        right_on=['Brand_normalized', 'Model_normalized'],
        how='left',
        suffixes=('', '_2023_no_year'),
        indicator=True
    )
    
    mask_2023_no_year = (merged_2023_no_year['_merge'] == 'both') & unmatched_2023
    
    df_final.loc[mask_2023_no_year, 'Top_Speed_mph'] = merged_2023_no_year.loc[mask_2023_no_year, 'Top_Speed_mph'].values
    df_final.loc[mask_2023_no_year, 'Mileage_MPG'] = merged_2023_no_year.loc[mask_2023_no_year, 'Mileage_MPG'].values
    df_final.loc[mask_2023_no_year, 'Acceleration_0_60_sec'] = merged_2023_no_year.loc[mask_2023_no_year, 'Acceleration_numeric'].values
    
    # Update vermogen
    vermogen_mask2 = ((df_final['vermogen'].isna()) | (df_final['vermogen'] == 0)) & mask_2023_no_year
    df_final.loc[vermogen_mask2, 'vermogen'] = merged_2023_no_year.loc[vermogen_mask2, 'Power_KW'].values
    
    # Update andere velden
    torque_mask2 = df_final['Torque_Nm'].isna() & merged_2023_no_year['Torque_Nm_numeric'].notna() & mask_2023_no_year
    df_final.loc[torque_mask2, 'Torque_Nm'] = merged_2023_no_year.loc[torque_mask2, 'Torque_Nm_numeric'].values
    
    print(f"    Matched {mask_2023_no_year.sum()} extra rijen zonder year")

# Merge 3: Car_Models met year
print("  Mergen Car_Models (met year)...")
merged_models = df_final.merge(
    df_models_grouped,
    left_on=['merk_normalized', 'model_normalized', 'bouwjaar'],
    right_on=['Brand_normalized', 'Model_normalized', 'Year'],
    how='left',
    suffixes=('', '_models'),
    indicator=True
)

mask_models = merged_models['_merge'] == 'both'
df_final.loc[mask_models, 'Number_of_Cylinders'] = merged_models.loc[mask_models, 'Number_of_Cylinders_numeric'].values
df_final.loc[mask_models, 'Number_of_Doors'] = merged_models.loc[mask_models, 'Number_of_Doors_numeric'].values

# Update vermogen als nog leeg
vermogen_mask3 = ((df_final['vermogen'].isna()) | (df_final['vermogen'] == 0)) & mask_models
df_final.loc[vermogen_mask3, 'vermogen'] = merged_models.loc[vermogen_mask3, 'Power_KW'].values

# Update Torque_Nm
torque_mask3 = df_final['Torque_Nm'].isna() & merged_models['Torque_Nm_numeric'].notna() & mask_models
df_final.loc[torque_mask3, 'Torque_Nm'] = merged_models.loc[torque_mask3, 'Torque_Nm_numeric'].values

# Update Engine_Size_L
eng_mask3 = df_final['Engine_Size_L'].isna() & merged_models['Engine_Size_L_numeric'].notna() & mask_models
df_final.loc[eng_mask3, 'Engine_Size_L'] = merged_models.loc[eng_mask3, 'Engine_Size_L_numeric'].values

print(f"    Matched {mask_models.sum()} rijen met year")

# Merge 4: Car_Models zonder year
print("  Mergen Car_Models (zonder year)...")
unmatched_models = df_final['Number_of_Cylinders'].isna()
if unmatched_models.sum() > 0:
    merged_models_no_year = df_final.merge(
        df_models_no_year,
        left_on=['merk_normalized', 'model_normalized'],
        right_on=['Brand_normalized', 'Model_normalized'],
        how='left',
        suffixes=('', '_models_no_year'),
        indicator=True
    )
    
    mask_models_no_year = (merged_models_no_year['_merge'] == 'both') & unmatched_models
    
    df_final.loc[mask_models_no_year, 'Number_of_Cylinders'] = merged_models_no_year.loc[mask_models_no_year, 'Number_of_Cylinders_numeric'].values
    df_final.loc[mask_models_no_year, 'Number_of_Doors'] = merged_models_no_year.loc[mask_models_no_year, 'Number_of_Doors_numeric'].values
    
    # Update vermogen
    vermogen_mask4 = ((df_final['vermogen'].isna()) | (df_final['vermogen'] == 0)) & mask_models_no_year
    df_final.loc[vermogen_mask4, 'vermogen'] = merged_models_no_year.loc[vermogen_mask4, 'Power_KW'].values
    
    print(f"    Matched {mask_models_no_year.sum()} extra rijen zonder year")

# Merge 5: cars_dataset met year
print("  Mergen cars_dataset (met year)...")
merged_cars = df_final.merge(
    df_cars_grouped,
    left_on=['merk_normalized', 'model_normalized', 'bouwjaar'],
    right_on=['Brand_normalized', 'Model_normalized', 'Year'],
    how='left',
    suffixes=('', '_cars'),
    indicator=True
)

mask_cars = merged_cars['_merge'] == 'both'
df_final.loc[mask_cars, 'Tax'] = merged_cars.loc[mask_cars, 'tax'].values
df_final.loc[mask_cars, 'MPG'] = merged_cars.loc[mask_cars, 'mpg'].values

# Update Mileage als nog leeg
mileage_mask = df_final['Mileage'].isna() & merged_cars['mileage'].notna() & mask_cars
df_final.loc[mileage_mask, 'Mileage'] = merged_cars.loc[mileage_mask, 'mileage'].values

# Update Transmission als nog leeg
trans_mask2 = df_final['Transmission'].isna() & merged_cars['transmission'].notna() & mask_cars
df_final.loc[trans_mask2, 'Transmission'] = merged_cars.loc[trans_mask2, 'transmission'].values

# Update Engine_Size_L als nog leeg
eng_mask4 = df_final['Engine_Size_L'].isna() & merged_cars['Engine_Size_L_numeric'].notna() & mask_cars
df_final.loc[eng_mask4, 'Engine_Size_L'] = merged_cars.loc[eng_mask4, 'Engine_Size_L_numeric'].values

print(f"    Matched {mask_cars.sum()} rijen met year")

# Merge 6: cars_dataset zonder year
print("  Mergen cars_dataset (zonder year)...")
unmatched_cars = df_final['Tax'].isna()
if unmatched_cars.sum() > 0:
    merged_cars_no_year = df_final.merge(
        df_cars_no_year,
        left_on=['merk_normalized', 'model_normalized'],
        right_on=['Brand_normalized', 'Model_normalized'],
        how='left',
        suffixes=('', '_cars_no_year'),
        indicator=True
    )
    
    mask_cars_no_year = (merged_cars_no_year['_merge'] == 'both') & unmatched_cars
    
    df_final.loc[mask_cars_no_year, 'Tax'] = merged_cars_no_year.loc[mask_cars_no_year, 'tax'].values
    df_final.loc[mask_cars_no_year, 'MPG'] = merged_cars_no_year.loc[mask_cars_no_year, 'mpg'].values
    
    # Update Mileage
    mileage_mask2 = df_final['Mileage'].isna() & merged_cars_no_year['mileage'].notna() & mask_cars_no_year
    df_final.loc[mileage_mask2, 'Mileage'] = merged_cars_no_year.loc[mileage_mask2, 'mileage'].values
    
    print(f"    Matched {mask_cars_no_year.sum()} extra rijen zonder year")

# Step 7: Cleanup en save
print("\n[Step 7] Opslaan van resultaat...")

# Verwijder temporary kolommen (maar behoud de nieuwe kolommen die we hebben toegevoegd!)
columns_to_drop = ['merk_normalized', 'model_normalized', 'Brand_normalized', 'Model_normalized', 
                   'Year', 'Power_KW', 'Torque_Nm_numeric', 'Acceleration_numeric', 
                   'Price_numeric', 'Engine_Size_L_numeric',
                   'price', 'fuelType_normalized', '_merge', 
                   'Number_of_Cylinders_numeric', 'Number_of_Doors_numeric']
# Verwijder alleen kolommen die tijdelijk zijn en niet de uiteindelijke nieuwe kolommen
df_final = df_final.drop(columns=[col for col in columns_to_drop if col in df_final.columns])

output_filename = os.path.join(data_dir, 'Cleaned_Car_Data_For_App_Fully_Enriched.csv')
df_final.to_csv(output_filename, index=False)

print(f"  [OK] Opgeslagen naar {output_filename}")

# Step 8: Summary
print("\n" + "=" * 80)
print("MERGE SAMENVATTING")
print("=" * 80)

print(f"\nBasis dataset: {len(df_base)} rijen")

print(f"\nNieuwe kolommen toegevoegd/geüpdatet:")
print(f"  Top_Speed_mph: {df_final['Top_Speed_mph'].notna().sum()} non-null waarden")
print(f"  Mileage_MPG: {df_final['Mileage_MPG'].notna().sum()} non-null waarden")
print(f"  Acceleration_0_60_sec: {df_final['Acceleration_0_60_sec'].notna().sum()} non-null waarden")
print(f"  Number_of_Cylinders: {df_final['Number_of_Cylinders'].notna().sum()} non-null waarden")
print(f"  Number_of_Doors: {df_final['Number_of_Doors'].notna().sum()} non-null waarden")
print(f"  Tax: {df_final['Tax'].notna().sum()} non-null waarden")
print(f"  MPG: {df_final['MPG'].notna().sum()} non-null waarden")

missing_before = df_base['vermogen'].isna().sum() + (df_base['vermogen'] == 0).sum()
missing_after = df_final['vermogen'].isna().sum() + (df_final['vermogen'] == 0).sum()
filled_vermogen = missing_before - missing_after

print(f"\nVermogen verbeteringen:")
print(f"  Voor merge: {missing_before} lege/nul waarden")
print(f"  Na merge: {missing_after} lege/nul waarden")
print(f"  [OK] {filled_vermogen} extra vermogen waarden ingevuld")

print("\n" + "=" * 80)
print("[OK] MERGE VOLTOOID!")
print("=" * 80)
