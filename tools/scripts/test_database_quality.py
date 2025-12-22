"""
Script om de kwaliteit van de Cleaned_Car_Data_For_App_Fully_Enriched.csv database te testen
Analyseert: missing values, data distributie, duplicaten, consistentie, en coverage
"""

import pandas as pd
import numpy as np
import os

# Bepaal data directory (relatief ten opzichte van script locatie)
script_dir = os.path.dirname(os.path.abspath(__file__))
project_root = os.path.dirname(script_dir)
data_dir = os.path.join(project_root, 'data')

print("=" * 80)
print("DATABASE KWALITEIT ANALYSE")
print("=" * 80)

# Laad de dataset
print("\n[1] Laden van dataset...")
print(f"  Data directory: {data_dir}")
df = pd.read_csv(os.path.join(data_dir, 'Cleaned_Car_Data_For_App_Fully_Enriched.csv'))
print(f"  [OK] Dataset geladen: {len(df)} rijen, {len(df.columns)} kolommen")

# Basis statistieken
print("\n" + "=" * 80)
print("[2] BASIS STATISTIEKEN")
print("=" * 80)
print(f"Totaal aantal auto's: {len(df):,}")
print(f"Unieke merken: {df['merk'].nunique()}")
print(f"Unieke modellen: {df['model'].nunique()}")
print(f"Bereik bouwjaren: {df['bouwjaar'].min():.0f} - {df['bouwjaar'].max():.0f}")

# Missing values analyse
print("\n" + "=" * 80)
print("[3] MISSING VALUES ANALYSE")
print("=" * 80)

missing_data = pd.DataFrame({
    'Kolom': df.columns,
    'Totaal Missing': df.isnull().sum(),
    'Percentage Missing': (df.isnull().sum() / len(df)) * 100,
    'Non-Null Count': df.notna().sum(),
    'Percentage Complete': (df.notna().sum() / len(df)) * 100
}).sort_values('Percentage Missing', ascending=False)

print("\nTop 10 kolommen met meeste missing values:")
print(missing_data.head(10).to_string(index=False))

print("\nKolommen met 100% compleetheid:")
complete_cols = missing_data[missing_data['Percentage Missing'] == 0]
print(f"  {len(complete_cols)} kolommen: {', '.join(complete_cols['Kolom'].tolist())}")

# Kritieke kolommen analyse
print("\n" + "=" * 80)
print("[4] KRITIEKE KOLOMMEN KWALITEIT")
print("=" * 80)

critical_columns = {
    'merk': 'Merk (verplicht)',
    'model': 'Model (verplicht)',
    'bouwjaar': 'Bouwjaar',
    'brandstof': 'Brandstoftype',
    'vermogen': 'Vermogen (KW)',
    'prijs': 'Prijs',
}

for col, desc in critical_columns.items():
    if col in df.columns:
        missing = df[col].isnull().sum()
        zero_or_empty = 0
        if df[col].dtype in ['int64', 'float64']:
            zero_or_empty = (df[col] == 0).sum() if col != 'bouwjaar' else 0
        
        total_issues = missing + zero_or_empty
        completeness = ((len(df) - total_issues) / len(df)) * 100
        
        status = "[OK] GOED" if completeness >= 90 else "[!] AANDACHT" if completeness >= 70 else "[X] PROBLEEM"
        
        print(f"\n{desc} ({col}):")
        print(f"  {status} - {completeness:.1f}% compleet")
        print(f"  Missing: {missing:,} ({missing/len(df)*100:.1f}%)")
        if zero_or_empty > 0:
            print(f"  Nul/lege waarden: {zero_or_empty:,} ({zero_or_empty/len(df)*100:.1f}%)")
        print(f"  Geldige waarden: {len(df) - total_issues:,}")

# Data distributie analyse
print("\n" + "=" * 80)
print("[5] DATA DISTRIBUTIE")
print("=" * 80)

print("\nBouwjaar distributie:")
print(df['bouwjaar'].describe())
print(f"  Meest voorkomend jaar: {df['bouwjaar'].mode().values[0]} ({df['bouwjaar'].value_counts().max()} auto's)")

print("\nVermogen distributie (alleen geldige waarden > 0):")
valid_power = df[df['vermogen'] > 0]['vermogen']
print(valid_power.describe())
print(f"  Auto's met geldig vermogen: {len(valid_power):,} ({len(valid_power)/len(df)*100:.1f}%)")

print("\nPrijs distributie (alleen geldige waarden > 0):")
valid_price = df[df['prijs'] > 0]['prijs']
print(valid_price.describe())
print(f"  Auto's met geldige prijs: {len(valid_price):,} ({len(valid_price)/len(df)*100:.1f}%)")
print(f"  Gemiddelde prijs: €{valid_price.mean():,.2f}")
print(f"  Mediaan prijs: €{valid_price.median():,.2f}")

# Brandstof type distributie
print("\nBrandstof type distributie:")
fuel_dist = df['brandstof'].value_counts()
print(fuel_dist.head(10))

# Top merken
print("\nTop 10 merken:")
brand_dist = df['merk'].value_counts().head(10)
print(brand_dist)

# Duplicaten analyse
print("\n" + "=" * 80)
print("[6] DUPLICATEN ANALYSE")
print("=" * 80)

# Exacte duplicaten (volledige rijen)
exact_duplicates = df.duplicated().sum()
print(f"Exacte duplicaten (volledige rijen): {exact_duplicates:,}")

