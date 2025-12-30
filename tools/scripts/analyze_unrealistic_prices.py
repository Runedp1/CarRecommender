"""
Script om auto's met onrealistisch lage prijzen te identificeren.
Helpt bij het vinden van data kwaliteitsproblemen.
"""
import csv
import os
import sys
from collections import Counter

# Bepaal data directory
script_dir = os.path.dirname(os.path.abspath(__file__))
project_root = os.path.dirname(os.path.dirname(script_dir))
csv_path = os.path.join(project_root, 'backend', 'data', 'df_master_v8_def.csv')

# Prijsdrempels (in euro's)
MIN_REALISTIC_PRICE = 300  # Zelfde als in CarRepository.cs
SUSPICIOUS_PRICE_THRESHOLD = 1000  # Onder deze prijs is verdacht
VERY_SUSPICIOUS_PRICE_THRESHOLD = 500  # Onder deze prijs is zeer verdacht

print("=" * 80)
print("ANALYSE: ONREALISTISCH LAGE PRIJZEN")
print("=" * 80)
print(f"CSV pad: {csv_path}")
print()
print(f"Prijsdrempels:")
print(f"  - Minimum realistische prijs: EUR {MIN_REALISTIC_PRICE}")
print(f"  - Verdachte prijs (te laag): < EUR {SUSPICIOUS_PRICE_THRESHOLD}")
print(f"  - Zeer verdachte prijs: < EUR {VERY_SUSPICIOUS_PRICE_THRESHOLD}")
print()

if not os.path.exists(csv_path):
    print(f"FOUT: CSV bestand niet gevonden: {csv_path}")
    sys.exit(1)

