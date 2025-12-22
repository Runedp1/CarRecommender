"""
Improved script to merge vehicles.csv with Cleaned_Car_Data_For_App.csv
and fill missing values (especially vermogen/power)
Uses multiple matching strategies for better results
"""

import pandas as pd
import numpy as np
import re

print("=" * 80)
print("IMPROVED DATABASE MERGE AND ENRICHMENT SCRIPT")
print("=" * 80)

# Step 1: Load both datasets
print("\n[Step 1] Loading datasets...")
df_cleaned = pd.read_csv('Cleaned_Car_Data_For_App.csv')
print(f"  [OK] Loaded Cleaned_Car_Data_For_App.csv: {len(df_cleaned)} rows")

print("  Loading vehicles.csv (this may take a moment)...")
df_vehicles = pd.read_csv('vehicles.csv')
print(f"  [OK] Loaded vehicles.csv: {len(df_vehicles)} rows")

# Step 2: Normalize brand names with improved mapping
print("\n[Step 2] Creating brand name normalization mapping...")

# Expanded brand mapping
brand_mapping = {
    'HYUNDAI': 'hyundai',
    'DAIMLER AG': 'mercedes-benz',
    'MERCEDES-BENZ': 'mercedes-benz',
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
}

# Normalize brand names in vehicles.csv
df_vehicles['Brand_normalized'] = df_vehicles['Brand'].str.upper().str.strip()
df_vehicles['Brand_normalized'] = df_vehicles['Brand_normalized'].map(brand_mapping).fillna(
    df_vehicles['Brand'].str.lower().str.strip()
)

# Normalize brand names in cleaned data
df_cleaned['merk_normalized'] = df_cleaned['merk'].str.lower().str.strip()

print(f"  [OK] Normalized brands")
print(f"    Vehicles unique brands: {df_vehicles['Brand_normalized'].nunique()}")
print(f"    Cleaned unique brands: {df_cleaned['merk_normalized'].nunique()}")

# Step 3: Normalize model names with improved logic
print("\n[Step 3] Normalizing model names...")

def normalize_model_name(name):
    """Normalize model name for matching - handles variations"""
    if pd.isna(name):
        return ""
    # Convert to lowercase, remove extra spaces
    name = str(name).lower().strip()
    # Remove special characters but keep spaces and hyphens
    name = re.sub(r'[^\w\s-]', '', name)
    # Normalize multiple spaces to single space
    name = re.sub(r'\s+', ' ', name)
    # Remove common prefixes/suffixes that might differ
    name = name.replace('eq ', '').replace(' eq', '')  # Electric variants
    name = name.replace('e-', '').replace('e ', '')  # E-prefixes
    return name

def extract_base_model(model_name):
    """Extract base model name (e.g., 'KONA KAUAI' -> 'kona')"""
    if not model_name:
        return ""
    # Split and take first significant word (skip common prefixes)
    words = model_name.split()
    # Skip common prefixes
    skip_words = {'eq', 'e', 'i', 'hybrid', 'electric', 'ev'}
    for word in words:
        if word not in skip_words and len(word) > 2:
            return word
    return words[0] if words else ""

df_vehicles['Model_normalized'] = df_vehicles['Veh_Model'].apply(normalize_model_name)
df_vehicles['Model_base'] = df_vehicles['Model_normalized'].apply(extract_base_model)
df_cleaned['model_normalized'] = df_cleaned['model'].apply(normalize_model_name)
df_cleaned['model_base'] = df_cleaned['model_normalized'].apply(extract_base_model)

print(f"  [OK] Normalized model names")

# Step 4: Prepare vehicles data for merging
print("\n[Step 4] Preparing vehicles data for merging...")

# Filter vehicles with valid Power_KW
df_vehicles_valid = df_vehicles[df_vehicles['Power_KW'].notna() & (df_vehicles['Power_KW'] > 0)].copy()

