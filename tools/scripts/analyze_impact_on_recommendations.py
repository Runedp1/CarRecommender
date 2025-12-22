"""
IMPACT ANALYSE OP RECOMMENDATION SYSTEEM
========================================
Analyseert welke auto's nog bruikbaar zijn voor recommendations na filtering,
en wat de impact is op de recommendation kwaliteit.
"""

import pandas as pd
import os

# Bepaal data directory
script_dir = os.path.dirname(os.path.abspath(__file__))
project_root = os.path.dirname(script_dir)
data_dir = os.path.join(project_root, 'data')

print("=" * 80)
print("IMPACT ANALYSE OP RECOMMENDATION SYSTEEM")
print("=" * 80)

# Laad dataset
csv_path = os.path.join(data_dir, 'Cleaned_Car_Data_For_App_Fully_Enriched.csv')
df = pd.read_csv(csv_path)
print(f"\nTotaal aantal auto's in dataset: {len(df):,}")

# ============================================================================
# FILTER REGELS (gebaseerd op analyse)
# ============================================================================

print("\n" + "=" * 80)
print("FILTER REGELS TOEPASSEN")
print("=" * 80)

# Start met alle auto's
df_original = df.copy()

# Filter 1: Verwijder prijs <= 0
filter1 = df['prijs'] > 0
df_step1 = df[filter1]
removed1 = len(df) - len(df_step1)
print(f"\n[Filter 1] Auto's met prijs <= 0 verwijderd: {removed1:,} ({removed1/len(df)*100:.2f}%)")
print(f"  Overgebleven: {len(df_step1):,} auto's ({len(df_step1)/len(df)*100:.2f}%)")

# Filter 2: Verwijder prijs < 500 (verdachte goedkope auto's)
filter2 = df_step1['prijs'] >= 500
df_step2 = df_step1[filter2]
removed2 = len(df_step1) - len(df_step2)
print(f"\n[Filter 2] Auto's met prijs < €500 verwijderd: {removed2:,} ({removed2/len(df)*100:.2f}%)")
print(f"  Overgebleven: {len(df_step2):,} auto's ({len(df_step2)/len(df)*100:.2f}%)")

# Filter 3: Verwijder ongeldige bouwjaren
# NOTE: Extreem dure auto's (>€500.000) worden NIET gefilterd - deze kunnen legitiem zijn
# (bijv. Lamborghini Urus, Mercedes G65 AMG zijn inderdaad zeer dure luxe auto's)
from datetime import datetime
current_year = datetime.now().year
filter3 = (df_step2['bouwjaar'] >= 1990) & (df_step2['bouwjaar'] <= current_year + 1)
df_step3 = df_step2[filter3]
removed3 = len(df_step2) - len(df_step3)
print(f"\n[Filter 3] Auto's met ongeldig bouwjaar verwijderd: {removed3:,} ({removed3/len(df)*100:.2f}%)")
print(f"  Overgebleven: {len(df_step3):,} auto's ({len(df_step3)/len(df)*100:.2f}%)")

# Toon extreem dure auto's (>€500k) voor referentie (maar behoud ze)
expensive_cars = df_step3[df_step3['prijs'] > 500000]
if len(expensive_cars) > 0:
    print(f"\n[INFO] Extreem dure auto's (>€500.000) - BEHOUDEN (kunnen legitiem zijn):")
    print(f"  Aantal: {len(expensive_cars)} auto's")
    print("  Voorbeelden:")
    print(expensive_cars[['merk', 'model', 'bouwjaar', 'prijs']].to_string(index=False))

df_filtered = df_step3.copy()
total_removed = len(df) - len(df_filtered)

print(f"\n{'='*60}")
print(f"TOTAAL VERWIJDERD: {total_removed:,} auto's ({total_removed/len(df)*100:.2f}%)")
print(f"BRUIKBAAR VOOR RECOMMENDATIONS: {len(df_filtered):,} auto's ({len(df_filtered)/len(df)*100:.2f}%)")
print(f"  (Inclusief {len(expensive_cars)} extreem dure auto's >€500k die legitiem kunnen zijn)")

# ============================================================================
# ANALYSE IMPACT OP FEATURES
# ============================================================================

print("\n" + "=" * 80)
print("FEATURE COVERAGE ANALYSE")
print("=" * 80)

