"""
Script om de diversiteit van de dataset te analyseren na verwijdering van duplicaten.
Analyseert: merken, brandstoffen, body types, prijsbereik, etc.
"""
import csv
import os
import sys
from collections import Counter, defaultdict

# Bepaal data directory
script_dir = os.path.dirname(os.path.abspath(__file__))
project_root = os.path.dirname(os.path.dirname(script_dir))
csv_path = os.path.join(project_root, 'backend', 'data', 'df_master_v8_def.csv')

print("=" * 80)
print("DATASET DIVERSITEIT ANALYSE")
print("=" * 80)
print(f"CSV pad: {csv_path}")
print()

if not os.path.exists(csv_path):
    print(f"FOUT: CSV bestand niet gevonden: {csv_path}")
    sys.exit(1)

# Data verzameling
unique_cars = {}  # Key: Brand|Model, Value: eerste rij data (als dictionary)
total_rows = 0

try:
    with open(csv_path, 'r', encoding='utf-8') as f:
        reader = csv.DictReader(f)
        fieldnames = reader.fieldnames
        
        # Zoek kolommen (flexibel zoals in C# code)
        brand_col = None
        model_col = None
        fuel_col = None
        body_type_col = None
        price_col = None
        year_col = None
        power_col = None
        transmission_col = None
        
        for col in fieldnames:
            col_lower = col.lower()
            if brand_col is None and any(x in col_lower for x in ['merk', 'brand', 'company names']):
                brand_col = col
            if model_col is None and any(x in col_lower for x in ['model', 'cars names']):
                model_col = col
            if fuel_col is None and any(x in col_lower for x in ['brandstof', 'fuel', 'fuel types']):
                fuel_col = col
            if body_type_col is None and any(x in col_lower for x in ['type_auto', 'bodytype', 'body type', 'carrosserie', 'type']):
                body_type_col = col
            if price_col is None and any(x in col_lower for x in ['budget', 'prijs', 'price', 'cars prices']):
                price_col = col
            if year_col is None and any(x in col_lower for x in ['bouwjaar', 'year', 'jaar']):
                year_col = col
            if power_col is None and any(x in col_lower for x in ['vermogen', 'power', 'horsepower', 'engines']):
                power_col = col
            if transmission_col is None and any(x in col_lower for x in ['transmission', 'transmissie']):
                transmission_col = col
        
        print(f"Gebruikte kolommen:")
        print(f"  Brand: '{brand_col}'")
        print(f"  Model: '{model_col}'")
        print(f"  Fuel: '{fuel_col}'")
        print(f"  Body Type: '{body_type_col}'")
        print(f"  Price: '{price_col}'")
        print(f"  Year: '{year_col}'")
        print(f"  Power: '{power_col}'")
        print(f"  Transmission: '{transmission_col}'")
        print()
        
        # Verwerk alle rijen
        for row in reader:
            total_rows += 1
            
            brand = (row.get(brand_col, '') or '').strip()
            model = (row.get(model_col, '') or '').strip()
            
            # Alleen toevoegen als beide niet leeg zijn
            if brand and model:
                unique_key = f"{brand.lower()}|{model.lower()}"
                
                # Bewaar eerste voorkoming van deze unieke combinatie
                if unique_key not in unique_cars:
                    unique_cars[unique_key] = row
        
        print("=" * 80)
        print(f"UNIEKE AUTO'S (na verwijdering duplicaten): {len(unique_cars)}")
        print("=" * 80)
        print()
        
        # Analyseer diversiteit
        brands = []
        fuels = []
        body_types = []
        transmissions = []
        prices = []
        years = []
        powers = []
        
        for unique_key, row in unique_cars.items():
            # Brand
            brand_val = (row.get(brand_col, '') or '').strip()
            if brand_val:
                brands.append(brand_val)
            
            # Fuel
            if fuel_col and row.get(fuel_col):
                fuel_val = (row.get(fuel_col, '') or '').strip()
                if fuel_val:
                    fuels.append(fuel_val)
            
            # Body Type
            if body_type_col and row.get(body_type_col):
                body_val = (row.get(body_type_col, '') or '').strip()
                if body_val:
                    body_types.append(body_val)
            
            # Transmission
            if transmission_col and row.get(transmission_col):
                trans_val = (row.get(transmission_col, '') or '').strip()
                if trans_val:
                    transmissions.append(trans_val)
            
            # Price
            if price_col and row.get(price_col):
                try:
                    price_str = (row.get(price_col, '') or '').strip()
                    # Verwijder niet-numerieke karakters (behalve punt en komma)
                    price_clean = ''.join(c for c in price_str if c.isdigit() or c in '.,')
                    price_clean = price_clean.replace(',', '.')
                    if price_clean:
                        price = float(price_clean)
                        if 300 <= price <= 500000:  # Realistische prijzen
                            prices.append(price)
                except:
                    pass
            
            # Year
            if year_col and row.get(year_col):
                try:
                    year_str = (row.get(year_col, '') or '').strip()
                    # Extract jaar (eerste 4 cijfers)
                    year_match = None
                    for i in range(len(year_str) - 3):
                        if year_str[i:i+4].isdigit():
                            year_match = int(year_str[i:i+4])
                            if 1990 <= year_match <= 2025:
                                years.append(year_match)
                                break
                except:
                    pass
            
            # Power
            if power_col and row.get(power_col):
                try:
                    power_str = (row.get(power_col, '') or '').strip()
                    # Haal alleen cijfers eruit
                    power_clean = ''.join(c for c in power_str if c.isdigit())
                    if power_clean:
                        power = int(power_clean)
                        if 20 <= power <= 800:  # Realistische vermogens
                            powers.append(power)
                except:
                    pass
        
        # Print analyse resultaten
        print("DIVERSITEIT ANALYSE")
        print("-" * 80)
        print()
        
        # Merken
        brand_counter = Counter(brands)
        print(f"1. MERKEN (Brands):")
        print(f"   Totaal unieke merken: {len(brand_counter)}")
        print(f"   Top 10 meest voorkomende merken:")
        for brand, count in brand_counter.most_common(10):
            print(f"     - {brand}: {count} modellen")
        print()
        
        # Brandstoffen
        fuel_counter = Counter(fuels)
        print(f"2. BRANDSTOFFEN (Fuel Types):")
        print(f"   Totaal unieke brandstoffen: {len(fuel_counter)}")
        print(f"   Verdeling:")
        for fuel, count in fuel_counter.most_common():
            percentage = (count / len(fuels)) * 100 if fuels else 0
            print(f"     - {fuel}: {count} auto's ({percentage:.1f}%)")
        print()
        
        # Body Types
        body_counter = Counter(body_types)
        print(f"3. CARROSSERIEËN (Body Types):")
        print(f"   Totaal unieke body types: {len(body_counter)}")
        print(f"   Verdeling:")
        for body, count in body_counter.most_common():
            percentage = (count / len(body_types)) * 100 if body_types else 0
            print(f"     - {body}: {count} auto's ({percentage:.1f}%)")
        print()
        
        # Transmissie
        trans_counter = Counter(transmissions)
        print(f"4. TRANSMISSIE (Transmission):")
        print(f"   Totaal unieke transmissies: {len(trans_counter)}")
        print(f"   Verdeling:")
        for trans, count in trans_counter.most_common():
            percentage = (count / len(transmissions)) * 100 if transmissions else 0
            print(f"     - {trans}: {count} auto's ({percentage:.1f}%)")
        print()
        
        # Prijsbereik
        if prices:
            prices.sort()
            print(f"5. PRIJSBEREIK (Price Range):")
            print(f"   Totaal auto's met geldige prijzen: {len(prices)}")
            print(f"   Minimum prijs: €{min(prices):,.2f}")
            print(f"   Maximum prijs: €{max(prices):,.2f}")
            print(f"   Gemiddelde prijs: €{sum(prices)/len(prices):,.2f}")
            print(f"   Mediaan prijs: €{prices[len(prices)//2]:,.2f}")
            
            # Prijsklassen
            price_ranges = {
                '€0 - €10.000': sum(1 for p in prices if p < 10000),
                '€10.000 - €20.000': sum(1 for p in prices if 10000 <= p < 20000),
                '€20.000 - €30.000': sum(1 for p in prices if 20000 <= p < 30000),
                '€30.000 - €50.000': sum(1 for p in prices if 30000 <= p < 50000),
                '€50.000 - €100.000': sum(1 for p in prices if 50000 <= p < 100000),
                '€100.000+': sum(1 for p in prices if p >= 100000),
            }
            print(f"   Verdeling over prijsklassen:")
            for range_name, count in price_ranges.items():
                percentage = (count / len(prices)) * 100
                print(f"     - {range_name}: {count} auto's ({percentage:.1f}%)")
            print()
        
        # Bouwjaar
        if years:
            years.sort()
            print(f"6. BOUWJAAR (Year Range):")
            print(f"   Totaal auto's met geldige bouwjaren: {len(years)}")
            print(f"   Oudste auto: {min(years)}")
            print(f"   Nieuwste auto: {max(years)}")
            print(f"   Gemiddeld bouwjaar: {sum(years)/len(years):.0f}")
            print()
        
        # Vermogen
        if powers:
            powers.sort()
            print(f"7. VERMOGEN (Power Range):")
            print(f"   Totaal auto's met geldige vermogens: {len(powers)}")
            print(f"   Minimum vermogen: {min(powers)} KW")
            print(f"   Maximum vermogen: {max(powers)} KW")
            print(f"   Gemiddeld vermogen: {sum(powers)/len(powers):.0f} KW")
            print()
        
        # CONCLUSIE
        print("=" * 80)
        print("CONCLUSIE")
        print("=" * 80)
        print()
        
        diversity_score = 0
        max_score = 7
        
        # Check diversiteit criteria
        checks = []
        
        # 1. Aantal merken
        if len(brand_counter) >= 20:
            checks.append(f"[OK] Goede merkendiversiteit ({len(brand_counter)} merken)")
            diversity_score += 1
        elif len(brand_counter) >= 10:
            checks.append(f"[!] Redelijke merkendiversiteit ({len(brand_counter)} merken)")
            diversity_score += 0.5
        else:
            checks.append(f"[X] Beperkte merkendiversiteit ({len(brand_counter)} merken)")
        
        # 2. Brandstoffen
        if len(fuel_counter) >= 4:
            checks.append(f"[OK] Goede brandstofdiversiteit ({len(fuel_counter)} types)")
            diversity_score += 1
        elif len(fuel_counter) >= 2:
            checks.append(f"[!] Redelijke brandstofdiversiteit ({len(fuel_counter)} types)")
            diversity_score += 0.5
        else:
            checks.append(f"[X] Beperkte brandstofdiversiteit ({len(fuel_counter)} types)")
        
        # 3. Body types
        if len(body_counter) >= 5:
            checks.append(f"[OK] Goede body type diversiteit ({len(body_counter)} types)")
            diversity_score += 1
        elif len(body_counter) >= 3:
            checks.append(f"[!] Redelijke body type diversiteit ({len(body_counter)} types)")
            diversity_score += 0.5
        else:
            checks.append(f"[X] Beperkte body type diversiteit ({len(body_counter)} types)")
        
        # 4. Prijsbereik
        if prices and (max(prices) - min(prices)) > 40000:
            checks.append(f"[OK] Goed prijsbereik (EUR {min(prices):,.0f} - EUR {max(prices):,.0f})")
            diversity_score += 1
        elif prices and (max(prices) - min(prices)) > 20000:
            checks.append(f"[!] Redelijk prijsbereik (EUR {min(prices):,.0f} - EUR {max(prices):,.0f})")
            diversity_score += 0.5
        else:
            checks.append(f"[X] Beperkt prijsbereik")
        
        # 5. Transmissie
        if len(trans_counter) >= 2:
            checks.append(f"[OK] Goede transmissiediversiteit ({len(trans_counter)} types)")
            diversity_score += 1
        else:
            checks.append(f"[!] Beperkte transmissiediversiteit ({len(trans_counter)} types)")
            diversity_score += 0.5
        
        # 6. Totale dataset grootte
        if len(unique_cars) >= 500:
            checks.append(f"[OK] Voldoende auto's voor recommendations ({len(unique_cars)} auto's)")
            diversity_score += 1
        elif len(unique_cars) >= 200:
            checks.append(f"[!] Redelijk aantal auto's ({len(unique_cars)} auto's)")
            diversity_score += 0.5
        else:
            checks.append(f"[X] Beperkt aantal auto's ({len(unique_cars)} auto's)")
        
        # 7. Prijsverdeling
        if prices:
            mid_range_count = sum(1 for p in prices if 15000 <= p <= 40000)
            mid_percentage = (mid_range_count / len(prices)) * 100
            if 30 <= mid_percentage <= 70:
                checks.append(f"[OK] Goede prijsverdeling ({mid_percentage:.1f}% in middenklasse)")
                diversity_score += 1
            else:
                checks.append(f"[!] Onevenwichtige prijsverdeling ({mid_percentage:.1f}% in middenklasse)")
                diversity_score += 0.5
        
        for check in checks:
            print(check)
        
        print()
        print(f"DIVERSITEITSScore: {diversity_score:.1f}/{max_score} ({diversity_score/max_score*100:.1f}%)")
        print()
        
        if diversity_score >= 5.5:
            print("[GOED] CONCLUSIE: De dataset heeft GOEDE diversiteit voor een recommendation engine!")
            print("        De engine kan waarschijnlijk goede recommendations geven voor verschillende")
            print("        gebruikersvoorkeuren en filters.")
        elif diversity_score >= 4:
            print("[REDELIJK] CONCLUSIE: De dataset heeft REDELIJKE diversiteit.")
            print("          De engine zal werken, maar sommige specifieke filters kunnen tot")
            print("          beperkte resultaten leiden.")
        else:
            print("[BEPERKT] CONCLUSIE: De dataset heeft BEPERKTE diversiteit.")
            print("          Overweeg om meer data toe te voegen voor betere recommendations.")
        
        print()
        print("=" * 80)
        
except Exception as e:
    print(f"FOUT bij het analyseren: {e}")
    import traceback
    traceback.print_exc()
    sys.exit(1)