# Create multiple lookup strategies
# Strategy 1: Exact match (brand + model + year)
vehicles_exact = df_vehicles_valid.groupby(['Brand_normalized', 'Model_normalized', 'year']).agg({
    'Power_KW': lambda x: x.median() if x.notna().sum() > 0 else np.nan,
    'CO2_wltp': lambda x: x.median() if x.notna().sum() > 0 else np.nan,
    'Electric range (km)': lambda x: x.median() if x.notna().sum() > 0 else np.nan,
    'El_Consumpt_whkm': lambda x: x.median() if x.notna().sum() > 0 else np.nan,
    'Engine_cm3': lambda x: x.median() if x.notna().sum() > 0 else np.nan,
}).reset_index()
vehicles_exact['match_type'] = 'exact'

# Strategy 2: Brand + base model + year (more flexible)
vehicles_base = df_vehicles_valid.groupby(['Brand_normalized', 'Model_base', 'year']).agg({
    'Power_KW': lambda x: x.median() if x.notna().sum() > 0 else np.nan,
    'CO2_wltp': lambda x: x.median() if x.notna().sum() > 0 else np.nan,
    'Electric range (km)': lambda x: x.median() if x.notna().sum() > 0 else np.nan,
    'El_Consumpt_whkm': lambda x: x.median() if x.notna().sum() > 0 else np.nan,
    'Engine_cm3': lambda x: x.median() if x.notna().sum() > 0 else np.nan,
}).reset_index()
vehicles_base.columns = ['Brand_normalized', 'Model_normalized', 'year', 'Power_KW', 'CO2_wltp', 
                         'Electric range (km)', 'El_Consumpt_whkm', 'Engine_cm3']
vehicles_base['match_type'] = 'base_model'

# Strategy 3: Brand + model without year (for cases where year doesn't match exactly)
vehicles_no_year = df_vehicles_valid.groupby(['Brand_normalized', 'Model_normalized']).agg({
    'Power_KW': lambda x: x.median() if x.notna().sum() > 0 else np.nan,
    'CO2_wltp': lambda x: x.median() if x.notna().sum() > 0 else np.nan,
    'Electric range (km)': lambda x: x.median() if x.notna().sum() > 0 else np.nan,
    'El_Consumpt_whkm': lambda x: x.median() if x.notna().sum() > 0 else np.nan,
    'Engine_cm3': lambda x: x.median() if x.notna().sum() > 0 else np.nan,
}).reset_index()
vehicles_no_year['year'] = np.nan
vehicles_no_year['match_type'] = 'no_year'

print(f"  [OK] Created lookup tables:")
print(f"    Exact matches: {len(vehicles_exact)} combinations")
print(f"    Base model matches: {len(vehicles_base)} combinations")
print(f"    No-year matches: {len(vehicles_no_year)} combinations")

# Step 5: Merge datasets using multiple strategies
print("\n[Step 5] Merging datasets (trying multiple strategies)...")

df_enriched = df_cleaned.copy()

# Initialize new columns
df_enriched['Power_KW_merged'] = np.nan
df_enriched['CO2_wltp_merged'] = np.nan
df_enriched['Electric_range_km_merged'] = np.nan
df_enriched['El_Consumpt_whkm_merged'] = np.nan
df_enriched['Engine_cm3_merged'] = np.nan
df_enriched['match_strategy'] = ''

# Strategy 1: Try exact match (brand + model + year)
print("  Trying Strategy 1: Exact match (brand + model + year)...")
merged_exact = df_enriched.merge(
    vehicles_exact,
    left_on=['merk_normalized', 'model_normalized', 'bouwjaar'],
    right_on=['Brand_normalized', 'Model_normalized', 'year'],
    how='left',
    suffixes=('', '_exact')
)

# Update where we found exact matches
exact_mask = merged_exact['match_type'] == 'exact'
df_enriched.loc[exact_mask, 'Power_KW_merged'] = merged_exact.loc[exact_mask, 'Power_KW']
df_enriched.loc[exact_mask, 'CO2_wltp_merged'] = merged_exact.loc[exact_mask, 'CO2_wltp']
df_enriched.loc[exact_mask, 'Electric_range_km_merged'] = merged_exact.loc[exact_mask, 'Electric range (km)']
df_enriched.loc[exact_mask, 'El_Consumpt_whkm_merged'] = merged_exact.loc[exact_mask, 'El_Consumpt_whkm']
df_enriched.loc[exact_mask, 'Engine_cm3_merged'] = merged_exact.loc[exact_mask, 'Engine_cm3']
df_enriched.loc[exact_mask, 'match_strategy'] = 'exact'
exact_count = exact_mask.sum()
print(f"    Found {exact_count} exact matches")

