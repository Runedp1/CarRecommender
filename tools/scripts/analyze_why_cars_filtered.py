"""
Analyseer WAAROM auto's worden gefilterd - zijn onze filters te streng?
"""

import pandas as pd
import os

script_dir = os.path.dirname(os.path.abspath(__file__))
project_root = os.path.dirname(script_dir)
data_dir = os.path.join(project_root, 'data')

print("=" * 80)
print("ANALYSE: WAAROM WORDEN AUTO'S GEFILTERD?")
print("=" * 80)

csv_path = os.path.join(data_dir, 'Cleaned_Car_Data_For_App_Fully_Enriched.csv')
df = pd.read_csv(csv_path)

MIN_REALISTIC_PRICE = 500
MAX_REALISTIC_PRICE = 500000
MIN_REALISTIC_POWER = 30
MAX_REALISTIC_POWER = 800
MIN_REALISTIC_YEAR = 1990
MAX_REALISTIC_YEAR = 2025

print(f"\nTotaal auto's in CSV: {len(df):,}")

# Parse zoals C# doet
df['prijs_clean'] = pd.to_numeric(df['prijs'], errors='coerce')
df['vermogen_clean'] = df['vermogen'].astype(str).str.replace(r'[^\d]', '', regex=True)
df['vermogen_clean'] = pd.to_numeric(df['vermogen_clean'], errors='coerce')
df['bouwjaar_clean'] = pd.to_numeric(df['bouwjaar'], errors='coerce')

# Check elke filter reden apart
print(f"\n{'='*60}")
print("DETAILANALYSE PER FILTER")
print(f"{'='*60}")

# 1. Missing merk/model
missing_brand = df[df['merk'].isna() | (df['merk'].astype(str).str.strip() == '')]
missing_model = df[df['model'].isna() | (df['model'].astype(str).str.strip() == '')]
missing_both = df[
    (df['merk'].isna() | (df['merk'].astype(str).str.strip() == '')) &
    (df['model'].isna() | (df['model'].astype(str).str.strip() == ''))
]

print(f"\n1. MISSING MERK/MODEL:")
print(f"   Missing merk: {len(missing_brand):,}")
print(f"   Missing model: {len(missing_model):,}")
print(f"   Missing beide: {len(missing_both):,}")

# 2. Prijs problemen
price_too_low = df[df['prijs_clean'] < MIN_REALISTIC_PRICE]
price_too_high = df[df['prijs_clean'] > MAX_REALISTIC_PRICE]
price_missing = df[df['prijs_clean'].isna()]

print(f"\n2. PRIJS PROBLEMEN:")
print(f"   Te goedkoop (< €{MIN_REALISTIC_PRICE}): {len(price_too_low):,}")
if len(price_too_low) > 0:
    print(f"     Voorbeelden: €{price_too_low['prijs_clean'].min():.2f} - €{price_too_low['prijs_clean'].max():.2f}")
print(f"   Te duur (> €{MAX_REALISTIC_PRICE}): {len(price_too_high):,}")
if len(price_too_high) > 0:
    print(f"     Voorbeelden: €{price_too_high['prijs_clean'].min():,.0f} - €{price_too_high['prijs_clean'].max():,.0f}")
print(f"   Missing/NaN: {len(price_missing):,}")

# 3. Vermogen problemen
power_too_low = df[(df['vermogen_clean'] < MIN_REALISTIC_POWER) & (df['vermogen_clean'].notna())]
power_zero = df[df['vermogen_clean'] == 0]
power_too_high = df[df['vermogen_clean'] > MAX_REALISTIC_POWER]
power_missing = df[df['vermogen_clean'].isna()]

print(f"\n3. VERMOGEN PROBLEMEN:")
print(f"   Te laag (< {MIN_REALISTIC_POWER} KW): {len(power_too_low):,}")
print(f"   Precies 0 KW: {len(power_zero):,}")
print(f"   Te hoog (> {MAX_REALISTIC_POWER} KW): {len(power_too_high):,}")
if len(power_too_high) > 0:
    print(f"     Voorbeelden: {power_too_high['vermogen_clean'].min():,.0f} - {power_too_high['vermogen_clean'].max():,.0f} KW")
print(f"   Missing/NaN: {len(power_missing):,}")

# 4. Bouwjaar problemen
year_too_old = df[(df['bouwjaar_clean'] < MIN_REALISTIC_YEAR) & (df['bouwjaar_clean'].notna())]
year_too_new = df[df['bouwjaar_clean'] > MAX_REALISTIC_YEAR]
year_missing = df[df['bouwjaar_clean'].isna()]

print(f"\n4. BOUWJAAR PROBLEMEN:")
print(f"   Te oud (< {MIN_REALISTIC_YEAR}): {len(year_too_old):,}")
if len(year_too_old) > 0:
    print(f"     Voorbeelden: {int(year_too_old['bouwjaar_clean'].min())} - {int(year_too_old['bouwjaar_clean'].max())}")
