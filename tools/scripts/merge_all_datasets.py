"""
Comprehensive script to merge ALL available datasets:
1. vehicles.csv - Technical specs (CO2, electric range, power)
2. car_price_prediction_.csv - Price, transmission, mileage, condition
3. CarsDatasets 2025.csv - Horsepower, torque, performance, seats
4. Cleaned_Car_Data_For_App.csv - Original dataset

Creates a fully enriched dataset with all available information
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
print("COMPREHENSIVE DATABASE MERGE AND ENRICHMENT SCRIPT")
print("=" * 80)
print(f"Data directory: {data_dir}")

# Step 1: Load all datasets
print("\n[Step 1] Loading all datasets...")
df_cleaned = pd.read_csv(os.path.join(data_dir, 'Cleaned_Car_Data_For_App_Enriched.csv'))
print(f"  [OK] Loaded Cleaned_Car_Data_For_App_Enriched.csv: {len(df_cleaned)} rows")

print("  Loading vehicles.csv...")
df_vehicles = pd.read_csv(os.path.join(data_dir, 'vehicles.csv'))
print(f"  [OK] Loaded vehicles.csv: {len(df_vehicles)} rows")

print("  Loading car_price_prediction_.csv...")
df_price = pd.read_csv(os.path.join(data_dir, 'car_price_prediction_.csv'))
print(f"  [OK] Loaded car_price_prediction_.csv: {len(df_price)} rows")

print("  Loading CarsDatasets 2025.csv...")
df_cars2025 = pd.read_csv(os.path.join(data_dir, 'CarsDatasets 2025.csv'), encoding='latin-1')
print(f"  [OK] Loaded CarsDatasets 2025.csv: {len(df_cars2025)} rows")

# Step 2: Normalize brand names
print("\n[Step 2] Normalizing brand names across all datasets...")

brand_mapping = {
    'HYUNDAI': 'hyundai',
    'DAIMLER AG': 'mercedes-benz',
    'MERCEDES-BENZ': 'mercedes-benz',
    'MERCEDES': 'mercedes-benz',
    'SMART': 'smart',
    'RENAULT': 'renault',
    'BMW': 'bmw',
    'AUDI': 'audi',
    'VOLKSWAGEN': 'volkswagen',
    'FORD': 'ford',
    'OPEL': 'opel',
    'PEUGEOT': 'peugeot',
    'CITROEN': 'citroen',
    'TOYOTA': 'toyota',
    'HONDA': 'honda',
    'NISSAN': 'nissan',
    'MAZDA': 'mazda',
    'VOLVO': 'volvo',
    'SKODA': 'skoda',
    'SEAT': 'seat',
    'FIAT': 'fiat',
    'ALFA ROMEO': 'alfa romeo',
    'JAGUAR': 'jaguar',
    'LAND ROVER': 'land rover',
    'MINI': 'mini',
    'PORSCHE': 'porsche',
    'TESLA': 'tesla',
    'KIA': 'kia',
    'GENESIS': 'genesis',
    'LEXUS': 'lexus',
    'INFINITI': 'infiniti',
    'ACURA': 'acura',
    'CHEVROLET': 'chevrolet',
    'CADILLAC': 'cadillac',
    'BUICK': 'buick',
    'CHRYSLER': 'chrysler',
    'DODGE': 'dodge',
    'JEEP': 'jeep',
    'FERRARI': 'ferrari',
    'ROLLS ROYCE': 'rolls-royce',
    'ASTON MARTIN': 'aston martin',
    'BENTLEY': 'bentley',
    'LAMBORGHINI': 'lamborghini',
}

def normalize_brand(brand):
    """Normalize brand name"""
    if pd.isna(brand):
        return ""
    brand_upper = str(brand).upper().strip()
    return brand_mapping.get(brand_upper, str(brand).lower().strip())

# Normalize brands in all datasets
df_cleaned['merk_normalized'] = df_cleaned['merk'].apply(normalize_brand)
df_vehicles['Brand_normalized'] = df_vehicles['Brand'].apply(normalize_brand)
df_price['Brand_normalized'] = df_price['Brand'].apply(normalize_brand)
df_cars2025['Brand_normalized'] = df_cars2025['Company Names'].apply(normalize_brand)

print(f"  [OK] Normalized brands across all datasets")

# Step 3: Normalize model names
print("\n[Step 3] Normalizing model names...")

def normalize_model_name(name):
    """Normalize model name for matching"""
    if pd.isna(name):
        return ""
    name = str(name).lower().strip()
    name = re.sub(r'[^\w\s-]', '', name)
    name = re.sub(r'\s+', ' ', name)
    name = name.replace('eq ', '').replace(' eq', '')
    name = name.replace('e-', '').replace('e ', '')
    return name

df_cleaned['model_normalized'] = df_cleaned['model'].apply(normalize_model_name)
df_vehicles['Model_normalized'] = df_vehicles['Veh_Model'].apply(normalize_model_name)
df_price['Model_normalized'] = df_price['Model'].apply(normalize_model_name)
df_cars2025['Model_normalized'] = df_cars2025['Cars Names'].apply(normalize_model_name)

print(f"  [OK] Normalized model names")

# Step 4: Prepare car_price_prediction_.csv data
print("\n[Step 4] Preparing car_price_prediction_.csv data...")

# Group by brand, model, year to get aggregated values
df_price_grouped = df_price.groupby(['Brand_normalized', 'Model_normalized', 'Year']).agg({
    'Price': lambda x: x.median() if x.notna().sum() > 0 else np.nan,
    'Engine Size': lambda x: x.median() if x.notna().sum() > 0 else np.nan,
    'Transmission': lambda x: x.mode()[0] if len(x.mode()) > 0 else np.nan,
    'Mileage': lambda x: x.median() if x.notna().sum() > 0 else np.nan,
    'Condition': lambda x: x.mode()[0] if len(x.mode()) > 0 else np.nan,
}).reset_index()

# Also create a version without year for broader matching
df_price_no_year = df_price.groupby(['Brand_normalized', 'Model_normalized']).agg({
    'Price': lambda x: x.median() if x.notna().sum() > 0 else np.nan,
    'Engine Size': lambda x: x.median() if x.notna().sum() > 0 else np.nan,
    'Transmission': lambda x: x.mode()[0] if len(x.mode()) > 0 else np.nan,
    'Mileage': lambda x: x.median() if x.notna().sum() > 0 else np.nan,
}).reset_index()
df_price_no_year['Year'] = np.nan

print(f"  [OK] Prepared price data: {len(df_price_grouped)} with year, {len(df_price_no_year)} without year")

# Step 5: Prepare CarsDatasets 2025.csv data
print("\n[Step 5] Preparing CarsDatasets 2025.csv data...")

def extract_horsepower(hp_str):
    """Extract numeric horsepower value (e.g., '963 hp' -> 963, '70-85 hp' -> 77.5)"""
    if pd.isna(hp_str):
        return np.nan
    hp_str = str(hp_str).lower().replace('hp', '').strip()
    # Handle ranges like "70-85"
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
    """Extract numeric torque value (e.g., '800 Nm' -> 800, '100 - 140 Nm' -> 120)"""
    if pd.isna(torque_str):
        return np.nan
    torque_str = str(torque_str).lower().replace('nm', '').strip()
    # Handle ranges
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

def extract_performance(perf_str):
    """Extract numeric performance value (e.g., '2.5 sec' -> 2.5)"""
    if pd.isna(perf_str):
        return np.nan
    perf_str = str(perf_str).lower().replace('sec', '').strip()
    try:
        return float(perf_str)
    except:
        return np.nan

def extract_seats(seats_str):
    """Extract numeric seats value"""
    if pd.isna(seats_str):
        return np.nan
    seats_str = str(seats_str).strip()
    # Handle "2+2" format
    if '+' in seats_str:
        try:
            parts = seats_str.split('+')
            return int(parts[0]) + int(parts[1])
        except:
            pass
    try:
        return int(float(seats_str))
    except:
        return np.nan

# Extract numeric values
df_cars2025['HorsePower_numeric'] = df_cars2025['HorsePower'].apply(extract_horsepower)
df_cars2025['Torque_numeric'] = df_cars2025['Torque'].apply(extract_torque)
df_cars2025['Performance_numeric'] = df_cars2025['Performance(0 - 100 )KM/H'].apply(extract_performance)
df_cars2025['Seats_numeric'] = df_cars2025['Seats'].apply(extract_seats)

# Convert horsepower to KW (1 HP ≈ 0.7457 KW)
df_cars2025['Power_KW_from_HP'] = df_cars2025['HorsePower_numeric'] * 0.7457

# Group by brand and model (this dataset doesn't have year info)
df_cars2025_grouped = df_cars2025.groupby(['Brand_normalized', 'Model_normalized']).agg({
    'Power_KW_from_HP': lambda x: x.median() if x.notna().sum() > 0 else np.nan,
    'Torque_numeric': lambda x: x.median() if x.notna().sum() > 0 else np.nan,
    'Performance_numeric': lambda x: x.median() if x.notna().sum() > 0 else np.nan,
    'Seats_numeric': lambda x: x.median() if x.notna().sum() > 0 else np.nan,
}).reset_index()

print(f"  [OK] Prepared CarsDatasets 2025 data: {len(df_cars2025_grouped)} unique brand-model combinations")
print(f"    Extracted horsepower: {df_cars2025_grouped['Power_KW_from_HP'].notna().sum()} values")
print(f"    Extracted torque: {df_cars2025_grouped['Torque_numeric'].notna().sum()} values")

# Step 6: Prepare vehicles.csv data (reuse from previous script logic)
print("\n[Step 6] Preparing vehicles.csv data...")

df_vehicles_valid = df_vehicles[df_vehicles['Power_KW'].notna() & (df_vehicles['Power_KW'] > 0)].copy()

vehicles_no_year = df_vehicles_valid.groupby(['Brand_normalized', 'Model_normalized']).agg({
    'Power_KW': lambda x: x.median() if x.notna().sum() > 0 else np.nan,
    'CO2_wltp': lambda x: x.median() if x.notna().sum() > 0 else np.nan,
    'Electric range (km)': lambda x: x.median() if x.notna().sum() > 0 else np.nan,
    'El_Consumpt_whkm': lambda x: x.median() if x.notna().sum() > 0 else np.nan,
    'Engine_cm3': lambda x: x.median() if x.notna().sum() > 0 else np.nan,
}).reset_index()

print(f"  [OK] Prepared vehicles.csv data: {len(vehicles_no_year)} combinations")

# Step 7: Merge all datasets
print("\n[Step 7] Merging all datasets...")

df_final = df_cleaned.copy()

# Initialize new columns
new_columns = {
    'Transmission': np.nan,
    'Mileage': np.nan,
    'Condition': np.nan,
    'Torque_Nm': np.nan,
    'Performance_0_100_sec': np.nan,
    'Seats': np.nan,
    'Engine_Size_L': np.nan,
}

for col, default_val in new_columns.items():
    df_final[col] = default_val

# Merge 1: car_price_prediction with year
print("  Merging car_price_prediction_.csv (with year)...")
merged_price = df_final.merge(
    df_price_grouped,
    left_on=['merk_normalized', 'model_normalized', 'bouwjaar'],
    right_on=['Brand_normalized', 'Model_normalized', 'Year'],
    how='left',
    suffixes=('', '_price'),
    left_index=True
)

price_mask = merged_price['Price'].notna()
df_final.loc[price_mask, 'Transmission'] = merged_price.loc[price_mask, 'Transmission']
df_final.loc[price_mask, 'Mileage'] = merged_price.loc[price_mask, 'Mileage']
df_final.loc[price_mask, 'Condition'] = merged_price.loc[price_mask, 'Condition']
df_final.loc[price_mask, 'Engine_Size_L'] = merged_price.loc[price_mask, 'Engine Size']
print(f"    Matched {price_mask.sum()} rows")

# Merge 2: car_price_prediction without year (for unmatched)
print("  Merging car_price_prediction_.csv (without year)...")
unmatched_price = df_final['Transmission'].isna()
if unmatched_price.sum() > 0:
    merged_price_no_year = df_final[unmatched_price].merge(
        df_price_no_year,
        left_on=['merk_normalized', 'model_normalized'],
        right_on=['Brand_normalized', 'Model_normalized'],
        how='left',
        suffixes=('', '_price2')
    )
    
    price_mask2 = merged_price_no_year['Price'].notna()
    price_indices = df_final[unmatched_price].index[price_mask2]
    
    df_final.loc[price_indices, 'Transmission'] = merged_price_no_year.loc[price_mask2, 'Transmission'].values
    df_final.loc[price_indices, 'Mileage'] = merged_price_no_year.loc[price_mask2, 'Mileage'].values
    df_final.loc[price_indices, 'Engine_Size_L'] = merged_price_no_year.loc[price_mask2, 'Engine Size'].values
    print(f"    Matched {len(price_indices)} additional rows")

# Merge 3: CarsDatasets 2025
print("  Merging CarsDatasets 2025.csv...")
merged_cars2025 = df_final.merge(
    df_cars2025_grouped,
    left_on=['merk_normalized', 'model_normalized'],
    right_on=['Brand_normalized', 'Model_normalized'],
    how='left',
    suffixes=('', '_cars2025')
)

cars2025_mask = merged_cars2025['Power_KW_from_HP'].notna()
df_final.loc[cars2025_mask, 'Torque_Nm'] = merged_cars2025.loc[cars2025_mask, 'Torque_numeric'].values
df_final.loc[cars2025_mask, 'Performance_0_100_sec'] = merged_cars2025.loc[cars2025_mask, 'Performance_numeric'].values
df_final.loc[cars2025_mask, 'Seats'] = merged_cars2025.loc[cars2025_mask, 'Seats_numeric'].values
print(f"    Matched {cars2025_mask.sum()} rows")

# Merge 4: vehicles.csv (for CO2 and other technical specs)
print("  Merging vehicles.csv...")
merged_vehicles = df_final.merge(
    vehicles_no_year,
    left_on=['merk_normalized', 'model_normalized'],
    right_on=['Brand_normalized', 'Model_normalized'],
    how='left',
    suffixes=('', '_vehicles')
)

vehicles_mask = merged_vehicles['Power_KW'].notna()
# Update CO2 and other columns if not already filled
co2_mask = vehicles_mask & df_final['CO2_wltp'].isna()
df_final.loc[co2_mask, 'CO2_wltp'] = merged_vehicles.loc[co2_mask, 'CO2_wltp'].values
df_final.loc[co2_mask, 'Electric_range_km'] = merged_vehicles.loc[co2_mask, 'Electric range (km)'].values
df_final.loc[co2_mask, 'El_Consumpt_whkm'] = merged_vehicles.loc[co2_mask, 'El_Consumpt_whkm'].values
df_final.loc[co2_mask, 'Engine_cm3'] = merged_vehicles.loc[co2_mask, 'Engine_cm3'].values
print(f"    Matched {vehicles_mask.sum()} rows")

# Step 8: Fill vermogen from multiple sources
print("\n[Step 8] Filling vermogen from multiple sources...")

missing_before = df_final['vermogen'].isna().sum()
zero_before = (df_final['vermogen'] == 0).sum()

print(f"  Before filling:")
print(f"    Missing (NaN): {missing_before}")
print(f"    Zero values: {zero_before}")

# Fill from CarsDatasets 2025 (horsepower converted to KW)
mask_fill_from_hp = ((df_final['vermogen'].isna()) | (df_final['vermogen'] == 0)) & \
                     (merged_cars2025['Power_KW_from_HP'].notna()) & \
                     (merged_cars2025['Power_KW_from_HP'] > 0) & \
                     (merged_cars2025['Power_KW_from_HP'] <= 1000)
df_final.loc[mask_fill_from_hp, 'vermogen'] = merged_cars2025.loc[mask_fill_from_hp, 'Power_KW_from_HP'].values

# Fill from vehicles.csv (if still missing)
mask_fill_from_vehicles = ((df_final['vermogen'].isna()) | (df_final['vermogen'] == 0)) & \
                          (merged_vehicles['Power_KW'].notna()) & \
                          (merged_vehicles['Power_KW'] > 0) & \
                          (merged_vehicles['Power_KW'] <= 1000)
df_final.loc[mask_fill_from_vehicles, 'vermogen'] = merged_vehicles.loc[mask_fill_from_vehicles, 'Power_KW'].values

# Clean up unrealistic values
unrealistic_mask = df_final['vermogen'] > 1000
if unrealistic_mask.sum() > 0:
    print(f"    Warning: Found {unrealistic_mask.sum()} unrealistic vermogen values (>1000 KW), setting to NaN")
    df_final.loc[unrealistic_mask, 'vermogen'] = np.nan

missing_after = df_final['vermogen'].isna().sum()
zero_after = (df_final['vermogen'] == 0).sum()
filled_count = (missing_before + zero_before) - (missing_after + zero_after)

print(f"  After filling:")
print(f"    Missing (NaN): {missing_after}")
print(f"    Zero values: {zero_after}")
print(f"    [OK] Filled {filled_count} additional values")

# Step 9: Clean up and save
print("\n[Step 9] Cleaning up and saving...")

# Remove temporary columns
columns_to_drop = ['merk_normalized', 'model_normalized', 'Brand_normalized', 'Model_normalized', 
                   'Year', 'Price', 'Engine Size', 'Power_KW_from_HP', 'Torque_numeric', 
                   'Performance_numeric', 'Seats_numeric', 'Power_KW', 'Electric range (km)', 
                   'El_Consumpt_whkm']
df_final = df_final.drop(columns=[col for col in columns_to_drop if col in df_final.columns])

output_filename = os.path.join(data_dir, 'Cleaned_Car_Data_For_App_Fully_Enriched.csv')
df_final.to_csv(output_filename, index=False)

print(f"  [OK] Saved to {output_filename}")

# Step 10: Generate summary
print("\n" + "=" * 80)
print("FINAL MERGE SUMMARY REPORT")
print("=" * 80)

print(f"\nOriginal enriched dataset:")
print(f"  Total rows: {len(df_cleaned)}")
print(f"  Missing vermogen: {missing_before}")
print(f"  Zero vermogen: {zero_before}")

print(f"\nFully enriched dataset:")
print(f"  Total rows: {len(df_final)}")
print(f"  Missing vermogen: {missing_after}")
print(f"  Zero vermogen: {zero_after}")
print(f"  [OK] Successfully filled {filled_count} additional vermogen values")

print(f"\nNew columns added:")
print(f"  Transmission: {df_final['Transmission'].notna().sum()} non-null values")
print(f"  Mileage: {df_final['Mileage'].notna().sum()} non-null values")
print(f"  Condition: {df_final['Condition'].notna().sum()} non-null values")
print(f"  Torque_Nm: {df_final['Torque_Nm'].notna().sum()} non-null values")
print(f"  Performance_0_100_sec: {df_final['Performance_0_100_sec'].notna().sum()} non-null values")
print(f"  Seats: {df_final['Seats'].notna().sum()} non-null values")
print(f"  Engine_Size_L: {df_final['Engine_Size_L'].notna().sum()} non-null values")

print(f"\nExisting columns:")
print(f"  CO2_wltp: {df_final['CO2_wltp'].notna().sum()} non-null values")
print(f"  Electric_range_km: {df_final['Electric_range_km'].notna().sum()} non-null values")

print("\n" + "=" * 80)
print("[OK] FULL MERGE COMPLETE!")
print("=" * 80)

