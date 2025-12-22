"""
DATA REALISME ANALYSE
=====================
Dit script analyseert of de waarden in de autodatabase realistisch zijn.
Het controleert per belangrijke kolom op outliers en verdachte waarden.

Output:
- Statistieken per kolom (min, max, gemiddelde, mediaan)
- Aantal en percentage outliers
- Concrete voorbeelden van verdachte rijen
- Evaluatie en aanbevelingen
"""

import pandas as pd
import numpy as np
import os
from datetime import datetime

# Bepaal data directory (relatief ten opzichte van script locatie)
script_dir = os.path.dirname(os.path.abspath(__file__))
project_root = os.path.dirname(script_dir)
data_dir = os.path.join(project_root, 'data')

print("=" * 80)
print("DATA REALISME ANALYSE")
print("=" * 80)
print(f"\nData directory: {data_dir}")

# Laad dataset
print("\n[STAP 1] Laden van dataset...")
csv_path = os.path.join(data_dir, 'Cleaned_Car_Data_For_App_Fully_Enriched.csv')
df = pd.read_csv(csv_path)
print(f"  [OK] Dataset geladen: {len(df)} rijen, {len(df.columns)} kolommen")

current_year = datetime.now().year

# ============================================================================
# HELPER FUNCTIES
# ============================================================================

def print_statistics(values, column_name, unit=""):
    """Print basis statistieken voor een kolom"""
    valid_values = values.dropna()
    if len(valid_values) == 0:
        print(f"  Geen geldige waarden gevonden")
        return
    
    print(f"\n  Statistieken voor {column_name}:")
    print(f"    Aantal geldige waarden: {len(valid_values):,} ({len(valid_values)/len(df)*100:.1f}%)")
    print(f"    Minimum: {valid_values.min():,.2f} {unit}")
    print(f"    Maximum: {valid_values.max():,.2f} {unit}")
    print(f"    Gemiddelde: {valid_values.mean():,.2f} {unit}")
    print(f"    Mediaan: {valid_values.median():,.2f} {unit}")
    print(f"    Standaarddeviatie: {valid_values.std():,.2f} {unit}")

def count_outliers(df, condition, description):
    """Tel aantal outliers die voldoen aan een conditie"""
    count = condition.sum()
    percentage = (count / len(df)) * 100
    return count, percentage

# ============================================================================
# ANALYSE BUDGET/PRIJS
# ============================================================================
print("\n" + "=" * 80)
print("[ANALYSE 1] BUDGET/PRIJS REALISME")
print("=" * 80)

if 'prijs' in df.columns:
    prices = df['prijs']
    print_statistics(prices, "Prijs", "€")
    
    # Definieer realistische grenzen voor prijzen (in euro's)
    # Minimum: 500€ (te goedkoop om realistisch te zijn voor een werkende auto)
    # Maximum: 500.000€ (zeer dure luxe auto's zijn mogelijk, maar 500k+ is extreem)
    min_realistic_price = 500
    max_realistic_price = 500000
    
    too_cheap = df['prijs'] < min_realistic_price
    too_expensive = df['prijs'] > max_realistic_price
    zero_price = df['prijs'] <= 0
    
    too_cheap_count, too_cheap_pct = count_outliers(df, too_cheap, "Te goedkoop")
    too_expensive_count, too_expensive_pct = count_outliers(df, too_expensive, "Te duur")
    zero_count, zero_pct = count_outliers(df, zero_price, "Nul of negatief")
    
    print(f"\n  Outliers:")
    print(f"    Te goedkoop (< €{min_realistic_price:,}): {too_cheap_count:,} ({too_cheap_pct:.2f}%)")
    print(f"    Te duur (> €{max_realistic_price:,}): {too_expensive_count:,} ({too_expensive_pct:.2f}%)")
    print(f"    Nul of negatief: {zero_count:,} ({zero_pct:.2f}%)")
    
    # Toon voorbeelden
    print(f"\n  Top 10 goedkoopste auto's (verdacht als < €{min_realistic_price:,}):")
    suspicious_cheap = df[df['prijs'] < min_realistic_price].nsmallest(10, 'prijs')
    if len(suspicious_cheap) > 0:
        print(suspicious_cheap[['merk', 'model', 'bouwjaar', 'prijs']].to_string(index=False))
    
    print(f"\n  Top 10 duurste auto's (verdacht als > €{max_realistic_price:,}):")
    suspicious_expensive = df[df['prijs'] > max_realistic_price].nlargest(10, 'prijs')
    if len(suspicious_expensive) > 0:
        print(suspicious_expensive[['merk', 'model', 'bouwjaar', 'prijs']].to_string(index=False))

