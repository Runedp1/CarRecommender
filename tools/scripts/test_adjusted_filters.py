"""
Test aangepaste filters met soepelere grenzen
"""

import pandas as pd
import os

script_dir = os.path.dirname(os.path.abspath(__file__))
project_root = os.path.dirname(script_dir)
data_dir = os.path.join(project_root, 'data')

print("=" * 80)
print("TEST: AANGEPASTE FILTERS (SOEPELER)")
print("=" * 80)

csv_path = os.path.join(data_dir, 'Cleaned_Car_Data_For_App_Fully_Enriched.csv')
df = pd.read_csv(csv_path)

# AANGEPASTE grenzen
MIN_REALISTIC_PRICE = 300  # Was 500
MAX_REALISTIC_PRICE = 500000
MIN_REALISTIC_POWER = 20   # Was 30
MAX_REALISTIC_POWER = 800
MIN_REALISTIC_YEAR = 1990
MAX_REALISTIC_YEAR = 2025

print(f"\nAANGEPASTE GRENZEN:")
print(f"  Prijs: €{MIN_REALISTIC_PRICE} - €{MAX_REALISTIC_PRICE} (was €500)")
print(f"  Vermogen: {MIN_REALISTIC_POWER} - {MAX_REALISTIC_POWER} KW (was 30 KW)")
print(f"  Bouwjaar: {MIN_REALISTIC_YEAR} - {MAX_REALISTIC_YEAR}")

df['prijs_clean'] = pd.to_numeric(df['prijs'], errors='coerce')
df['vermogen_clean'] = df['vermogen'].astype(str).str.replace(r'[^\d]', '', regex=True)
df['vermogen_clean'] = pd.to_numeric(df['vermogen_clean'], errors='coerce')
df['bouwjaar_clean'] = pd.to_numeric(df['bouwjaar'], errors='coerce')

realistic_mask = (
    (df['prijs_clean'] >= MIN_REALISTIC_PRICE) & (df['prijs_clean'] <= MAX_REALISTIC_PRICE) &
    (df['vermogen_clean'] >= MIN_REALISTIC_POWER) & (df['vermogen_clean'] <= MAX_REALISTIC_POWER) &
    (df['bouwjaar_clean'] >= MIN_REALISTIC_YEAR) & (df['bouwjaar_clean'] <= MAX_REALISTIC_YEAR) &
    (df['merk'].notna()) & (df['merk'].astype(str).str.strip() != '') &
    (df['model'].notna()) & (df['model'].astype(str).str.strip() != '')
)

realistic_cars = df[realistic_mask].copy()

print(f"\nTotaal auto's in CSV: {len(df):,}")
print(f"Na aangepaste filters: {len(realistic_cars):,} auto's")
print(f"Gefilterd: {len(df) - len(realistic_cars):,} auto's ({(len(df) - len(realistic_cars))/len(df)*100:.2f}%)")
print(f"Verbetering: +{len(realistic_cars) - 2173:,} auto's t.o.v. oude filters")

# Verificatie
price_violations = realistic_cars[
    (realistic_cars['prijs_clean'] < MIN_REALISTIC_PRICE) | 
    (realistic_cars['prijs_clean'] > MAX_REALISTIC_PRICE)
]
power_violations = realistic_cars[
    (realistic_cars['vermogen_clean'] < MIN_REALISTIC_POWER) | 
    (realistic_cars['vermogen_clean'] > MAX_REALISTIC_POWER)
]

print(f"\nVERIFICATIE:")
print(f"  Prijs schendingen: {len(price_violations):,}")
print(f"  Vermogen schendingen: {len(power_violations):,}")

if len(realistic_cars) > 0:
    print(f"\nSTATISTIEKEN:")
    print(f"  Prijs: €{realistic_cars['prijs_clean'].min():,.0f} - €{realistic_cars['prijs_clean'].max():,.0f} (mediaan: €{realistic_cars['prijs_clean'].median():,.0f})")
    print(f"  Vermogen: {realistic_cars['vermogen_clean'].min():,.0f} - {realistic_cars['vermogen_clean'].max():,.0f} KW (mediaan: {realistic_cars['vermogen_clean'].median():,.0f} KW)")

print("\n" + "=" * 80)
if len(price_violations) == 0 and len(power_violations) == 0:
    print("OK: Filters werken correct!")
print("=" * 80)