# Duplicaten op basis van merk+model+bouwjaar (mogelijk verschillende prijzen/configuraties)
key_duplicates = df.duplicated(subset=['merk', 'model', 'bouwjaar'], keep=False).sum()
print(f"Auto's met zelfde merk+model+bouwjaar: {key_duplicates:,} ({key_duplicates/len(df)*100:.1f}%)")
if key_duplicates > 0:
    duplicate_groups = df[df.duplicated(subset=['merk', 'model', 'bouwjaar'], keep=False)].groupby(['merk', 'model', 'bouwjaar']).size()
    print(f"  Unieke combinaties met duplicaten: {len(duplicate_groups):,}")

# Feature coverage analyse
print("\n" + "=" * 80)
print("[7] FEATURE COVERAGE (Nieuwe kolommen)")
print("=" * 80)

new_features = {
    'CO2_wltp': 'CO2 uitstoot',
    'Electric_range_km': 'Elektrische actieradius',
    'Engine_cm3': 'Motorinhoud (cm³)',
    'Transmission': 'Transmissie type',
    'Mileage': 'Kilometerstand',
    'Condition': 'Conditie',
    'Torque_Nm': 'Koppel (Nm)',
    'Performance_0_100_sec': '0-100 km/h tijd',
    'Seats': 'Aantal zitplaatsen',
    'Engine_Size_L': 'Motorinhoud (L)',
    'Acceleration_0_60_sec': '0-60 mph acceleratie',
    'Number_of_Cylinders': 'Aantal cilinders',
    'Number_of_Doors': 'Aantal deuren',
    'Tax': 'Belasting',
    'MPG': 'Miles per gallon',
    'Top_Speed_mph': 'Top snelheid (mph)',
    'Mileage_MPG': 'Brandstofverbruik (MPG)',
}

print("\nFeature coverage (percentage auto's met deze informatie):")
feature_coverage = []
for col, desc in new_features.items():
    if col in df.columns:
        coverage = (df[col].notna().sum() / len(df)) * 100
        status = "[OK]" if coverage >= 50 else "[!]" if coverage >= 25 else "[X]"
        feature_coverage.append({
            'Feature': desc,
            'Kolom': col,
            'Coverage %': coverage,
            'Count': df[col].notna().sum(),
            'Status': status
        })

coverage_df = pd.DataFrame(feature_coverage).sort_values('Coverage %', ascending=False)
print(coverage_df.to_string(index=False))

# Data kwaliteit score
print("\n" + "=" * 80)
print("[8] DATA KWALITEIT SCORE")
print("=" * 80)

# Bereken overall kwaliteit score
critical_weight = 0.4  # 40% voor kritieke kolommen
feature_weight = 0.3   # 30% voor feature coverage
completeness_weight = 0.3  # 30% voor overall completeness

# Kritieke kolommen score
critical_score = 0
critical_count = 0
for col in ['merk', 'model', 'bouwjaar', 'brandstof']:
    if col in df.columns:
        critical_score += (df[col].notna().sum() / len(df)) * 100
        critical_count += 1
critical_score = critical_score / critical_count if critical_count > 0 else 0

# Feature coverage score (gemiddelde coverage van alle features)
feature_score = coverage_df['Coverage %'].mean() if len(coverage_df) > 0 else 0

# Overall completeness score
overall_completeness = (df.notna().sum().sum() / (len(df) * len(df.columns))) * 100

# Totale kwaliteit score
quality_score = (critical_score * critical_weight + 
                 feature_score * feature_weight + 
                 overall_completeness * completeness_weight)

print(f"\nKritieke kolommen score: {critical_score:.1f}%")
print(f"Feature coverage score: {feature_score:.1f}%")
print(f"Overall completeness: {overall_completeness:.1f}%")
print(f"\n{'='*60}")
print(f"TOTALE KWALITEIT SCORE: {quality_score:.1f}%")

if quality_score >= 80:
    print("[OK] UITSTEKEND - Database is zeer compleet en bruikbaar")
elif quality_score >= 65:
    print("[OK] GOED - Database is goed, maar sommige features kunnen verbeterd worden")
elif quality_score >= 50:
    print("[!] MATIG - Database heeft redelijke kwaliteit, maar er is ruimte voor verbetering")
else:
    print("[X] LAAG - Database heeft veel missing data, overweeg extra databronnen")

# Aanbevelingen
print("\n" + "=" * 80)
print("[9] AANBEVELINGEN")
print("=" * 80)

recommendations = []

if df['vermogen'].isna().sum() > len(df) * 0.1:
    recommendations.append("[!] Veel missing vermogen waarden - overweeg externe data bronnen")

if coverage_df[coverage_df['Coverage %'] < 25].shape[0] > 0:
    low_coverage = coverage_df[coverage_df['Coverage %'] < 25]
    recommendations.append(f"[!] {len(low_coverage)} features hebben lage coverage (<25%) - overweeg deze te verrijken")

if exact_duplicates > len(df) * 0.05:
    recommendations.append("[!] Veel exacte duplicaten gevonden - overweeg deze te verwijderen")

if key_duplicates > len(df) * 0.3:
    recommendations.append("[!] Veel auto's met zelfde merk+model+bouwjaar - dit kan legitiem zijn (verschillende configuraties)")

if not recommendations:
    recommendations.append("[OK] Geen kritieke problemen gevonden - database kwaliteit is goed")

for i, rec in enumerate(recommendations, 1):
    print(f"{i}. {rec}")

print("\n" + "=" * 80)
print("ANALYSE VOLTOOID")
print("=" * 80)
