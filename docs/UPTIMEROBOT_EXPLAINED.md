# UptimeRobot - Hoe het Werkt

## ✅ Ja, het werkt ook als je PC uitstaat!

**UptimeRobot is een cloud service** die volledig onafhankelijk van jouw computer werkt.

---

## 🌐 Hoe UptimeRobot Werkt

### Wat is UptimeRobot?
- **Cloud service** die draait op UptimeRobot's servers
- **Niet op jouw PC** - draait ergens anders op het internet
- **24/7 actief** - werkt altijd, ook als je PC uit staat
- **Volledig automatisch** - je hoeft niets te doen na setup

### Hoe houdt het je app actief?
1. **UptimeRobot's servers** sturen elke 5-10 minuten een HTTP request naar je Azure App Service
2. Dit gebeurt **automatisch** en **continu**
3. Je Azure App Service blijft actief omdat er regelmatig requests binnenkomen
4. **Geen slaapstand** = docenten kunnen altijd direct de website openen

---

## 🔄 Vergelijking

### ❌ Lokale Script (werkt NIET als PC uitstaat):
```powershell
# Dit script draait op JOUW PC
while ($true) {
    Invoke-WebRequest -Uri "https://app-carrecommender-web-dev2.azurewebsites.net"
    Start-Sleep -Seconds 600
}
```
**Probleem:** Als je PC uitstaat, stopt het script → app gaat in slaapstand

### ✅ UptimeRobot (werkt WEL als PC uitstaat):
```
UptimeRobot Server (ergens op internet)
    ↓ (elke 10 minuten)
    HTTP Request → Azure App Service
    ↓
    App blijft actief ✅
```
**Voordeel:** Werkt altijd, ook als je PC uitstaat, slaapt, of offline is

---

## 📊 Praktisch Voorbeeld

### Scenario: Je PC staat uit

**Zondag 00:00** - Je PC staat uit
- UptimeRobot blijft requests sturen → App blijft actief ✅

**Maandag 10:00** - Docent opent website
- App is actief (geen slaapstand)
- Website werkt direct ✅

**Maandag 10:30** - Je komt thuis, PC staat nog uit
- UptimeRobot blijft werken
- Docenten kunnen nog steeds de website gebruiken ✅

---

## ✅ Voordelen van UptimeRobot

1. **Werkt 24/7** - Ook als je PC uitstaat
2. **Volledig automatisch** - Setup één keer, daarna geen actie nodig
3. **Gratis** - Geen kosten
4. **Betrouwbaar** - Cloud service met hoge uptime
5. **Geen onderhoud** - Werkt jarenlang zonder aandacht

---

## 🎯 Setup (Eén Keer)

1. Ga naar: https://uptimerobot.com
2. Maak account (5 minuten)
3. Voeg monitor toe (2 minuten)
4. **Klaar!** - Werkt nu altijd, ook als je PC uitstaat

---

## 💡 Andere Cloud Services (Ook Gratis)

Als je UptimeRobot niet wilt gebruiken, zijn er alternatieven:

### 1. Azure Logic Apps (Binnen Azure)
- Cloud service van Microsoft
- Werkt ook als je PC uitstaat
- Gratis tier beschikbaar

### 2. GitHub Actions (Als je GitHub gebruikt)
- Cloud service van GitHub
- Werkt ook als je PC uitstaat
- Gratis voor publieke repos

### 3. Cron-job Services
- Verschillende gratis opties beschikbaar
- Allemaal cloud-based

---

## ❌ Wat Werkt NIET als PC uitstaat

- Lokale PowerShell scripts
- Lokale Python scripts
- Browser extensies
- Alles wat op jouw PC draait

---

## ✅ Conclusie

**UptimeRobot werkt perfect als je PC uitstaat!**

- Cloud service (draait niet op jouw PC)
- Werkt 24/7 automatisch
- Houdt je app actief
- Docenten kunnen altijd de website gebruiken

**Setup één keer → Werkt altijd!**

---

**Status:** ✅ Werkt ook als PC uitstaat
**Type:** Cloud service (niet lokaal)

