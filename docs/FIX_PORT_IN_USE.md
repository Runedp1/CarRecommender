# Fix: Poort Al In Gebruik

## 🔍 Het Probleem

```
Failed to bind to address http://127.0.0.1:7000: address already in use.
```

**Oorzaak:** Een vorig gecrasht project draait nog op de achtergrond en gebruikt poort 7000.

---

## ✅ Oplossing: Stop Het Proces

### Methode 1: Via Command Line (Aanbevolen)

**Stap 1: Vind welk proces de poort gebruikt:**
```powershell
netstat -ano | findstr :7000
```

**Output voorbeeld:**
```
TCP    127.0.0.1:7000         0.0.0.0:0              LISTENING       17988
```

**Stap 2: Stop het proces:**
```powershell
taskkill /PID 17988 /F
```

**Stap 3: Verifieer dat poort vrij is:**
```powershell
netstat -ano | findstr :7000
```

**Als er niets wordt getoond:** ✅ Poort is vrij!

---

### Methode 2: Via Task Manager

1. Open **Task Manager** (Ctrl+Shift+Esc)
2. Ga naar tab **"Details"**
3. Zoek naar proces met PID **17988** (of andere PID uit netstat)
4. Rechtsklik → **"End Task"**

---

### Methode 3: Wijzig Poort (Alternatief)

Als je de poort wilt wijzigen in plaats van het proces te stoppen:

**Edit `frontend/CarRecommender.Web/Properties/launchSettings.json`:**

```json
{
  "profiles": {
    "http": {
      "applicationUrl": "http://localhost:7002",  // ← Wijzig poort
      ...
    },
    "https": {
      "applicationUrl": "https://localhost:7003;http://localhost:7002",  // ← Wijzig poort
      ...
    }
  }
}
```

**Maar:** Het is beter om het oude proces te stoppen, zodat je de standaard poorten kunt gebruiken.

---

## 🔍 Andere Veelgebruikte Poorten

Als je meerdere projecten hebt, check ook deze poorten:

```powershell
# Check poort 5000 (vaak gebruikt door API)
netstat -ano | findstr :5000

# Check poort 5001 (HTTPS)
netstat -ano | findstr :5001

# Check poort 7001 (HTTPS frontend)
netstat -ano | findstr :7001
```

---

## 💡 Voorkomen in de Toekomst

**Altijd stoppen voordat je sluit:**
- Gebruik **Ctrl+C** in de terminal waar `dotnet run` draait
- Of sluit de terminal niet abrupt

**Als je vergeet te stoppen:**
- Gebruik bovenstaande methode om het proces te vinden en te stoppen

---

## ✅ Na Fix: Test Opnieuw

```powershell
cd frontend/CarRecommender.Web
dotnet run
```

**Verwacht:** Applicatie start zonder poort errors!

---

**Status:** ✅ Poort probleem opgelost
**Volgende:** Test applicatie opnieuw!










