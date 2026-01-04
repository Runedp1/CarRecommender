# Quota Exceeded Diagnose - Met Slechts 2 App Services

## 🔍 Situatie
- Je hebt maar 2 App Services (backend + frontend) ✅
- Maar status toont "Quota exceeded" ❌
- Deployments falen ❌

## 🎯 Mogelijke Oorzaken

### Oorzaak 1: Twee Verschillende App Service Plans

**Check dit:**
1. Azure Portal → App Services → `app-carrecommender-dev`
2. Klik op "App Service plan" (rechts in het overzicht)
3. Noteer de naam (bijv. `ASP-xxx`)
4. Azure Portal → App Services → `pp-carrecommender-web-dev`
5. Klik op "App Service plan"
6. Noteer de naam

**Als de namen VERSCHILLEND zijn:**
- ❌ Probleem: Je hebt 2 App Service Plans
- ✅ Oplossing: Zet beide apps in hetzelfde plan (zie onder)

### Oorzaak 2: Instance Count > 1

**Check dit:**
1. Azure Portal → App Service plans → Selecteer je plan
2. Klik op "Scale out (App Service plan)" in het menu
3. Check "Instance count"

**Als Instance count > 1:**
- ❌ Probleem: Free tier ondersteunt maar 1 instance
- ✅ Oplossing: Zet op 1 instance

### Oorzaak 3: Pricing Tier is niet Free

**Check dit:**
1. Azure Portal → App Service plans → Selecteer je plan
2. Check "Pricing tier" bovenaan

**Als Pricing tier NIET "Free (F1)" is:**
- ❌ Probleem: Betaalde tier kan quota overschrijden
- ✅ Oplossing: Downgrade naar Free (F1) als mogelijk

### Oorzaak 4: Andere Resources Overschrijden Quota

**Check dit:**
1. Azure Portal → Subscriptions → Azure for Students
2. Klik "Usage + quotas"
3. Bekijk ALLE resources:
   - App Services: moet ≤ 2 zijn
   - App Service Plans: moet ≤ 1 zijn (voor free tier)
   - Storage accounts: hoeveel heb je?
   - Function Apps: hoeveel heb je?
   - Andere services?

**Als andere resources quota overschrijden:**
- Verwijder ongebruikte resources

## ✅ Oplossing: Combineer Apps in Één App Service Plan

### Stap 1: Identificeer Welke Plan je Wilt Gebruiken

1. Ga naar één van je App Services
2. Noteer de App Service Plan naam
3. Gebruik deze plan voor beide apps

### Stap 2: Change App Service Plan voor de Andere App

1. Azure Portal → App Services → `pp-carrecommender-web-dev` (of de andere app)
2. Klik op "Change App Service Plan" in het menu (of klik op de App Service Plan link en dan "Change App Service Plan")
3. Selecteer de App Service Plan waar de andere app in zit
4. Klik "Apply" of "OK"
5. Wacht tot de wijziging voltooid is (kan 1-2 minuten duren)

### Stap 3: Verwijder Lege App Service Plan (Als Applicable)

**WAARSCHUWING: Alleen doen als de plan NU leeg is (geen apps meer)!**

1. Azure Portal → App Service plans
2. Zoek de plan die nu leeg is
3. Klik op de plan
4. Klik "Delete"
5. Bevestig verwijdering

### Stap 4: Verifieer Instance Count

1. Azure Portal → App Service plans → Selecteer je (nu enige) plan
2. Klik "Scale out (App Service plan)"
3. Zorg dat "Instance count" = 1 (voor Free tier)
4. Klik "Apply" als je het hebt gewijzigd

### Stap 5: Verifieer Pricing Tier

1. Azure Portal → App Service plans → Selecteer je plan
2. Check "Pricing tier"
3. Moet "Free (F1)" zijn
4. Als het een betaalde tier is:
   - Klik "Scale up (App Service plan)"
   - Selecteer "Free (F1)"
   - Klik "Apply"
   - **LET OP**: Dit kan kosten met zich meebrengen als je al op betaalde tier zit

### Stap 6: Wacht en Check Status

1. Wacht 5-10 minuten
2. Azure Portal → App Services
3. Check status van beide apps:
   - Moet "Running" zijn (niet "Quota exceeded")
4. Als nog steeds "Quota exceeded":
   - Azure Portal → App Services → Selecteer app → "Restart"
   - Wacht 2-3 minuten
   - Check status opnieuw

## 📋 Checklist

- [ ] Beide apps zitten in de ZELFDE App Service Plan
- [ ] Je hebt maar 1 App Service Plan (andere zijn verwijderd als leeg)
- [ ] Instance count = 1 (voor beide apps, via de plan)
- [ ] Pricing tier = Free (F1)
- [ ] Gecheckt "Usage + quotas" voor andere overschreden resources
- [ ] Gewacht 5-10 minuten na wijzigingen
- [ ] App Services status = "Running" (niet "Quota exceeded")
- [ ] Herstart apps als status nog niet "Running" is

## 🔍 Als Het Nog Steeds Niet Werkt

### Check Azure Support Center

1. Azure Portal → "Help + support" (vraagteken icoon bovenaan)
2. Klik "New support request"
3. Selecteer issue type: "Service and subscription limits (quotas)"
4. Beschrijf het probleem

### Check Subscription Status

1. Azure Portal → Subscriptions → Azure for Students
2. Check "Status":
   - Moet "Active" zijn
   - Als "Disabled" of "Warned": Dit kan het probleem zijn

### Check Resource Usage in Detail

1. Azure Portal → Subscriptions → Azure for Students
2. "Usage + quotas" → Bekijk elke resource categorie
3. Zoek naar resources die op 100% of meer zitten
4. Dit vertelt je precies welke quota overschreden is

## 💡 Meest Waarschijnlijke Oplossing

**Als je 2 App Services hebt maar quota overschreden is:**
- 99% kans dat je 2 verschillende App Service Plans hebt
- Oplossing: Zet beide apps in hetzelfde plan
- Dit zou het probleem moeten oplossen



