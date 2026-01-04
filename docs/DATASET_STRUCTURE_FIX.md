# Fix: Nieuwe Dataset Structuur

## 🔍 Probleem

De nieuwe dataset (`df_master_v8_def.csv`) heeft een **andere structuur** dan de oude:
- **Nieuwe structuur**: `merk,model,bouwjaar,type_auto,brandstof,transmissie,vermogen,prijs`
- **Geen ID kolom** - ID wordt nu gegenereerd op basis van rijnummer
- **Kolom namen** zijn anders (bijv. `prijs` i.p.v. `budget`)

## ✅ Fixes Geïmplementeerd

### 1. Verbeterde Kolomdetectie (`FindColumnIndex`)

**Voor:**
- Gebruikte alleen `Contains()` matching
- Kon verkeerde kolommen matchen

**Na:**
- **Exacte match eerst** (meest betrouwbaar)
- **Contains match als fallback** (voor flexibiliteit)
- Betere prioriteit voor Nederlandse kolomnamen (`prijs` i.p.v. `budget`)

### 2. Validatie Toegevoegd

- Controleert of kritieke kolommen zijn gevonden (`merk`, `model`)
- Waarschuwingen als `vermogen` of `prijs` kolommen niet worden gevonden
- Toont beschikbare kolommen bij fouten

### 3. ID Generatie

- Nieuwe dataset heeft geen ID kolom
- ID wordt nu gegenereerd op basis van rijnummer (1-based)
- Rij 1 (na header) = ID 1, Rij 2 = ID 2, etc.

## 📊 Nieuwe Dataset Structuur

```
Header: merk,model,bouwjaar,type_auto,brandstof,transmissie,vermogen,prijs
```

**Kolom Indices:**
- `merk` → Index 0
- `model` → Index 1
- `bouwjaar` → Index 2
- `type_auto` → Index 3
- `brandstof` → Index 4
- `transmissie` → Index 5
- `vermogen` → Index 6
- `prijs` → Index 7

## 🔧 Code Wijzigingen

### `FindColumnIndex` Methode
```csharp
// EERST: Probeer exacte match (meest betrouwbaar)
if (header.Equals(nameLower, StringComparison.OrdinalIgnoreCase))
{
    return i;
}

// DAN: Probeer contains match (voor flexibiliteit)
if (header.Contains(nameLower))
{
    return i;
}
```

### Kolom Detectie Prioriteit
```csharp
int budgetIndex = FindColumnIndex(headerColumns, new[] { "prijs", "price", "budget", "cars prices" }); 
// PRIJS eerst voor nieuwe dataset
```

### Validatie
```csharp
if (merkIndex < 0 || modelIndex < 0)
{
    Console.WriteLine($"[ERROR] ⚠️ KRITIEKE KOLOMMEN NIET GEVONDEN!");
    // ...
}
```

## 📋 Wat te Controleren

### Bij Startup - Console Output:

```
[PARSE] Header kolommen gevonden: merk | model | bouwjaar | type_auto | brandstof | transmissie | vermogen | prijs
[PARSE] Aantal kolommen: 8
[PARSE] Kolom indices - ID:-1, Merk:0, Model:1, Vermogen:6, Brandstof:4, Budget:7, Bouwjaar:2
[PARSE] Eerste data rij heeft 8 kolommen:
[PARSE]   Kolom[0] 'merk' = 'chevrolet'
[PARSE]   Kolom[1] 'model' = 'equinox'
[PARSE]   Kolom[6] 'vermogen' = '182.0'
[PARSE] ⚠️ VERIFICATIE: Vermogen kolom[6] = '182.0'
```

### Als er een probleem is:

```
[ERROR] ⚠️ KRITIEKE KOLOMMEN NIET GEVONDEN!
[ERROR] MerkIndex: -1, ModelIndex: -1
[ERROR] Beschikbare kolommen: ...
[WARNING] ⚠️ Vermogen kolom niet gevonden!
[WARNING] ⚠️ Prijs/Budget kolom niet gevonden!
```

## 🚀 Volgende Stappen

1. **Herstart de backend** om de nieuwe code te laden
2. **Controleer de console output** voor:
   - Kolom indices (moeten 0,1,2,4,6,7 zijn)
   - Vermogen parsing (moet index 6 zijn)
   - Aantal geladen auto's (moet ~65.128 zijn)
3. **Test de API** om te zien of Vermogen waarden nu correct zijn

## 📊 Verwacht Gedrag

- **65.128 regels** in CSV (exclusief header)
- **~65.128 auto's** moeten worden geladen
- **Vermogen waarden** moeten realistisch zijn (20-800 KW)
- **ID's** moeten 1, 2, 3, ... zijn (gebaseerd op rijnummer)

Als dit niet het geval is, toont de logging nu precies waar het probleem zit.




