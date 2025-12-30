"""
Script om te analyseren wat er gebeurt als we voor dubbele auto's (zelfde Brand|Model)
de auto met de HOOGSTE prijs behouden en de anderen weggooien.
Dan analyseren hoeveel onrealistisch lage prijzen er nog over zijn.
"""
import csv
import os
import sys
from collections import defaultdict

# Bepaal data directory
script_dir = os.path.dirname(os.path.abspath(__file__))
project_root = os.path.dirname(os.path.dirname(script_dir))
csv_path = os.path.join(project_root, 'backend', 'data', 'df_master_v8_def.csv')

# Prijsdrempels
MIN_REALISTIC_PRICE = 300
SUSPICIOUS_PRICE_THRESHOLD = 1000
VERY_SUSPICIOUS_PRICE_THRESHOLD = 500

print("=" * 80)
print("ANALYSE: BEHOUD HOOGSTE PRIJS PER BRAND|MODEL COMBINATIE")
print("=" * 80)
print(f"CSV pad: {csv_path}")
print()
print("Strategie: Voor elke Brand|Model combinatie behouden we alleen de auto")
print("met de HOOGSTE prijs. Alle anderen worden weggegooid.")
print()

if not os.path.exists(csv_path):
    print(f"FOUT: CSV bestand niet gevonden: {csv_path}")
    sys.exit(1)

# Data verzameling: Key = Brand|Model, Value = lijst van alle auto's met deze combinatie
cars_by_unique_key = defaultdict(list)