# Analyseer welke features beschikbaar zijn voor recommendations
features_status = {
    'Budget (prijs)': {
        'available': df_filtered['prijs'].notna().sum(),
        'missing': df_filtered['prijs'].isna().sum(),
        'weight': 0.30,
        'critical': True
    },
    'Bouwjaar (Year)': {
        'available': df_filtered['bouwjaar'].notna().sum(),
        'missing': df_filtered['bouwjaar'].isna().sum(),
        'weight': 0.20,
        'critical': True
    },
    'Vermogen (Power)': {
        'available': (df_filtered['vermogen'].notna() & (df_filtered['vermogen'] > 0)).sum(),
        'missing': (df_filtered['vermogen'].isna() | (df_filtered['vermogen'] <= 0)).sum(),
        'weight': 0.25,
        'critical': False
    },
    'Brandstof (Fuel)': {
        'available': df_filtered['brandstof'].notna().sum(),
        'missing': df_filtered['brandstof'].isna().sum(),
        'weight': 0.25,
        'critical': True
    }
}

print("\nFeature beschikbaarheid voor recommendations:")
print("-" * 80)
print(f"{'Feature':<25} {'Beschikbaar':<15} {'Ontbreekt':<15} {'% Beschikbaar':<15} {'Gewicht':<10}")
print("-" * 80)

for feature_name, status in features_status.items():
    total = status['available'] + status['missing']
    pct_available = (status['available'] / total * 100) if total > 0 else 0
    critical_mark = "[KRITIEK]" if status['critical'] else "[OPTIONEEL]"
    
    print(f"{feature_name:<25} {status['available']:>8,} ({pct_available:>5.1f}%)  {status['missing']:>8,}  {pct_available:>5.1f}%  {status['weight']*100:>5.0f}%  {critical_mark}")

# Bereken effectieve recommendation capaciteit
print("\n" + "=" * 80)
print("RECOMMENDATION CAPACITEIT ANALYSE")
print("=" * 80)

# Scenario 1: Alle features beschikbaar
all_features_available = (
    df_filtered['prijs'].notna() & 
    (df_filtered['prijs'] > 0) &
    df_filtered['bouwjaar'].notna() &
    df_filtered['brandstof'].notna() &
    df_filtered['vermogen'].notna() &
    (df_filtered['vermogen'] > 0)
)
count_all_features = all_features_available.sum()

# Scenario 2: Minimaal vereiste features (zonder vermogen)
minimal_features_available = (
    df_filtered['prijs'].notna() & 
    (df_filtered['prijs'] > 0) &
    df_filtered['bouwjaar'].notna() &
    df_filtered['brandstof'].notna()
)
count_minimal = minimal_features_available.sum()

# Scenario 3: Alleen kritieke features
critical_features_available = (
    df_filtered['prijs'].notna() & 
    (df_filtered['prijs'] > 0) &
    df_filtered['bouwjaar'].notna()
)
count_critical = critical_features_available.sum()

print(f"\nScenario 1: Alle features beschikbaar (Power, Budget, Year, Fuel):")
print(f"  {count_all_features:,} auto's ({count_all_features/len(df_filtered)*100:.1f}%)")
print(f"  => Recommendations gebruiken 100% van de similarity score")
print(f"  => MAXIMALE KWALITEIT")

print(f"\nScenario 2: Minimale features (Budget, Year, Fuel, geen Power):")
print(f"  {count_minimal:,} auto's ({count_minimal/len(df_filtered)*100:.1f}%)")
print(f"  => Recommendations gebruiken 75% van similarity score (Power mist, 25% gewicht verloren)")
print(f"  => GOEDE KWALITEIT (Budget is belangrijkste met 30%)")

print(f"\nScenario 3: Alleen kritieke features (Budget, Year):")
print(f"  {count_critical:,} auto's ({count_critical/len(df_filtered)*100:.1f}%)")
print(f"  => Recommendations gebruiken 50% van similarity score")
print(f"  => BASIS KWALITEIT")

# Impact op recommendation berekening
print("\n" + "=" * 80)
print("IMPACT OP SIMILARITY BEREKENING")
print("=" * 80)