# Data verzameling
cars_with_prices = []
very_suspicious = []  # < EUR 500
suspicious = []  # EUR 500 - EUR 1000
low_but_ok = []  # EUR 1000 - EUR 300
below_minimum = []  # < EUR 300

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
        print(f"  Year: '{year_col}'")
        print(f"  Fuel: '{fuel_col}'")
        print()
        
        row_number = 0
        for row in reader:
            row_number += 1
            
            brand = (row.get(brand_col, '') or '').strip()
            model = (row.get(model_col, '') or '').strip()
            
            if not brand or not model:
                continue
            
            # Parse prijs
            price_str = (row.get(price_col, '') or '').strip()
            price = None
            
            try:
                # Verwijder niet-numerieke karakters (behalve punt en komma)
                price_clean = ''.join(c for c in price_str if c.isdigit() or c in '.,')
                price_clean = price_clean.replace(',', '.')
                if price_clean:
                    price = float(price_clean)
            except:
                continue
            
            if price is None:
                continue
            
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
            
            cars_with_prices.append(car_info)
            
            # Categoriseer op prijs
            if price < VERY_SUSPICIOUS_PRICE_THRESHOLD:
                very_suspicious.append(car_info)
                if price < MIN_REALISTIC_PRICE:
                    below_minimum.append(car_info)
            elif price < SUSPICIOUS_PRICE_THRESHOLD:
                suspicious.append(car_info)
            elif price < MIN_REALISTIC_PRICE:
                below_minimum.append(car_info)
            elif price < 3000:
                low_but_ok.append(car_info)
        
        print("=" * 80)
        print("RESULTATEN")
        print("=" * 80)
        print()
        
        print(f"Totaal auto's met geldige prijzen: {len(cars_with_prices)}")
        print()
        
        # Onder minimum realistische prijs
        print(f"1. AUTO'S ONDER MINIMUM REALISTISCHE PRIJS (EUR {MIN_REALISTIC_PRICE}):")
        print(f"   Aantal: {len(below_minimum)}")
        if below_minimum:
            print(f"   Dit zijn auto's die door de huidige filter worden uitgefilterd.")
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
        if very_suspicious:
            print(f"   Deze prijzen zijn extreem laag en waarschijnlijk data fouten.")
            print(f"   Top 20 laagste prijzen:")
            for car in sorted(very_suspicious, key=lambda x: x['price'])[:20]:
                year_str = f"{car['year']}" if car['year'] else "onbekend"
                fuel_str = f" ({car['fuel']})" if car['fuel'] else ""
                print(f"     - Rij {car['row']:5d}: {car['brand']:20s} {car['model']:20s} | "
                      f"EUR {car['price']:>10,.2f} | Jaar: {year_str:4s}{fuel_str}")
            
            # Analyseer per merk
            brand_counter = Counter(c['brand'] for c in very_suspicious)
            print(f"\n   Verdeling per merk (top 10):")
            for brand, count in brand_counter.most_common(10):
                print(f"     - {brand}: {count} auto's")
            print()
        
        # Verdacht (EUR 500 - EUR 1000)
        print(f"3. VERDACHTE PRIJZEN (EUR {VERY_SUSPICIOUS_PRICE_THRESHOLD} - EUR {SUSPICIOUS_PRICE_THRESHOLD}):")
        print(f"   Aantal: {len(suspicious)}")
        if suspicious:
            print(f"   Deze prijzen zijn laag maar mogelijk nog realistisch (oude auto's, schade, etc.).")
            print(f"   Voorbeelden:")
            for car in sorted(suspicious, key=lambda x: x['price'])[:15]:
                year_str = f"{car['year']}" if car['year'] else "onbekend"
                fuel_str = f" ({car['fuel']})" if car['fuel'] else ""
                print(f"     - Rij {car['row']:5d}: {car['brand']:20s} {car['model']:20s} | "
                      f"EUR {car['price']:>10,.2f} | Jaar: {year_str:4s}{fuel_str}")
            
            # Analyseer bouwjaar
            years_with_prices = [(c['year'], c['price']) for c in suspicious if c['year']]
            if years_with_prices:
                avg_year = sum(y for y, p in years_with_prices) / len(years_with_prices)
                oldest = min(y for y, p in years_with_prices)
                newest = max(y for y, p in years_with_prices)
                print(f"\n   Bouwjaar analyse:")
                print(f"     - Gemiddeld bouwjaar: {avg_year:.0f}")
                print(f"     - Oudste: {oldest}, Nieuwste: {newest}")
            print()
        
        # Laag maar ok (EUR 1000 - EUR 3000)
        print(f"4. LAGE MAAR MOGELIJK REALISTISCHE PRIJZEN (EUR {SUSPICIOUS_PRICE_THRESHOLD} - EUR 3.000):")
        print(f"   Aantal: {len(low_but_ok)}")
        print(f"   Dit zijn prijzen die laag zijn maar mogelijk nog realistisch voor oude auto's.")
        print()
        
        # Prijsverdeling analyse
        print("=" * 80)
        print("PRIJSVERDELING ANALYSE")
        print("=" * 80)
        print()
        
        price_ranges = {
            f'< EUR {VERY_SUSPICIOUS_PRICE_THRESHOLD}': len(very_suspicious),
            f'EUR {VERY_SUSPICIOUS_PRICE_THRESHOLD} - EUR {SUSPICIOUS_PRICE_THRESHOLD}': len(suspicious),
            f'EUR {SUSPICIOUS_PRICE_THRESHOLD} - EUR 3.000': len(low_but_ok),
            'EUR 3.000 - EUR 10.000': sum(1 for c in cars_with_prices if 3000 <= c['price'] < 10000),
            'EUR 10.000 - EUR 20.000': sum(1 for c in cars_with_prices if 10000 <= c['price'] < 20000),
            'EUR 20.000+': sum(1 for c in cars_with_prices if c['price'] >= 20000),
        }
        
        for range_name, count in price_ranges.items():
            percentage = (count / len(cars_with_prices)) * 100 if cars_with_prices else 0
            print(f"  {range_name:30s}: {count:5d} auto's ({percentage:5.1f}%)")
        
        print()
        print("=" * 80)
        print("AANBEVELINGEN")
        print("=" * 80)
        print()
        
        if len(below_minimum) > 0:
            print(f"[!] {len(below_minimum)} auto's hebben prijzen onder EUR {MIN_REALISTIC_PRICE}")
            print(f"    Deze worden al gefilterd door de huidige IsCarRealistic check.")
            print()
        
        if len(very_suspicious) > 0:
            print(f"[!] {len(very_suspicious)} auto's hebben zeer verdachte prijzen (< EUR {VERY_SUSPICIOUS_PRICE_THRESHOLD})")
            print(f"    Overweeg om deze handmatig te controleren of een striktere filter te gebruiken.")
            print()
        
        if len(suspicious) > 0:
            print(f"[!] {len(suspicious)} auto's hebben verdachte prijzen (EUR {VERY_SUSPICIOUS_PRICE_THRESHOLD}-{SUSPICIOUS_PRICE_THRESHOLD})")
            print(f"    Deze kunnen realistisch zijn voor oude auto's of auto's met schade.")
            print()
        
        # Bereken minimum realistische prijs op basis van data
        valid_prices = [c['price'] for c in cars_with_prices if c['price'] >= MIN_REALISTIC_PRICE]
        if valid_prices:
            valid_prices.sort()
            p5 = valid_prices[int(len(valid_prices) * 0.05)]  # 5e percentiel
            p10 = valid_prices[int(len(valid_prices) * 0.10)]  # 10e percentiel
            
            print(f"STATISTISCHE ANALYSE:")
            print(f"  Huidige minimum realistische prijs: EUR {MIN_REALISTIC_PRICE}")
            print(f"  5e percentiel van geldige prijzen: EUR {p5:,.2f}")
            print(f"  10e percentiel van geldige prijzen: EUR {p10:,.2f}")
            print()
            print(f"  Als de 5e percentiel (EUR {p5:,.0f}) veel hoger is dan EUR {MIN_REALISTIC_PRICE},")
            print(f"  zou je kunnen overwegen om de minimum prijs te verhogen.")
        
        print()
        print("=" * 80)
        
except Exception as e:
    print(f"FOUT bij het analyseren: {e}")
    import traceback
    traceback.print_exc()
    sys.exit(1)