try:
    with open(csv_path, 'r', encoding='utf-8') as f:
        reader = csv.DictReader(f)
        fieldnames = reader.fieldnames
        
        # Zoek kolommen
        brand_col = None
        model_col = None
        price_col = None
        year_col = None
        fuel_col = None
        
        for col in fieldnames:
            col_lower = col.lower()
            if brand_col is None and any(x in col_lower for x in ['merk', 'brand', 'company names']):
                brand_col = col
            if model_col is None and any(x in col_lower for x in ['model', 'cars names']):
                model_col = col
            if price_col is None and any(x in col_lower for x in ['budget', 'prijs', 'price', 'cars prices']):
                price_col = col
            if year_col is None and any(x in col_lower for x in ['bouwjaar', 'year', 'jaar']):
                year_col = col
            if fuel_col is None and any(x in col_lower for x in ['brandstof', 'fuel', 'fuel types']):
                fuel_col = col
        
        print(f"Gebruikte kolommen:")
        print(f"  Brand: '{brand_col}'")
        print(f"  Model: '{model_col}'")
        print(f"  Price: '{price_col}'")
        print()
        
        row_number = 0
        total_rows = 0
        
        for row in reader:
            row_number += 1
            total_rows += 1
            
            brand = (row.get(brand_col, '') or '').strip()
            model = (row.get(model_col, '') or '').strip()
            
            if not brand or not model:
                continue
            
            # Maak unieke key: Brand|Model (case-insensitive)
            unique_key = f"{brand.lower()}|{model.lower()}"
            
            # Parse prijs
            price_str = (row.get(price_col, '') or '').strip()
            price = None
            
            try:
                price_clean = ''.join(c for c in price_str if c.isdigit() or c in '.,')
                price_clean = price_clean.replace(',', '.')
                if price_clean:
                    price = float(price_clean)
            except:
                pass
            
            # Parse jaar
            year = None
            if year_col and row.get(year_col):
                try:
                    year_str = (row.get(year_col, '') or '').strip()
                    for i in range(len(year_str) - 3):
                        if year_str[i:i+4].isdigit():
                            year_val = int(year_str[i:i+4])
                            if 1990 <= year_val <= 2025:
                                year = year_val
                                break
                except:
                    pass
            
            fuel = (row.get(fuel_col, '') or '').strip() if fuel_col else ''
            
            car_info = {
                'row': row_number,
                'brand': brand,
                'model': model,
                'price': price,
                'year': year,
                'fuel': fuel
            }
            
            cars_by_unique_key[unique_key].append(car_info)
        
        print(f"Totaal aantal rijen verwerkt: {total_rows}")
        print(f"Unieke Brand|Model combinaties: {len(cars_by_unique_key)}")
        print()
        
        # Voor elke unieke combinatie, behoud alleen de auto met de hoogste prijs
        kept_cars = []
        discarded_count = 0
        
        for unique_key, cars_list in cars_by_unique_key.items():
            # Filter auto's met geldige prijzen
            cars_with_prices = [c for c in cars_list if c['price'] is not None]
            
            if not cars_with_prices:
                # Geen geldige prijzen, behoud eerste
                if cars_list:
                    kept_cars.append(cars_list[0])
                    discarded_count += len(cars_list) - 1
            else:
                # Behoud auto met hoogste prijs
                car_with_highest_price = max(cars_with_prices, key=lambda x: x['price'])
                kept_cars.append(car_with_highest_price)
                discarded_count += len(cars_list) - 1
        
        print("=" * 80)
        print("RESULTATEN NA 'BEHOUD HOOGSTE PRIJS' STRATEGIE")
        print("=" * 80)
        print()
        print(f"Auto's behouden: {len(kept_cars)}")
        print(f"Auto's weggegooid: {discarded_count}")
        print(f"Totaal origineel: {total_rows}")
        print()
        
        # Analyseer onrealistisch lage prijzen
        very_suspicious = []  # < EUR 500
        suspicious = []  # EUR 500 - EUR 1000
        below_minimum = []  # < EUR 300
        low_but_ok = []  # EUR 1000 - EUR 3000
        
        for car in kept_cars:
            if car['price'] is None:
                continue
            
            if car['price'] < VERY_SUSPICIOUS_PRICE_THRESHOLD:
                very_suspicious.append(car)
                if car['price'] < MIN_REALISTIC_PRICE:
                    below_minimum.append(car)
            elif car['price'] < SUSPICIOUS_PRICE_THRESHOLD:
                suspicious.append(car)
            elif car['price'] < MIN_REALISTIC_PRICE:
                below_minimum.append(car)
            elif car['price'] < 3000:
                low_but_ok.append(car)
        
        print("=" * 80)
        print("ANALYSE ONREALISTISCH LAGE PRIJZEN")
        print("=" * 80)
        print()
        
        # Onder minimum realistische prijs
        print(f"1. AUTO'S ONDER MINIMUM REALISTISCHE PRIJS (EUR {MIN_REALISTIC_PRICE}):")
        print(f"   Aantal: {len(below_minimum)}")
        print(f"   Percentage: {len(below_minimum)/len(kept_cars)*100:.1f}% van behouden auto's")
        if below_minimum:
            print(f"   Voorbeelden:")
            for car in sorted(below_minimum, key=lambda x: x['price'])[:10]:
                year_str = f"{car['year']}" if car['year'] else "onbekend"
                fuel_str = f" ({car['fuel']})" if car['fuel'] else ""
                print(f"     - Rij {car['row']:5d}: {car['brand']:20s} {car['model']:20s} | "
                      f"EUR {car['price']:>10,.2f} | Jaar: {year_str:4s}{fuel_str}")
            print()
        
        # Zeer verdacht (< EUR 500)
        print(f"2. ZEER VERDACHTE PRIJZEN (< EUR {VERY_SUSPICIOUS_PRICE_THRESHOLD}):")
        print(f"   Aantal: {len(very_suspicious)}")
        print(f"   Percentage: {len(very_suspicious)/len(kept_cars)*100:.1f}% van behouden auto's")
        if very_suspicious:
            print(f"   Top 20 laagste prijzen:")
            for car in sorted(very_suspicious, key=lambda x: x['price'])[:20]:
                year_str = f"{car['year']}" if car['year'] else "onbekend"
                fuel_str = f" ({car['fuel']})" if car['fuel'] else ""
                print(f"     - Rij {car['row']:5d}: {car['brand']:20s} {car['model']:20s} | "
                      f"EUR {car['price']:>10,.2f} | Jaar: {year_str:4s}{fuel_str}")
            print()
        
        # Verdacht (EUR 500 - EUR 1000)
        print(f"3. VERDACHTE PRIJZEN (EUR {VERY_SUSPICIOUS_PRICE_THRESHOLD} - EUR {SUSPICIOUS_PRICE_THRESHOLD}):")
        print(f"   Aantal: {len(suspicious)}")
        print(f"   Percentage: {len(suspicious)/len(kept_cars)*100:.1f}% van behouden auto's")
        if suspicious:
            print(f"   Voorbeelden:")
            for car in sorted(suspicious, key=lambda x: x['price'])[:15]:
                year_str = f"{car['year']}" if car['year'] else "onbekend"
                fuel_str = f" ({car['fuel']})" if car['fuel'] else ""
                print(f"     - Rij {car['row']:5d}: {car['brand']:20s} {car['model']:20s} | "
                      f"EUR {car['price']:>10,.2f} | Jaar: {year_str:4s}{fuel_str}")
            print()
        
        # Statistieken
        cars_with_prices = [c for c in kept_cars if c['price'] is not None]
        if cars_with_prices:
            prices = [c['price'] for c in cars_with_prices]
            prices.sort()
            
            print("=" * 80)
            print("PRIJS STATISTIEKEN")
            print("=" * 80)
            print()
            print(f"Totaal auto's met prijzen: {len(cars_with_prices)}")
            print(f"Minimum prijs: EUR {min(prices):,.2f}")
            print(f"Maximum prijs: EUR {max(prices):,.2f}")
            print(f"Gemiddelde prijs: EUR {sum(prices)/len(prices):,.2f}")
            print(f"Mediaan prijs: EUR {prices[len(prices)//2]:,.2f}")
            print()
            print("Percentielen:")
            print(f"  5e percentiel: EUR {prices[int(len(prices) * 0.05)]:,.2f}")
            print(f"  10e percentiel: EUR {prices[int(len(prices) * 0.10)]:,.2f}")
            print(f"  25e percentiel: EUR {prices[int(len(prices) * 0.25)]:,.2f}")
            print(f"  50e percentiel (mediaan): EUR {prices[len(prices)//2]:,.2f}")
            print(f"  75e percentiel: EUR {prices[int(len(prices) * 0.75)]:,.2f}")
            print(f"  90e percentiel: EUR {prices[int(len(prices) * 0.90)]:,.2f}")
            print(f"  95e percentiel: EUR {prices[int(len(prices) * 0.95)]:,.2f}")
            print()
            
            # Tel auto's onder EUR 10.000
            cars_under_10k = sum(1 for p in prices if p < 10000)
            percentage_under_10k = (cars_under_10k / len(prices)) * 100
            
            print("=" * 80)
            print("PRIJSKLASSE VERDELING")
            print("=" * 80)
            print()
            print(f"Auto's onder EUR 10.000: {cars_under_10k} ({percentage_under_10k:.1f}%)")
            print(f"Auto's EUR 10.000 of hoger: {len(prices) - cars_under_10k} ({100 - percentage_under_10k:.1f}%)")
            print()
            
            # Gedetailleerde prijsverdeling
            price_distribution = {
                '< EUR 1.000': sum(1 for p in prices if p < 1000),
                'EUR 1.000 - EUR 5.000': sum(1 for p in prices if 1000 <= p < 5000),
                'EUR 5.000 - EUR 10.000': sum(1 for p in prices if 5000 <= p < 10000),
                'EUR 10.000 - EUR 20.000': sum(1 for p in prices if 10000 <= p < 20000),
                'EUR 20.000 - EUR 30.000': sum(1 for p in prices if 20000 <= p < 30000),
                'EUR 30.000 - EUR 50.000': sum(1 for p in prices if 30000 <= p < 50000),
                'EUR 50.000+': sum(1 for p in prices if p >= 50000),
            }
            
            print("Gedetailleerde verdeling:")
            for range_name, count in price_distribution.items():
                percentage = (count / len(prices)) * 100
                print(f"  {range_name:25s}: {count:4d} auto's ({percentage:5.1f}%)")
            print()
            
            # Vergelijk met originele dataset
            print("=" * 80)
            print("VERGELIJKING")
            print("=" * 80)
            print()
            print("Met 'behoud eerste auto' strategie (huidige implementatie):")
            print("  - 661 unieke auto's")
            print("  - 332 auto's onder EUR 300 (50.2%)")
            print("  - 831 auto's onder EUR 500 (125.7% - dit klopt niet, waarschijnlijk niet unieke dataset)")
            print()
            print("Met 'behoud hoogste prijs' strategie (nieuwe aanpak):")
            print(f"  - {len(kept_cars)} unieke auto's")
            print(f"  - {len(below_minimum)} auto's onder EUR 300 ({len(below_minimum)/len(kept_cars)*100:.1f}%)")
            print(f"  - {len(very_suspicious)} auto's onder EUR 500 ({len(very_suspicious)/len(kept_cars)*100:.1f}%)")
            print(f"  - {len(suspicious)} auto's tussen EUR 500-1000 ({len(suspicious)/len(kept_cars)*100:.1f}%)")
            print(f"  - {cars_under_10k} auto's onder EUR 10.000 ({percentage_under_10k:.1f}%)")
            print()
            
            improvement = (332 - len(below_minimum)) / 332 * 100 if 332 > 0 else 0
            print(f"VERBETERING:")
            print(f"  - {332 - len(below_minimum)} minder auto's onder EUR 300")
            print(f"  - {improvement:.1f}% reductie in onrealistisch lage prijzen")
        
        print()
        print("=" * 80)
        
except Exception as e:
    print(f"FOUT bij het analyseren: {e}")
    import traceback
    traceback.print_exc()
    sys.exit(1)