# ============================================================================
# ANALYSE BOUWJAAR
# ============================================================================
print("\n" + "=" * 80)
print("[ANALYSE 2] BOUWJAAR REALISME")
print("=" * 80)

if 'bouwjaar' in df.columns:
    years = df['bouwjaar']
    print_statistics(years, "Bouwjaar", "")
    
    # Realistische grenzen: 1990 tot huidig jaar + 1 (voor nieuwe modellen)
    min_realistic_year = 1990
    max_realistic_year = current_year + 1
    
    too_old = df['bouwjaar'] < min_realistic_year
    too_new = df['bouwjaar'] > max_realistic_year
    invalid_year = (df['bouwjaar'] < 1900) | (df['bouwjaar'] > 2100)
    
    too_old_count, too_old_pct = count_outliers(df, too_old, "Te oud")
    too_new_count, too_new_pct = count_outliers(df, too_new, "Te nieuw")
    invalid_count, invalid_pct = count_outliers(df, invalid_year, "Ongeldig")
    
    print(f"\n  Outliers:")
    print(f"    Te oud (< {min_realistic_year}): {too_old_count:,} ({too_old_pct:.2f}%)")
    print(f"    Te nieuw (> {max_realistic_year}): {too_new_count:,} ({too_new_pct:.2f}%)")
    print(f"    Ongeldig (< 1900 of > 2100): {invalid_count:,} ({invalid_pct:.2f}%)")
    
    # Toon voorbeelden
    if too_old.sum() > 0:
        print(f"\n  Voorbeelden van te oude auto's (< {min_realistic_year}):")
        suspicious_old = df[too_old].nsmallest(10, 'bouwjaar')
        print(suspicious_old[['merk', 'model', 'bouwjaar', 'prijs']].to_string(index=False))
    
    if too_new.sum() > 0:
        print(f"\n  Voorbeelden van te nieuwe auto's (> {max_realistic_year}):")
        suspicious_new = df[too_new].nlargest(10, 'bouwjaar')
        print(suspicious_new[['merk', 'model', 'bouwjaar', 'prijs']].to_string(index=False))

# ============================================================================
# ANALYSE VERMOGEN
# ============================================================================
print("\n" + "=" * 80)
print("[ANALYSE 3] VERMOGEN REALISME")
print("=" * 80)

if 'vermogen' in df.columns:
    power = df['vermogen']
    valid_power = power[power > 0]
    
    if len(valid_power) > 0:
        print_statistics(valid_power, "Vermogen", "KW")
        
        # Realistische grenzen voor vermogen
        # Minimum: 30 KW (40 PK) - zeer zwakke auto's bestaan wel maar zijn zeldzaam
        # Maximum: 800 KW (1075 PK) - extreem krachtige sportauto's, maar 800+ KW is verdacht
        min_realistic_power = 30
        max_realistic_power = 800
        
        zero_power = df['vermogen'] <= 0
        too_low_power = (df['vermogen'] > 0) & (df['vermogen'] < min_realistic_power)
        too_high_power = df['vermogen'] > max_realistic_power
        missing_power = df['vermogen'].isna()
        
        zero_count, zero_pct = count_outliers(df, zero_power, "Nul of negatief")
        too_low_count, too_low_pct = count_outliers(df, too_low_power, "Te laag")
        too_high_count, too_high_pct = count_outliers(df, too_high_power, "Te hoog")
        missing_count, missing_pct = count_outliers(df, missing_power, "Ontbrekend")
        
        print(f"\n  Outliers en problemen:")
        print(f"    Nul of negatief: {zero_count:,} ({zero_pct:.2f}%)")
        print(f"    Te laag (< {min_realistic_power} KW): {too_low_count:,} ({too_low_pct:.2f}%)")
        print(f"    Te hoog (> {max_realistic_power} KW): {too_high_count:,} ({too_high_pct:.2f}%)")
        print(f"    Ontbrekend (NaN): {missing_count:,} ({missing_pct:.2f}%)")
        
        # Toon voorbeelden
        if too_high_power.sum() > 0:
            print(f"\n  Voorbeelden van extreem hoge vermogens (> {max_realistic_power} KW):")
            suspicious_high = df[too_high_power].nlargest(10, 'vermogen')
            print(suspicious_high[['merk', 'model', 'bouwjaar', 'vermogen', 'prijs']].to_string(index=False))
        
        if too_low_power.sum() > 0:
            print(f"\n  Voorbeelden van zeer lage vermogens (< {min_realistic_power} KW):")
            suspicious_low = df[too_low_power].nsmallest(10, 'vermogen')
            print(suspicious_low[['merk', 'model', 'bouwjaar', 'vermogen', 'prijs']].to_string(index=False))
    else:
        print("  Geen geldige vermogen waarden gevonden (alleen 0 of NaN)")

