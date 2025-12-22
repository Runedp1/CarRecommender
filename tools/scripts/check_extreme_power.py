"""
Controleer hoeveel auto's extreem hoog vermogen hebben (>1000 KW)
"""

import pandas as pd
import os

# Bepaal data directory
script_dir = os.path.dirname(os.path.abspath(__file__))
project_root = os.path.dirname(script_dir)
data_dir = os.path.join(project_root, 'data')

print("=" * 80)
print("ANALYSE EXTREEM HOOG VERMOGEN")
print("=" * 80)

# Laad dataset
csv_path = os.path.join(data_dir, 'Cleaned_Car_Data_For_App_Fully_Enriched.csv')
df = pd.read_csv(csv_path)

print(f"\nTotaal auto's in dataset: {len(df):,}")

# Filter op vermogen > 1000 KW
extreme_power = df[df['vermogen'] > 1000]

print(f"\nAuto's met vermogen > 1000 KW: {len(extreme_power):,} ({len(extreme_power)/len(df)*100:.2f}%)")

if len(extreme_power) > 0:
    print("\nVoorbeelden van auto's met extreem hoog vermogen (>1000 KW):")
    print(extreme_power[['merk', 'model', 'bouwjaar', 'vermogen', 'prijs']].to_string(index=False))
    
    print(f"\nStatistieken van extreem hoog vermogen:")
    print(f"  Minimum: {extreme_power['vermogen'].min():.2f} KW")
    print(f"  Maximum: {extreme_power['vermogen'].max():.2f} KW")
    print(f"  Gemiddelde: {extreme_power['vermogen'].mean():.2f} KW")
    print(f"  Mediaan: {extreme_power['vermogen'].median():.2f} KW")

# Check ook andere verdachte ranges
print("\n" + "=" * 80)
print("VERMOGEN DISTRIBUTIE")
print("=" * 80)

print(f"\nAuto's met vermogen > 800 KW: {len(df[df['vermogen'] > 800]):,}")
print(f"Auto's met vermogen > 500 KW: {len(df[df['vermogen'] > 500]):,}")
print(f"Auto's met vermogen > 300 KW: {len(df[df['vermogen'] > 300]):,}")

# Realistisch maximum voor auto's is ongeveer 800 KW (sommige hypercars)
realistic_max = 800
unrealistic = df[df['vermogen'] > realistic_max]

print(f"\n{'='*60}")
print(f"Auto's met onrealistisch hoog vermogen (> {realistic_max} KW): {len(unrealistic):,} ({len(unrealistic)/len(df)*100:.2f}%)")

if len(unrealistic) > 0:
    print("\nTop 20 auto's met onrealistisch hoog vermogen:")
    top_unrealistic = unrealistic.nlargest(20, 'vermogen')
    print(top_unrealistic[['merk', 'model', 'bouwjaar', 'vermogen', 'prijs']].to_string(index=False))

print("\n" + "=" * 80)
print("ANALYSE VOLTOOID")
print("=" * 80)


