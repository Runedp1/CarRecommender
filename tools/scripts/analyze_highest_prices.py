"""
Script om de niet-dubbele auto's (unieke Brand|Model combinaties) 
met de hoogste prijzen te analyseren.
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
MAX_REALISTIC_PRICE = 500000  # Zelfde als in CarRepository.cs

print("=" * 80)
print("ANALYSE: HOOGSTE PRIJZEN VAN UNIEKE AUTO'S")
print("=" * 80)
print(f"CSV pad: {csv_path}")
print()

if not os.path.exists(csv_path):
    print(f"FOUT: CSV bestand niet gevonden: {csv_path}")
    sys.exit(1)

# Data verzameling: Key = Brand|Model, Value = eerste rij met deze combinatie
unique_cars = {}

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
        body_type_col = None
        power_col = None
        transmission_col = None
        
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
            if body_type_col is None and any(x in col_lower for x in ['type_auto', 'bodytype', 'body type', 'carrosserie', 'type']):
                body_type_col = col
            if power_col is None and any(x in col_lower for x in ['vermogen', 'power', 'horsepower', 'engines']):
                power_col = col
            if transmission_col is None and any(x in col_lower for x in ['transmission', 'transmissie']):
                transmission_col = col
        
        print(f"Gebruikte kolommen:")
        print(f"  Brand: '{brand_col}'")
        print(f"  Model: '{model_col}'")
        print(f"  Price: '{price_col}'")
        print(f"  Year: '{year_col}'")
        print(f"  Fuel: '{fuel_col}'")
        print(f"  Body Type: '{body_type_col}'")
        print(f"  Power: '{power_col}'")
        print(f"  Transmission: '{transmission_col}'")
        print()
        
        row_number = 0
        for row in reader:
            row_number += 1
            
            brand = (row.get(brand_col, '') or '').strip()
            model = (row.get(model_col, '') or '').strip()
            
            if not brand or not model:
                continue
            
            # Maak unieke key: Brand|Model (case-insensitive)
            unique_key = f"{brand.lower()}|{model.lower()}"
            
            # Bewaar eerste voorkoming van deze unieke combinatie
            if unique_key not in unique_cars:
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
                
                # Parse vermogen
                power = None
                if power_col and row.get(power_col):
                    try:
                        power_str = (row.get(power_col, '') or '').strip()
                        power_clean = ''.join(c for c in power_str if c.isdigit())
                        if power_clean:
                            power_val = int(power_clean)
                            if 20 <= power_val <= 800:
                                power = power_val
                    except:
                        pass
                
                unique_cars[unique_key] = {
                    'row': row_number,
                    'brand': brand,
                    'model': model,
                    'price': price,
                    'year': year,
                    'fuel': (row.get(fuel_col, '') or '').strip() if fuel_col else '',
                    'body_type': (row.get(body_type_col, '') or '').strip() if body_type_col else '',
                    'power': power,
                    'transmission': (row.get(transmission_col, '') or '').strip() if transmission_col else ''
                }
        
        print("=" * 80)
        print(f"UNIEKE AUTO'S (na verwijdering duplicaten): {len(unique_cars)}")
        print("=" * 80)
        print()
        
        # Filter auto's met geldige prijzen
        cars_with_prices = []
        for key, car in unique_cars.items():
            if car['price'] is not None and MIN_REALISTIC_PRICE <= car['price'] <= MAX_REALISTIC_PRICE:
                cars_with_prices.append(car)
        
        print(f"Auto's met geldige prijzen (EUR {MIN_REALISTIC_PRICE} - EUR {MAX_REALISTIC_PRICE}): {len(cars_with_prices)}")
        print()
        
        # Sorteer op prijs (hoogste eerst)
        cars_with_prices.sort(key=lambda x: x['price'], reverse=True)
        
        # Toon top 50 hoogste prijzen
        print("=" * 80)
        print("TOP 50 HOOGSTE PRIJZEN")
        print("=" * 80)
        print()
        print(f"{'#':<4} {'Rij':<6} {'Merk':<20} {'Model':<25} {'Prijs (EUR)':<15} {'Jaar':<6} {'Vermogen':<10} {'Brandstof':<12} {'Body Type':<15}")
        print("-" * 120)
        
        for i, car in enumerate(cars_with_prices[:50], 1):
            price_str = f"{car['price']:>14,.2f}" if car['price'] else "N/A"
            year_str = f"{car['year']}" if car['year'] else "N/A"
            power_str = f"{car['power']} KW" if car['power'] else "N/A"
            fuel_str = car['fuel'][:11] if car['fuel'] else "N/A"
            body_str = car['body_type'][:14] if car['body_type'] else "N/A"
            
            print(f"{i:<4} {car['row']:<6} {car['brand']:<20} {car['model']:<25} {price_str:<15} {year_str:<6} {power_str:<10} {fuel_str:<12} {body_str:<15}")
        
        print()
        print("=" * 80)
        print("STATISTIEKEN")
        print("=" * 80)
        print()
        
        if cars_with_prices:
            prices = [car['price'] for car in cars_with_prices]
            prices.sort()
            
            print(f"Totaal unieke auto's met geldige prijzen: {len(cars_with_prices)}")
            print(f"Minimum prijs: EUR {min(prices):,.2f}")
            print(f"Maximum prijs: EUR {max(prices):,.2f}")
            print(f"Gemiddelde prijs: EUR {sum(prices)/len(prices):,.2f}")
            print(f"Mediaan prijs: EUR {prices[len(prices)//2]:,.2f}")
            print()
            print("Percentielen:")
            print(f"  95e percentiel: EUR {prices[int(len(prices) * 0.95)]:,.2f}")
            print(f"  90e percentiel: EUR {prices[int(len(prices) * 0.90)]:,.2f}")
            print(f"  75e percentiel: EUR {prices[int(len(prices) * 0.75)]:,.2f}")
            print(f"  50e percentiel (mediaan): EUR {prices[len(prices)//2]:,.2f}")
            print(f"  25e percentiel: EUR {prices[int(len(prices) * 0.25)]:,.2f}")
            print()
            
            # Analyseer prijsklassen
            price_classes = {
                'EUR 100.000+': sum(1 for p in prices if p >= 100000),
                'EUR 80.000 - EUR 100.000': sum(1 for p in prices if 80000 <= p < 100000),
                'EUR 60.000 - EUR 80.000': sum(1 for p in prices if 60000 <= p < 80000),
                'EUR 50.000 - EUR 60.000': sum(1 for p in prices if 50000 <= p < 60000),
                'EUR 40.000 - EUR 50.000': sum(1 for p in prices if 40000 <= p < 50000),
                'EUR 30.000 - EUR 40.000': sum(1 for p in prices if 30000 <= p < 40000),
                'EUR 20.000 - EUR 30.000': sum(1 for p in prices if 20000 <= p < 30000),
                'EUR 10.000 - EUR 20.000': sum(1 for p in prices if 10000 <= p < 20000),
                'EUR 5.000 - EUR 10.000': sum(1 for p in prices if 5000 <= p < 10000),
                '< EUR 5.000': sum(1 for p in prices if p < 5000),
            }
            
            print("Verdeling over prijsklassen:")
            for class_name, count in price_classes.items():
                percentage = (count / len(prices)) * 100
                print(f"  {class_name:30s}: {count:4d} auto's ({percentage:5.1f}%)")
            print()
            
            # Auto's boven MAX_REALISTIC_PRICE
            above_max = [car for car in unique_cars.values() if car['price'] and car['price'] > MAX_REALISTIC_PRICE]
            if above_max:
                print(f"[!] WAARSCHUWING: {len(above_max)} auto's hebben prijzen boven EUR {MAX_REALISTIC_PRICE}")
                print("    Deze worden uitgefilterd door de huidige IsCarRealistic check.")
                print("    Top 10:")
                above_max.sort(key=lambda x: x['price'], reverse=True)
                for car in above_max[:10]:
                    print(f"      - {car['brand']} {car['model']}: EUR {car['price']:,.2f}")
                print()
        
        # Analyseer merken met hoogste prijzen
        print("=" * 80)
        print("TOP 20 MERKEN MET HOOGSTE GEMIDDELDE PRIJZEN")
        print("=" * 80)
        print()
        
        brand_prices = defaultdict(list)
        for car in cars_with_prices:
            brand_prices[car['brand']].append(car['price'])
        
        brand_avg_prices = []
        for brand, prices_list in brand_prices.items():
            if len(prices_list) > 0:
                brand_avg_prices.append((brand, sum(prices_list) / len(prices_list), len(prices_list)))
        
        brand_avg_prices.sort(key=lambda x: x[1], reverse=True)
        
        print(f"{'Merk':<25} {'Gem. Prijs (EUR)':<20} {'Aantal Modellen':<15}")
        print("-" * 60)
        for brand, avg_price, count in brand_avg_prices[:20]:
            print(f"{brand:<25} {avg_price:>18,.2f} {count:>15}")
        
        print()
        print("=" * 80)
        
except Exception as e:
    print(f"FOUT bij het analyseren: {e}")
    import traceback
    traceback.print_exc()
    sys.exit(1)

