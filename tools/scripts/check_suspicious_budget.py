"""
Controleer hoeveel auto's verdachte budget waarden hebben (zoals "C2-15" of andere tekst)
"""

import pandas as pd
import numpy as np
import os

# Bepaal data directory
script_dir = os.path.dirname(os.path.abspath(__file__))
project_root = os.path.dirname(script_dir)
data_dir = os.path.join(project_root, 'data')

print("=" * 80)
print("ANALYSE VERDACHTE BUDGET WAARDEN")
print("=" * 80)

# Laad dataset
csv_path = os.path.join(data_dir, 'Cleaned_Car_Data_For_App_Fully_Enriched.csv')
df = pd.read_csv(csv_path)

print(f"\nTotaal auto's in dataset: {len(df):,}")

# Check welke kolommen we hebben voor prijs
price_columns = [col for col in df.columns if 'prijs' in col.lower() or 'price' in col.lower() or 'budget' in col.lower()]
print(f"\nPrijs/budget kolommen gevonden: {price_columns}")

# Gebruik de hoofdprijs kolom (meestal 'prijs')
main_price_col = 'prijs' if 'prijs' in df.columns else (price_columns[0] if price_columns else None)

if main_price_col is None:
    print("GEEN PRIJS KOLOM GEVONDEN!")
    exit()

print(f"\nAnalyseren van kolom: '{main_price_col}'")
print(f"Datatype: {df[main_price_col].dtype}")

# Check voor niet-numerieke waarden (mogelijk tekst zoals "C2-15")
# Converteer naar string om te checken
df_price_str = df[main_price_col].astype(str)

# Check voor verdachte string patronen
suspicious_patterns = ['C2-15', 'C2', 'nan', 'NaN', 'None', 'NULL', 'null']

print(f"\n{'='*60}")
print("CHECK OP VERDACHTE TEKST WAARDEN IN PRIJS KOLOM")
print(f"{'='*60}")

for pattern in suspicious_patterns:
    matches = df_price_str.str.contains(pattern, case=False, na=False)
    count = matches.sum()
    if count > 0:
        print(f"\nGevonden '{pattern}': {count:,} auto's ({count/len(df)*100:.2f}%)")
        if count <= 20:
            print("\nVoorbeelden:")
            examples = df[matches]
            print(examples[['merk', 'model', 'bouwjaar', main_price_col]].head(20).to_string(index=False))

# Check voor niet-numerieke waarden
print(f"\n{'='*60}")
print("ALGEMENE PRIJS DATA ANALYSE")
print(f"{'='*60}")

# Probeer te converteren naar numeriek
numeric_prices = pd.to_numeric(df[main_price_col], errors='coerce')

# Check hoeveel succesvol geconverteerd werden
valid_numeric = numeric_prices.notna() & (numeric_prices > 0)
invalid_count = len(df) - valid_numeric.sum()

print(f"\nGeldige numerieke prijzen: {valid_numeric.sum():,} ({valid_numeric.sum()/len(df)*100:.2f}%)")
print(f"Ongeldige/missing prijzen: {invalid_count:,} ({invalid_count/len(df)*100:.2f}%)")

# Toon voorbeelden van ongeldige waarden
invalid_prices = df[~valid_numeric]
if len(invalid_prices) > 0:
    print(f"\nVoorbeelden van auto's met ongeldige prijs waarden:")
    print(f"Unieke ongeldige waarden: {invalid_prices[main_price_col].unique()[:20]}")
    
    print("\nEerste 20 auto's met ongeldige prijs:")
    print(invalid_prices[['merk', 'model', 'bouwjaar', main_price_col]].head(20).to_string(index=False))

# Check ook op zeer lage prijzen (mogelijk conversiefouten)
valid_price_data = df[valid_numeric]
if len(valid_price_data) > 0:
    very_low_prices = valid_price_data[valid_price_data[main_price_col] < 10]
    if len(very_low_prices) > 0:
        print(f"\n{'='*60}")
        print(f"AUTO'S MET ZEER LAGE PRIJS (< €10) - MOGELIJK CONVERSIEFOUTEN")
        print(f"Aantal: {len(very_low_prices):,}")
        print("\nVoorbeelden:")
        print(very_low_prices[['merk', 'model', 'bouwjaar', main_price_col]].head(20).to_string(index=False))

print("\n" + "=" * 80)
print("ANALYSE VOLTOOID")
print("=" * 80)