print(f"   Te nieuw (> {MAX_REALISTIC_YEAR}): {len(year_too_new):,}")
print(f"   Missing/NaN: {len(year_missing):,}")

# Combinaties
print(f"\n{'='*60}")
print("COMBINATIES VAN PROBLEMEN")
print(f"{'='*60}")

# Auto's die door meerdere filters worden uitgesloten
all_issues = df[
    (df['prijs_clean'].isna() | (df['prijs_clean'] < MIN_REALISTIC_PRICE) | (df['prijs_clean'] > MAX_REALISTIC_PRICE)) |
    (df['vermogen_clean'].isna() | (df['vermogen_clean'] < MIN_REALISTIC_POWER) | (df['vermogen_clean'] > MAX_REALISTIC_POWER)) |
    (df['bouwjaar_clean'].isna() | (df['bouwjaar_clean'] < MIN_REALISTIC_YEAR) | (df['bouwjaar_clean'] > MAX_REALISTIC_YEAR)) |
    (df['merk'].isna() | (df['merk'].astype(str).str.strip() == '')) |
    (df['model'].isna() | (df['model'].astype(str).str.strip() == ''))
]

# Tel hoeveel problemen per auto
def count_issues(row):
    issues = 0
    if pd.isna(row['prijs_clean']) or row['prijs_clean'] < MIN_REALISTIC_PRICE or row['prijs_clean'] > MAX_REALISTIC_PRICE:
        issues += 1
    if pd.isna(row['vermogen_clean']) or row['vermogen_clean'] < MIN_REALISTIC_POWER or row['vermogen_clean'] > MAX_REALISTIC_POWER:
        issues += 1
    if pd.isna(row['bouwjaar_clean']) or row['bouwjaar_clean'] < MIN_REALISTIC_YEAR or row['bouwjaar_clean'] > MAX_REALISTIC_YEAR:
        issues += 1
    if pd.isna(row['merk']) or str(row['merk']).strip() == '':
        issues += 1
    if pd.isna(row['model']) or str(row['model']).strip() == '':
        issues += 1
    return issues

all_issues['num_issues'] = all_issues.apply(count_issues, axis=1)

print(f"\nAuto's met problemen:")
for i in range(1, 6):
    count = len(all_issues[all_issues['num_issues'] == i])
    if count > 0:
        print(f"  {i} probleem(en): {count:,} auto's")

# Realistische auto's
realistic_mask = (
    (df['prijs_clean'] >= MIN_REALISTIC_PRICE) & (df['prijs_clean'] <= MAX_REALISTIC_PRICE) &
    (df['vermogen_clean'] >= MIN_REALISTIC_POWER) & (df['vermogen_clean'] <= MAX_REALISTIC_POWER) &
    (df['bouwjaar_clean'] >= MIN_REALISTIC_YEAR) & (df['bouwjaar_clean'] <= MAX_REALISTIC_YEAR) &
    (df['merk'].notna()) & (df['merk'].astype(str).str.strip() != '') &
    (df['model'].notna()) & (df['model'].astype(str).str.strip() != '')
)

realistic_cars = df[realistic_mask]

print(f"\n{'='*60}")
print("SAMENVATTING")
print(f"{'='*60}")
print(f"Totaal auto's: {len(df):,}")
print(f"Realistische auto's: {len(realistic_cars):,} ({len(realistic_cars)/len(df)*100:.2f}%)")
print(f"Gefilterde auto's: {len(df) - len(realistic_cars):,} ({(len(df) - len(realistic_cars))/len(df)*100:.2f}%)")

# Top redenen
print(f"\n{'='*60}")
print("TOP REDENEN VOOR FILTERING")
print(f"{'='*60}")

reasons = {}
if len(power_zero) > 0:
    reasons['Vermogen = 0'] = len(power_zero)
if len(power_too_low) > 0:
    reasons[f'Vermogen < {MIN_REALISTIC_POWER} KW'] = len(power_too_low)
if len(price_too_low) > 0:
    reasons[f'Prijs < €{MIN_REALISTIC_PRICE}'] = len(price_too_low)
if len(missing_model) > 0:
    reasons['Missing model'] = len(missing_model)
if len(missing_brand) > 0:
    reasons['Missing merk'] = len(missing_brand)
if len(price_missing) > 0:
    reasons['Missing prijs'] = len(price_missing)
if len(power_missing) > 0:
    reasons['Missing vermogen'] = len(power_missing)

sorted_reasons = sorted(reasons.items(), key=lambda x: x[1], reverse=True)
for reason, count in sorted_reasons[:10]:
    print(f"  {reason}: {count:,} auto's ({count/len(df)*100:.1f}%)")

print("\n" + "=" * 80)

