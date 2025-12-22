"""
Analyseer de kwaliteit van recommendations - zoek naar onrealistische waarden
"""

import pandas as pd
import os

script_dir = os.path.dirname(os.path.abspath(__file__))
project_root = os.path.dirname(script_dir)
data_dir = os.path.join(project_root, 'data')

print("=" * 80)
print("ANALYSE RECOMMENDATION KWALITEIT - ONREALISTISCHE WAARDEN")
print("=" * 80)

csv_path = os.path.join(data_dir, 'Cleaned_Car_Data_For_App_Fully_Enriched.csv')
df = pd.read_csv(csv_path)

print(f"\nTotaal auto's in dataset: {len(df):,}")

# Realistische grenzen
MIN_REALISTIC_PRICE = 500
MAX_REALISTIC_PRICE = 500000
MIN_REALISTIC_POWER = 30  # KW
MAX_REALISTIC_POWER = 800  # KW - zeer hoge maar nog realistische sportwagens
MIN_REALISTIC_YEAR = 1990
MAX_REALISTIC_YEAR = 2025

print(f"\n{'='*60}")
print("PROBLEMATISCHE AUTO'S")
print(f"{'='*60}")

# Prijs problemen
too_cheap = df[df['prijs'] < MIN_REALISTIC_PRICE]
too_expensive = df[df['prijs'] > MAX_REALISTIC_PRICE]
price_issues = pd.concat([too_cheap, too_expensive]).drop_duplicates()

print(f"\nPRIJS PROBLEMEN:")
print(f"  Te goedkoop (< €{MIN_REALISTIC_PRICE}): {len(too_cheap):,} auto's")
print(f"  Te duur (> €{MAX_REALISTIC_PRICE}): {len(too_expensive):,} auto's")
print(f"  Totaal prijs problemen: {len(price_issues):,} ({len(price_issues)/len(df)*100:.2f}%)")

if len(price_issues) > 0:
    print("\nVoorbeelden prijs problemen:")
    print(price_issues[['merk', 'model', 'bouwjaar', 'prijs']].head(20).to_string(index=False))

# Vermogen problemen
too_low_power = df[df['vermogen'] < MIN_REALISTIC_POWER]
too_high_power = df[df['vermogen'] > MAX_REALISTIC_POWER]
power_issues = pd.concat([too_low_power, too_high_power]).drop_duplicates()

print(f"\nVERMOGEN PROBLEMEN:")
print(f"  Te laag vermogen (< {MIN_REALISTIC_POWER} KW): {len(too_low_power):,} auto's")
print(f"  Te hoog vermogen (> {MAX_REALISTIC_POWER} KW): {len(too_high_power):,} auto's")
print(f"  Totaal vermogen problemen: {len(power_issues):,} ({len(power_issues)/len(df)*100:.2f}%)")

if len(too_high_power) > 0:
    print("\nVoorbeelden zeer hoog vermogen:")
    high_power_sorted = too_high_power.sort_values('vermogen', ascending=False)
    print(high_power_sorted[['merk', 'model', 'bouwjaar', 'vermogen', 'prijs']].head(20).to_string(index=False))

if len(too_low_power) > 0:
    print("\nVoorbeelden zeer laag vermogen:")
    low_power_sorted = too_low_power.sort_values('vermogen', ascending=True)
    print(low_power_sorted[['merk', 'model', 'bouwjaar', 'vermogen', 'prijs']].head(20).to_string(index=False))

# Bouwjaar problemen
old_year = df[df['bouwjaar'] < MIN_REALISTIC_YEAR]
future_year = df[df['bouwjaar'] > MAX_REALISTIC_YEAR]
year_issues = pd.concat([old_year, future_year]).drop_duplicates()

print(f"\nBOUWJAAR PROBLEMEN:")
print(f"  Te oud (< {MIN_REALISTIC_YEAR}): {len(old_year):,} auto's")
print(f"  Toekomst (> {MAX_REALISTIC_YEAR}): {len(future_year):,} auto's")
print(f"  Totaal bouwjaar problemen: {len(year_issues):,} ({len(year_issues)/len(df)*100:.2f}%)")

# Combinaties van problemen
both_price_and_power = df[
    ((df['prijs'] < MIN_REALISTIC_PRICE) | (df['prijs'] > MAX_REALISTIC_PRICE)) &
    ((df['vermogen'] < MIN_REALISTIC_POWER) | (df['vermogen'] > MAX_REALISTIC_POWER))
]

print(f"\nCOMBINATIE PROBLEMEN:")
print(f"  Zowel prijs als vermogen onrealistisch: {len(both_price_and_power):,} auto's")

# Filter voor realistische auto's
realistic_cars = df[
    (df['prijs'] >= MIN_REALISTIC_PRICE) & (df['prijs'] <= MAX_REALISTIC_PRICE) &
    (df['vermogen'] >= MIN_REALISTIC_POWER) & (df['vermogen'] <= MAX_REALISTIC_POWER) &
    (df['bouwjaar'] >= MIN_REALISTIC_YEAR) & (df['bouwjaar'] <= MAX_REALISTIC_YEAR)
]

print(f"\n{'='*60}")
print("SAMENVATTING")
print(f"{'='*60}")
print(f"Totaal auto's: {len(df):,}")
print(f"Realistische auto's: {len(realistic_cars):,} ({len(realistic_cars)/len(df)*100:.2f}%)")
print(f"Auto's met problemen: {len(df) - len(realistic_cars):,} ({(len(df) - len(realistic_cars))/len(df)*100:.2f}%)")

print("\n" + "=" * 80)
print("AANBEVELING VOOR FILTERS:")
print("=" * 80)
print(f"- Filter prijs: {MIN_REALISTIC_PRICE} <= prijs <= {MAX_REALISTIC_PRICE}")
print(f"- Filter vermogen: {MIN_REALISTIC_POWER} <= vermogen <= {MAX_REALISTIC_POWER} KW")
print(f"- Filter bouwjaar: {MIN_REALISTIC_YEAR} <= bouwjaar <= {MAX_REALISTIC_YEAR}")

