# DEMO OUTPUT VOORBEELD
**Voor docent - Voorbeeld van wat het recommendation systeem produceert**

---

## 📊 HUIDIGE SITUATIE

**Er wordt GEEN filtering toegepast** - alle auto's worden meegenomen in recommendations.

De recommendations zijn **NIET random** - ze gebruiken similarity scores gebaseerd op:
- Budget (prijs): 30% gewicht
- Vermogen: 25% gewicht  
- Brandstof: 25% gewicht
- Bouwjaar: 20% gewicht

**Maar**: Auto's met verdachte prijzen (< €500) worden nog wel meegenomen.

---

## 🖥️ VOORBEELD OUTPUT

Wanneer je `dotnet run` uitvoert, krijg je zoiets als:

```
================================================================================
CAR RECOMMENDATION SYSTEM
================================================================================

CSV-bestand gevonden: C:\...\data\Cleaned_Car_Data_For_App_Fully_Enriched.csv
Laden van auto's...

Aantal ingelezen auto's: 20,755

Eerste 5 auto's:
----------------------------------------------------------------------------------------------------
Merk            Model                Vermogen   Brandstof    Budget          Bouwjaar  
----------------------------------------------------------------------------------------------------
Toyota          Camry                180        Petrol       €25,000.00      2020     
Honda           Civic                150        Petrol       €18,500.00      2019     
BMW             3 Series             200        Petrol       €35,000.00      2021     
Mercedes-Benz   C-Class              220        Diesel       €42,000.00      2020     
Audi            A4                   190        Petrol       €38,000.00      2022     

====================================================================================================
DEMO: Content-Based Recommendation
====================================================================================================

Target auto: Toyota Camry (2020)
  Vermogen: 180 KW | Brandstof: Petrol | Budget: €25,000.00

Top 5 Aanbevelingen (gebaseerd op Power, Budget, Year en Fuel):
------------------------------------------------------------------------------------------------------------------------------------------------------
Merk            Model                Vermogen   Brandstof    Budget          Bouwjaar   Similarity   Image Path                                    
------------------------------------------------------------------------------------------------------------------------------------------------------
Toyota          Camry                175        Petrol       €24,500.00      2019       0.9234       images/toyota/camry/1234.jpg                  
Honda           Accord               185        Petrol       €26,000.00      2020       0.9156       images/honda/accord/5678.jpg                  
Nissan          Altima               170        Petrol       €23,500.00      2021       0.9102       images/nissan/altima/9012.jpg                  
Mazda           6                    175        Petrol       €24,000.00      2020       0.9087       images/mazda/6/3456.jpg                       
Hyundai         Sonata               160        Petrol       €22,500.00      2019       0.9054       images/hyundai/sonata/7890.jpg                 

Druk op een toets om af te sluiten...
```

---

## ✅ WAT WERKT ER

1. **CSV Parsing**: Laadt 20.755 auto's uit CSV
2. **Similarity Berekening**: Niet random - gebruikt wiskundige formules
3. **Gewogen Scores**: Budget heeft 30% gewicht (meest belangrijk)
4. **Sortering**: Recommendations gesorteerd op similarity (hoogste eerst)
5. **Image Paths**: Genereert paths voor alle auto's

---

## ⚠️ WAT NOG NIET GEFILTERD WORDT

- Auto's met prijs < €500 (verdachte data)
- Auto's met prijs = 0
- Auto's met onrealistische bouwjaren

**Impact**: Deze auto's kunnen nog steeds in recommendations voorkomen.

---

## 🎯 HOE TE DEMONSTREREN AAN DOCENT

### Optie 1: Live Demo
```bash
cd "Recommendation System"
dotnet run
```

### Optie 2: Screenshot van Output
Maak een screenshot van de console output na `dotnet run`.

### Optie 3: Toon Code
Wijs op:
- `RecommendationEngine.cs` regel 43-50: Budget similarity berekening
- `RecommendationService.cs` regel 54-59: Similarity berekening voor elke auto
- `RecommendationService.cs` regel 70-72: Sorteren op similarity

---

## 📈 VERWACHTE RESULTATEN

**Voor een Toyota Camry (2020, €25.000, 180 KW, Petrol):**

Je zou moeten zien:
1. Andere middenklasse sedans (Camry, Accord, Altima)
2. Vergelijkbare prijzen (±€3.000)
3. Vergelijkbare vermogens (±20 KW)
4. Zelfde brandstof (Petrol)
5. Vergelijkbare bouwjaren (2019-2021)

**Similarity scores** zouden tussen 0.85-0.95 moeten liggen voor goede matches.

---

## 🔍 VERIFICATIE DAT HET WERKT

**Test zelf:**
1. Run `dotnet run`
2. Kijk naar de similarity scores - moeten logisch zijn (hoogste voor meest vergelijkbare auto's)
3. Check of recommendations vergelijkbare auto's zijn (niet random)
4. Test met verschillende target auto's

**Voor docent:**
- Toon dat similarity scores verschillen (niet allemaal hetzelfde = niet random)
- Toon dat recommendations logisch zijn (vergelijkbare auto's)
- Toon de code die dit berekent (RecommendationEngine.cs)