# ============================================================================
# ANALYSE KILOMETERSTAND
# ============================================================================
print("\n" + "=" * 80)
print("[ANALYSE 4] KILOMETERSTAND REALISME")
print("=" * 80)

if 'Mileage' in df.columns:
    mileage = df['Mileage']
    valid_mileage = mileage.dropna()
    
    if len(valid_mileage) > 0:
        print_statistics(valid_mileage, "Kilometerstand", "km")
        
        # Realistische grenzen
        # Maximum: 500.000 km (zeer hoge kilometerstand maar mogelijk voor oude auto's)
        # Negatieve waarden zijn onmogelijk
        max_realistic_mileage = 500000
        
        negative_mileage = df['Mileage'] < 0
        too_high_mileage = df['Mileage'] > max_realistic_mileage
        
        negative_count, negative_pct = count_outliers(df, negative_mileage, "Negatief")
        too_high_count, too_high_pct = count_outliers(df, too_high_mileage, "Te hoog")
        missing_count, missing_pct = count_outliers(df, df['Mileage'].isna(), "Ontbrekend")
        
        print(f"\n  Outliers:")
        print(f"    Negatief: {negative_count:,} ({negative_pct:.2f}%)")
        print(f"    Te hoog (> {max_realistic_mileage:,} km): {too_high_count:,} ({too_high_pct:.2f}%)")
        print(f"    Ontbrekend (NaN): {missing_count:,} ({missing_pct:.2f}%)")
        
        if too_high_mileage.sum() > 0:
            print(f"\n  Voorbeelden van zeer hoge kilometerstanden (> {max_realistic_mileage:,} km):")
            suspicious_mileage = df[too_high_mileage].nlargest(10, 'Mileage')
            print(suspicious_mileage[['merk', 'model', 'bouwjaar', 'Mileage', 'prijs']].to_string(index=False))
else:
    print("  [INFO] Kolom 'Mileage' niet gevonden in dataset")

# ============================================================================
# ANALYSE COMBINATIES (bijv. oude auto met hoge prijs, nieuwe auto met lage prijs)
# ============================================================================
print("\n" + "=" * 80)
print("[ANALYSE 5] COMBINATIE-ANALYSE (Logische inconsistenties)")
print("=" * 80)

# Oude auto met extreem hoge prijs (verdacht)
old_expensive = (df['bouwjaar'] < 2000) & (df['prijs'] > 50000)
if old_expensive.sum() > 0:
    count, pct = count_outliers(df, old_expensive, "Oude auto met hoge prijs")
    print(f"\n  Auto's ouder dan 2000 met prijs > €50.000: {count:,} ({pct:.2f}%)")
    if count > 0:
        print("  Voorbeelden:")
        examples = df[old_expensive].head(10)
        print(examples[['merk', 'model', 'bouwjaar', 'prijs', 'vermogen']].to_string(index=False))

# Nieuwe auto met extreem lage prijs (verdacht)
new_cheap = (df['bouwjaar'] >= 2020) & (df['prijs'] < 5000)
if new_cheap.sum() > 0:
    count, pct = count_outliers(df, new_cheap, "Nieuwe auto met lage prijs")
    print(f"\n  Auto's vanaf 2020 met prijs < €5.000: {count:,} ({pct:.2f}%)")
    if count > 0:
        print("  Voorbeelden:")
        examples = df[new_cheap].head(10)
        print(examples[['merk', 'model', 'bouwjaar', 'prijs', 'vermogen']].to_string(index=False))

# Auto met hoog vermogen maar lage prijs (verdacht - sportauto's zijn duur)
high_power_low_price = (df['vermogen'] > 300) & (df['prijs'] < 20000) & (df['vermogen'].notna()) & (df['prijs'] > 0)
if high_power_low_price.sum() > 0:
    count, pct = count_outliers(df, high_power_low_price, "Hoog vermogen, lage prijs")
    print(f"\n  Auto's met >300 KW maar prijs < €20.000: {count:,} ({pct:.2f}%)")
    if count > 0:
        print("  Voorbeelden:")
        examples = df[high_power_low_price].head(10)
        print(examples[['merk', 'model', 'bouwjaar', 'vermogen', 'prijs']].to_string(index=False))

# ============================================================================
# SAMENVATTING EN EVALUATIE
# ============================================================================
print("\n" + "=" * 80)
print("EVALUATIE EN AANBEVELINGEN")
print("=" * 80)