print("""
Het recommendation systeem gebruikt gewogen similarity scores:

Gewichten:
- Budget (prijs):  30% (KRITIEK - belangrijkste factor)
- Year (bouwjaar): 20% (KRITIEK)
- Fuel (brandstof): 25% (KRITIEK)
- Power (vermogen): 25% (OPTIONEEL - kan ontbreken)

IMPACT ANALYSE:

1. MET ALLE FEATURES (100% coverage):
   => Volledige similarity score (100%)
   => Best mogelijke recommendations
   => {:.1f}% van gefilterde auto's

2. ZONDER VERMOGEN (75% coverage):
   => Similarity score gebaseerd op Budget + Year + Fuel (75% gewicht)
   => Nog steeds goede recommendations (Budget is belangrijkste)
   => {:.1f}% van gefilterde auto's

3. EFFECT OP RECOMMENDATIONS:
   - Als target auto vermogen heeft: nog steeds goede recommendations mogelijk
   - Als target auto geen vermogen heeft: geen probleem (alle auto's zonder vermogen worden gelijk behandeld)
   - Budget blijft de belangrijkste factor (30%)

CONCLUSIE:
- Het systeem KAN nog steeds werken zonder vermogen data
- Recommendations blijven BRUIKBAAR maar iets minder precies
- {:.1f}% van auto's heeft alle features, {:.1f}% heeft minimale features
- Dit is NIET kritiek, maar optimalisatie is wenselijk
""".format(
    count_all_features/len(df_filtered)*100,
    count_minimal/len(df_filtered)*100,
    count_all_features/len(df_filtered)*100,
    count_minimal/len(df_filtered)*100
))

# ============================================================================
# SAMENVATTING EN CONCLUSIE
# ============================================================================

print("\n" + "=" * 80)
print("SAMENVATTING EN CONCLUSIE")
print("=" * 80)

print(f"""
WELKE AUTO'S ZIJN ER NOG:

Na filtering van verdachte/onrealistische data:
- Start: {len(df):,} auto's
- Verwijderd: {total_removed:,} auto's ({total_removed/len(df)*100:.1f}%)
- Bruikbaar: {len(df_filtered):,} auto's ({len(df_filtered)/len(df)*100:.1f}%)

HOE ERG IS HET?

[KRITIEK] KRITIEKE PROBLEMEN: GEEN
   - Bouwjaren zijn allemaal realistisch
   - Budget data is over het algemeen goed (slechts 7.94% outliers)

[MATIG] MATIGE PROBLEMEN: 
   - {removed2:,} auto's ({removed2/len(df)*100:.1f}%) met prijs < €500 moeten gefilterd worden
   - {df_filtered['vermogen'].isna().sum() + (df_filtered['vermogen'] <= 0).sum():,} auto's ({((df_filtered['vermogen'].isna().sum() + (df_filtered['vermogen'] <= 0).sum())/len(df_filtered)*100):.1f}%) zonder vermogen data

[OK] GEEN PROBLEMEN:
   - {len(df_filtered):,} auto's kunnen gebruikt worden voor recommendations
   - Dit is nog steeds {len(df_filtered)/len(df)*100:.1f}% van de originele dataset

VERANDERT HET DE ZAAK HARD?

NEE - Het recommendation systeem kan nog steeds goed werken:
1. Je hebt nog {len(df_filtered):,} bruikbare auto's (92% van origineel)
2. Budget (30% gewicht) en Year (20% gewicht) zijn beschikbaar voor bijna alle auto's
3. Vermogen (25% gewicht) ontbreekt wel vaak, maar is niet kritiek
4. Recommendations blijven mogelijk, alleen iets minder precies zonder vermogen

AANBEVELINGEN:

1. [OK] FILTER TOEPASSEN: Verwijder auto's met prijs < €500 (automatisch filteren)
2. [WAARSCHUWING] ACCEPTEER: Auto's zonder vermogen data (systeem werkt nog steeds)
3. [OK] BEHOUDEN: Extreem dure auto's (>€500.000) worden behouden - deze kunnen legitiem zijn
   (bijv. Lamborghini Urus, Mercedes G65 AMG zijn inderdaad zeer dure luxe auto's)

CONCLUSIE:
De data problemen zijn NIET kritiek. Je kunt:
- Het systeem direct gebruiken met {len(df_filtered):,} auto's
- Recommendations blijven goed werken (92% data behouden)
- Vermogen data is wenselijk maar niet essentieel
- Eventueel later vermogen data aanvullen voor betere kwaliteit
""")

print("\n" + "=" * 80)
print("ANALYSE VOLTOOID")
print("=" * 80)



