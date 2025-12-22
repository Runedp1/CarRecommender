"""
Controleer hoeveel auto's zeer laag vermogen hebben (<30 KW of verdachte waarden zoals 0.008)
"""

import pandas as pd
import os

# Bepaal data directory
script_dir = os.path.dirname(os.path.abspath(__file__))
project_root = os.path.dirname(script_dir)
data_dir = os.path.join(project_root, 'data')

print("=" * 80)
print("ANALYSE ZEER LAAG VERMOGEN")
print("=" * 80)

# Laad dataset
csv_path = os.path.join(data_dir, 'Cleaned_Car_Data_For_App_Fully_Enriched.csv')
df = pd.read_csv(csv_path)

print(f"\nTotaal auto's in dataset: {len(df):,}")

# Filter op zeer laag vermogen
very_low_power = df[(df['vermogen'] > 0) & (df['vermogen'] < 30)]

print(f"\nAuto's met vermogen tussen 0 en 30 KW: {len(very_low_power):,} ({len(very_low_power)/len(df)*100:.2f}%)")

if len(very_low_power) > 0:
    print("\nTop 20 auto's met zeer laag vermogen:")
    top_low = very_low_power.nsmallest(20, 'vermogen')
    print(top_low[['merk', 'model', 'bouwjaar', 'vermogen', 'prijs']].to_string(index=False))

# Check verdachte zeer lage waarden (< 1 KW - waarschijnlijk conversiefouten)
suspicious_low = df[(df['vermogen'] > 0) & (df['vermogen'] < 1)]

print(f"\n{'='*60}")
print(f"VERDACHT: Auto's met vermogen < 1 KW (waarschijnlijk conversiefouten):")
print(f"Aantal: {len(suspicious_low):,} ({len(suspicious_low)/len(df)*100:.2f}%)")

if len(suspicious_low) > 0:
    print("\nVoorbeelden:")
    print(suspicious_low[['merk', 'model', 'bouwjaar', 'vermogen', 'prijs']].head(20).to_string(index=False))

# Check auto's met vermogen = 0 of NaN
zero_or_missing = df[(df['vermogen'] == 0) | (df['vermogen'].isna())]
print(f"\n{'='*60}")
print(f"Auto's met vermogen = 0 of ontbrekend: {len(zero_or_missing):,} ({len(zero_or_missing)/len(df)*100:.2f}%)")

print("\n" + "=" * 80)
print("ANALYSE VOLTOOID")
print("=" * 80)