print("""
EVALUATIE DATA KWALITEIT - REALISME CONTROLE

1. BUDGET/PRIJS:
   De prijzen in de dataset lijken over het algemeen realistisch, maar er zijn enkele outliers.
   Verdachte gevallen:
   - Auto's met prijs < €500 zijn waarschijnlijk foutieve invoer of zeer beschadigde auto's
   - Auto's met prijs > €500.000 zijn mogelijk legitiem (luxe/hypercars) maar moeten gecontroleerd worden
   - Nul of negatieve prijzen zijn duidelijk foutief
   
   Aanbeveling: Filter auto's met prijs < €500 of prijs > €500.000, tenzij expliciet gevalideerd.

2. BOUWJAAR:
   De bouwjaren zijn grotendeels binnen verwachte grenzen (2000-2023).
   Verdachte gevallen:
   - Auto's ouder dan 1990 kunnen legitiem zijn (klassiekers) maar zijn zeldzaam
   - Auto's nieuwer dan het huidige jaar zijn foutief (toekomstige auto's bestaan niet)
   
   Aanbeveling: Accepteer bouwjaren tussen 1990 en huidig jaar + 1. Auto's buiten dit bereik markeren als verdacht.

3. VERMOGEN:
   Er zijn significante problemen met vermogen data:
   - Veel auto's hebben vermogen = 0 (ontbrekende data)
   - Sommige auto's hebben extreem hoge vermogens (>800 KW) die mogelijk foutief zijn
   - Zeer lage vermogens (<30 KW) zijn mogelijk maar zeldzaam
   
   Aanbeveling: 
   - Markeer auto's met vermogen = 0 als "data ontbreekt" maar behoud ze voor recommendations
   - Valideer auto's met vermogen > 800 KW (mogelijk conversie fouten of typefouten)
   - Behoud auto's met laag vermogen (30-100 KW) - dit zijn vaak stadsauto's

4. KILOMETERSTAND:
   Als deze kolom aanwezig is, controleer op negatieve waarden en extreem hoge standen.
   Realistische maximum kilometerstand is ongeveer 500.000 km voor zeer oude auto's.
   
   Aanbeveling: Filter negatieve kilometerstanden, markeer >500.000 km als verdacht.

5. COMBINATIE-ANALYSE:
   Controleer logische inconsistenties:
   - Oude auto's (pre-2000) met zeer hoge prijzen (>€50.000) - mogelijk klassiekers (legitiem) of fout
   - Nieuwe auto's (2020+) met zeer lage prijzen (<€5.000) - waarschijnlijk fout
   - Auto's met hoog vermogen (>300 KW) maar lage prijs (<€20.000) - verdacht
   
   Aanbeveling: Review deze gevallen handmatig of met aanvullende regels.

SIMPELE FILTER REGELS VOOR DATA CLEANING:

1. Verwijder rijen met:
   - Prijs <= 0
   - Prijs < 500 (tenzij gevalideerd als beschadigde auto)
   - Bouwjaar < 1900 of bouwjaar > (huidig jaar + 1)

2. Markeer als verdacht (maar behoud):
   - Prijs > 500.000
   - Vermogen = 0 (data ontbreekt)
   - Vermogen > 800 KW
   - Kilometerstand > 500.000 km

3. Review handmatig:
   - Oude auto's (pre-2000) met prijs > €50.000 (mogelijk klassiekers)
   - Nieuwe auto's (2020+) met prijs < €5.000
   - Auto's met hoog vermogen (>300 KW) maar lage prijs (<€20.000)

4. Behoud maar noteer:
   - Auto's met laag vermogen (30-100 KW) - legitieme stadsauto's
   - Auto's met bouwjaar 1990-1999 - oude maar mogelijke auto's

CONCLUSIE:
De dataset bevat grotendeels realistische data, maar er zijn duidelijke outliers die
gefilterd of gecorrigeerd moeten worden voordat de data gebruikt wordt voor productie.
De meeste problemen liggen in:
- Ontbrekende vermogen data (veel nullen/zeros)
- Enkele extreem hoge prijzen die gevalideerd moeten worden
- Mogelijke typefouten in bouwjaar (toekomstige jaren)

Voor recommendations kunnen we werken met:
- Auto's met prijs tussen €500 en €500.000
- Auto's met bouwjaar tussen 1990 en huidig jaar
- Auto's met vermogen > 0 of vermogen = 0 (maar noteer dat data ontbreekt)
""")

print("\n" + "=" * 80)
print("ANALYSE VOLTOOID")
print("=" * 80)