# Strategy 2: Try base model match (brand + base_model + year) for unmatched rows
print("  Trying Strategy 2: Base model match (brand + base_model + year)...")
unmatched_mask = df_enriched['match_strategy'] == ''
if unmatched_mask.sum() > 0:
    merged_base = df_enriched[unmatched_mask].merge(
        vehicles_base,
        left_on=['merk_normalized', 'model_base', 'bouwjaar'],
        right_on=['Brand_normalized', 'Model_normalized', 'year'],
        how='left',
        suffixes=('', '_base')
    )
    
    base_mask = (merged_base['match_type'] == 'base_model') & (merged_base['Power_KW'].notna())
    base_indices = df_enriched[unmatched_mask].index[base_mask]
    
    df_enriched.loc[base_indices, 'Power_KW_merged'] = merged_base.loc[base_mask, 'Power_KW'].values
    df_enriched.loc[base_indices, 'CO2_wltp_merged'] = merged_base.loc[base_mask, 'CO2_wltp'].values
    df_enriched.loc[base_indices, 'Electric_range_km_merged'] = merged_base.loc[base_mask, 'Electric range (km)'].values
    df_enriched.loc[base_indices, 'El_Consumpt_whkm_merged'] = merged_base.loc[base_mask, 'El_Consumpt_whkm'].values
    df_enriched.loc[base_indices, 'Engine_cm3_merged'] = merged_base.loc[base_mask, 'Engine_cm3'].values
    df_enriched.loc[base_indices, 'match_strategy'] = 'base_model'
    print(f"    Found {len(base_indices)} base model matches")

# Strategy 3: Try brand + model without year for still unmatched rows
print("  Trying Strategy 3: Brand + model (no year)...")
unmatched_mask = df_enriched['match_strategy'] == ''
if unmatched_mask.sum() > 0:
    merged_no_year = df_enriched[unmatched_mask].merge(
        vehicles_no_year,
        left_on=['merk_normalized', 'model_normalized'],
        right_on=['Brand_normalized', 'Model_normalized'],
        how='left',
        suffixes=('', '_no_year')
    )
    
    no_year_mask = (merged_no_year['match_type'] == 'no_year') & (merged_no_year['Power_KW'].notna())
    no_year_indices = df_enriched[unmatched_mask].index[no_year_mask]
    
    df_enriched.loc[no_year_indices, 'Power_KW_merged'] = merged_no_year.loc[no_year_mask, 'Power_KW'].values
    df_enriched.loc[no_year_indices, 'CO2_wltp_merged'] = merged_no_year.loc[no_year_mask, 'CO2_wltp'].values
    df_enriched.loc[no_year_indices, 'Electric_range_km_merged'] = merged_no_year.loc[no_year_mask, 'Electric range (km)'].values
    df_enriched.loc[no_year_indices, 'El_Consumpt_whkm_merged'] = merged_no_year.loc[no_year_mask, 'El_Consumpt_whkm'].values
    df_enriched.loc[no_year_indices, 'Engine_cm3_merged'] = merged_no_year.loc[no_year_mask, 'Engine_cm3'].values
    df_enriched.loc[no_year_indices, 'match_strategy'] = 'no_year'
    print(f"    Found {len(no_year_indices)} no-year matches")

total_matched = (df_enriched['match_strategy'] != '').sum()
print(f"  [OK] Total matched: {total_matched} rows ({total_matched/len(df_enriched)*100:.1f}%)")

# Step 6: Fill missing vermogen values
print("\n[Step 6] Filling missing vermogen values...")

missing_before = df_enriched['vermogen'].isna().sum()
zero_before = (df_enriched['vermogen'] == 0).sum()
total_missing_or_zero = missing_before + zero_before

print(f"  Before filling:")
print(f"    Missing (NaN): {missing_before}")
print(f"    Zero values: {zero_before}")
print(f"    Total to fill: {total_missing_or_zero}")

# Fill NaN values with Power_KW_merged
mask_nan = df_enriched['vermogen'].isna() & df_enriched['Power_KW_merged'].notna()
df_enriched.loc[mask_nan, 'vermogen'] = df_enriched.loc[mask_nan, 'Power_KW_merged']

