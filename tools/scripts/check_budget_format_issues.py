"""
Check op budget formatting problemen - specifiek zoeken naar problemen met Budget waarden
"""

import pandas as pd
import os

# Bepaal data directory
script_dir = os.path.dirname(os.path.abspath(__file__))
project_root = os.path.dirname(script_dir)
data_dir = os.path.join(project_root, 'data')

print("=" * 80)
print("CHECK BUDGET FORMATTING PROBLEMEN")
print("=" * 80)

# Laad dataset
csv_path = os.path.join(data_dir, 'Cleaned_Car_Data_For_App_Fully_Enriched.csv')
df = pd.read_csv(csv_path)

print(f"\nTotaal auto's: {len(df):,}")

# Check op problematische budget waarden
print(f"\n{'='*60}")
print("BUDGET WAARDEN ANALYSE")
print(f"{'='*60}")

# Check voor 0, negatief, of zeer klein
zero_budget = df[df['prijs'] == 0]
negative_budget = df[df['prijs'] < 0]
very_low_budget = df[(df['prijs'] > 0) & (df['prijs'] < 500)]

print(f"\nBudget = 0: {len(zero_budget):,} auto's")
print(f"Budget < 0: {len(negative_budget):,} auto's")
print(f"Budget tussen 0 en €500: {len(very_low_budget):,} auto's")

# Check voor zeer hoge budget waarden (mogelijk conversiefouten)
very_high_budget = df[df['prijs'] > 1000000]

print(f"\nBudget > €1.000.000: {len(very_high_budget):,} auto's")

if len(very_high_budget) > 0:
    print("\nVoorbeelden:")
    print(very_high_budget[['merk', 'model', 'bouwjaar', 'prijs']].head(10).to_string(index=False))

# Toon statistieken
print(f"\n{'='*60}")
print("BUDGET STATISTIEKEN")
print(f"{'='*60}")

valid_budget = df[df['prijs'] > 0]
print(f"\nAuto's met geldige budget (> 0): {len(valid_budget):,}")
if len(valid_budget) > 0:
    print(f"  Minimum: €{valid_budget['prijs'].min():,.2f}")
    print(f"  Maximum: €{valid_budget['prijs'].max():,.2f}")
    print(f"  Gemiddelde: €{valid_budget['prijs'].mean():,.2f}")
    print(f"  Mediaan: €{valid_budget['prijs'].median():,.2f}")

print("\n" + "=" * 80)
print("ANALYSE VOLTOOID")
print("=" * 80)


