"""
Test script om te verifiëren dat de C# filters correct werken.
Simuleert wat er zou moeten gebeuren na het inladen.
"""

import pandas as pd
import os

script_dir = os.path.dirname(os.path.abspath(__file__))
project_root = os.path.dirname(script_dir)
data_dir = os.path.join(project_root, 'data')

print("=" * 80)
print("TEST: C# FILTERS - VERIFICATIE")
print("=" * 80)

csv_path = os.path.join(data_dir, 'Cleaned_Car_Data_For_App_Fully_Enriched.csv')
df = pd.read_csv(csv_path)

# C# filter logica (gekopieerd uit IsCarRealistic)
MIN_REALISTIC_PRICE = 500
MAX_REALISTIC_PRICE = 500000
MIN_REALISTIC_POWER = 30
MAX_REALISTIC_POWER = 800
MIN_REALISTIC_YEAR = 1990
MAX_REALISTIC_YEAR = 2025

print(f"\nTotaal auto's in CSV: {len(df):,}")

# Parse budget/prijs (net zoals C# doet)
df['prijs_clean'] = pd.to_numeric(df['prijs'], errors='coerce')

# Parse vermogen (net zoals C# doet - haal alleen cijfers eruit)
df['vermogen_clean'] = df['vermogen'].astype(str).str.replace(r'[^\d]', '', regex=True)
df['vermogen_clean'] = pd.to_numeric(df['vermogen_clean'], errors='coerce')

# Parse bouwjaar
df['bouwjaar_clean'] = pd.to_numeric(df['bouwjaar'], errors='coerce')

# Apply filters (zoals IsCarRealistic in C#)
realistic_mask = (
    (df['prijs_clean'] >= MIN_REALISTIC_PRICE) & (df['prijs_clean'] <= MAX_REALISTIC_PRICE) &
    (df['vermogen_clean'] >= MIN_REALISTIC_POWER) & (df['vermogen_clean'] <= MAX_REALISTIC_POWER) &
    (df['bouwjaar_clean'] >= MIN_REALISTIC_YEAR) & (df['bouwjaar_clean'] <= MAX_REALISTIC_YEAR) &
    (df['merk'].notna()) & (df['merk'].astype(str).str.strip() != '') &
    (df['model'].notna()) & (df['model'].astype(str).str.strip() != '')
)

realistic_cars = df[realistic_mask].copy()

print(f"\nNa C# filters (IsCarRealistic): {len(realistic_cars):,} auto's")
print(f"Gefilterd: {len(df) - len(realistic_cars):,} auto's ({(len(df) - len(realistic_cars))/len(df)*100:.2f}%)")

# Check of er nog onrealistische waarden zijn
print(f"\n{'='*60}")
print("VERIFICATIE: Zijn er nog onrealistische waarden?")
print(f"{'='*60}")

price_violations = realistic_cars[
    (realistic_cars['prijs_clean'] < MIN_REALISTIC_PRICE) | 
    (realistic_cars['prijs_clean'] > MAX_REALISTIC_PRICE)
]
power_violations = realistic_cars[
    (realistic_cars['vermogen_clean'] < MIN_REALISTIC_POWER) | 
    (realistic_cars['vermogen_clean'] > MAX_REALISTIC_POWER)
]
year_violations = realistic_cars[
    (realistic_cars['bouwjaar_clean'] < MIN_REALISTIC_YEAR) | 
    (realistic_cars['bouwjaar_clean'] > MAX_REALISTIC_YEAR)
]

print(f"\nPRIJS schendingen: {len(price_violations):,}")
if len(price_violations) > 0:
    print("  Voorbeelden:")
    print(price_violations[['merk', 'model', 'bouwjaar', 'prijs']].head(10))

print(f"\nVERMOGEN schendingen: {len(power_violations):,}")
if len(power_violations) > 0:
    print("  Voorbeelden:")
    print(power_violations[['merk', 'model', 'bouwjaar', 'vermogen']].head(10))

print(f"\nBOUWJAAR schendingen: {len(year_violations):,}")
if len(year_violations) > 0:
    print("  Voorbeelden:")
    print(year_violations[['merk', 'model', 'bouwjaar', 'prijs']].head(10))

# Statistieken van gefilterde data
if len(realistic_cars) > 0:
    print(f"\n{'='*60}")
    print("STATISTIEKEN GEFILTERDE DATA")
    print(f"{'='*60}")
    print(f"\nPRIJS:")
    print(f"  Min: €{realistic_cars['prijs_clean'].min():,.0f}")
    print(f"  Max: €{realistic_cars['prijs_clean'].max():,.0f}")
    print(f"  Gemiddelde: €{realistic_cars['prijs_clean'].mean():,.0f}")
    print(f"  Mediaan: €{realistic_cars['prijs_clean'].median():,.0f}")
    
    print(f"\nVERMOGEN:")
    print(f"  Min: {realistic_cars['vermogen_clean'].min():,.0f} KW")
    print(f"  Max: {realistic_cars['vermogen_clean'].max():,.0f} KW")
    print(f"  Gemiddelde: {realistic_cars['vermogen_clean'].mean():,.1f} KW")
    print(f"  Mediaan: {realistic_cars['vermogen_clean'].median():,.0f} KW")
    
    print(f"\nBOUWJAAR:")
    print(f"  Min: {int(realistic_cars['bouwjaar_clean'].min())}")
    print(f"  Max: {int(realistic_cars['bouwjaar_clean'].max())}")
    print(f"  Gemiddelde: {realistic_cars['bouwjaar_clean'].mean():.1f}")
    print(f"  Mediaan: {int(realistic_cars['bouwjaar_clean'].median())}")

print("\n" + "=" * 80)
if len(price_violations) == 0 and len(power_violations) == 0 and len(year_violations) == 0:
    print("OK: FILTERS WERKEN CORRECT - Geen onrealistische waarden meer!")
else:
    print("WAARSCHUWING: Er zijn nog steeds schendingen!")
print("=" * 80)