# Fill zero values with Power_KW_merged (if available and > 0)
mask_zero = (df_enriched['vermogen'] == 0) & (df_enriched['Power_KW_merged'].notna()) & (df_enriched['Power_KW_merged'] > 0)
df_enriched.loc[mask_zero, 'vermogen'] = df_enriched.loc[mask_zero, 'Power_KW_merged']

# Clean up unrealistic values (filter out values > 1000 KW which is unrealistic for cars)
# Most cars are between 50-500 KW, so anything > 1000 is likely an error
unrealistic_mask = df_enriched['vermogen'] > 1000
if unrealistic_mask.sum() > 0:
    print(f"    Warning: Found {unrealistic_mask.sum()} unrealistic vermogen values (>1000 KW), setting to NaN")
    df_enriched.loc[unrealistic_mask, 'vermogen'] = np.nan

missing_after = df_enriched['vermogen'].isna().sum()
zero_after = (df_enriched['vermogen'] == 0).sum()
filled_count = total_missing_or_zero - (missing_after + zero_after)

print(f"  After filling:")
print(f"    Missing (NaN): {missing_after}")
print(f"    Zero values: {zero_after}")
print(f"    [OK] Filled {filled_count} values")

# Step 7: Add new columns from vehicles.csv
print("\n[Step 7] Adding new columns from vehicles.csv...")

df_enriched['CO2_wltp'] = df_enriched['CO2_wltp_merged']
df_enriched['Electric_range_km'] = df_enriched['Electric_range_km_merged']
df_enriched['El_Consumpt_whkm'] = df_enriched['El_Consumpt_whkm_merged']
df_enriched['Engine_cm3'] = df_enriched['Engine_cm3_merged']

# Clean up temporary columns
columns_to_drop = ['Brand_normalized', 'Model_normalized', 'year', 
                   'Power_KW_merged', 'CO2_wltp_merged', 'Electric_range_km_merged',
                   'El_Consumpt_whkm_merged', 'Engine_cm3_merged', 
                   'merk_normalized', 'model_normalized', 'model_base', 'match_strategy']
df_enriched = df_enriched.drop(columns=[col for col in columns_to_drop if col in df_enriched.columns])

print(f"  [OK] Added columns: CO2_wltp, Electric_range_km, El_Consumpt_whkm, Engine_cm3")

# Step 8: Save enriched dataset
print("\n[Step 8] Saving enriched dataset...")

output_filename = 'Cleaned_Car_Data_For_App_Enriched.csv'
df_enriched.to_csv(output_filename, index=False)

print(f"  [OK] Saved to {output_filename}")

# Step 9: Generate summary report
print("\n" + "=" * 80)
print("MERGE SUMMARY REPORT")
print("=" * 80)

print(f"\nOriginal dataset:")
print(f"  Total rows: {len(df_cleaned)}")
print(f"  Missing vermogen: {missing_before}")
print(f"  Zero vermogen: {zero_before}")

print(f"\nEnriched dataset:")
print(f"  Total rows: {len(df_enriched)}")
print(f"  Missing vermogen: {missing_after}")
print(f"  Zero vermogen: {zero_after}")
print(f"  [OK] Successfully filled {filled_count} vermogen values")

print(f"\nNew columns added:")
print(f"  CO2_wltp: {df_enriched['CO2_wltp'].notna().sum()} non-null values")
print(f"  Electric_range_km: {df_enriched['Electric_range_km'].notna().sum()} non-null values")
print(f"  El_Consumpt_whkm: {df_enriched['El_Consumpt_whkm'].notna().sum()} non-null values")
print(f"  Engine_cm3: {df_enriched['Engine_cm3'].notna().sum()} non-null values")

print(f"\nMatching statistics:")
matched_rows = df_enriched['CO2_wltp'].notna().sum()
match_percentage = (matched_rows / len(df_enriched)) * 100
print(f"  Rows matched with vehicles.csv: {matched_rows} ({match_percentage:.1f}%)")

print("\n" + "=" * 80)
print("[OK] MERGE COMPLETE!")
print("=" * 80)

