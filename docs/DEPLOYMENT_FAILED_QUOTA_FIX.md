# Deployment Faalt - Quota Exceeded Oplossing

## 🔴 Probleem: Beide Deployments Falen + Quota Exceeded

**Symptomen:**
- Frontend deployment faalt
- Backend deployment faalt  
- Azure Portal toont "Quota exceeded" status
- App Services kunnen niet starten

## 🎯 Root Cause: Azure Subscription Quota Overschreden

Azure for Students heeft strikte limieten. Als je quota overschreden bent, kunnen deployments falen EN apps kunnen niet starten.

## ✅ OPLOSSING: Verwijder Ongebruikte Resources

### Stap 1: Identificeer Alle App Services

1. **Azure Portal** → **Resource groups**
2. Bekijk ALLE resource groups die je hebt
3. Tel het totaal aantal App Services:
   - `app-carrecommender-dev` (backend)
   - `pp-carrecommender-web-dev` (frontend)
   - Eventuele andere App Services?

**Voor Azure for Students:**
- Meestal maar **1 gratis App Service** toegestaan
- Of maximaal 2 App Services als beide in dezelfde App Service Plan zitten

### Stap 2: Check App Service Plans

1. **Azure Portal** → **App Service plans**
2. Bekijk alle App Service Plans:
   - `ASP-carrecommenderdevrg-9b05` (deze zou beide apps moeten hosten)
   - Eventuele andere App Service Plans?

**Belangrijk:**
- Je moet maar **1 App Service Plan** hebben voor beide apps
- Beide apps (backend + frontend) moeten in de ZELFDE App Service Plan zitten (free tier)

### Stap 3: Combineer Apps in Één App Service Plan (Aanbevolen)

**Optie A: Beide Apps in dezelfde App Service Plan (Free Tier)**

1. **Azure Portal** → **App Service plans** → **ASP-carrecommenderdevrg-9b05**
2. Check welke apps erin zitten:
   - Moeten beide apps hierin zitten (backend + frontend)
3. Als één van de apps in een andere App Service Plan zit:
   - Ga naar die App Service
   - **Change App Service Plan**
   - Selecteer `ASP-carrecommenderdevrg-9b05`
   - Klik **"Apply"**

### Stap 4: Verwijder Ongebruikte Resources

Als je MEER dan 2 App Services hebt OF meer dan 1 App Service Plan:

1. **Identificeer welke App Services je nodig hebt:**
   - ✅ `app-carrecommender-dev` (backend) - NODIG
   - ✅ `pp-carrecommender-web-dev` (frontend) - NODIG
   - ❌ Alle andere App Services - VERWIJDEREN

2. **Verwijder ongebruikte App Services:**
   - Azure Portal → App Services → Selecteer ongebruikte App Service
   - Klik **"Delete"**
   - Bevestig verwijdering

3. **Verwijder ongebruikte App Service Plans:**
   - Azure Portal → App Service plans
   - Als je meer dan 1 plan hebt, verwijder dan de lege/ongebruikte
   - **WAARSCHUWING**: Verwijder alleen als er GEEN apps meer in zitten!

4. **Verwijder andere ongebruikte resources:**
   - Storage accounts (als je er meer dan nodig hebt)
   - Function Apps (als je die hebt maar niet gebruikt)
   - Andere services die je niet nodig hebt

### Stap 5: Wacht en Verifieer Quota

1. **Wacht 5-10 minuten** na het verwijderen van resources
2. **Azure Portal** → **Subscriptions** → **Azure for Students**
3. Klik op **"Usage + quotas"**
4. Check of quota nu binnen limieten zijn:
   - App Services: ≤ 2 (of 1, afhankelijk van je plan)
   - App Service Plans: ≤ 1 (voor free tier)

### Stap 6: Verifieer App Service Status

1. **Azure Portal** → **App Services**
2. Check status van beide apps:
   - `app-carrecommender-dev` → Status moet **"Running"** zijn (niet "Quota exceeded")
   - `pp-carrecommender-web-dev` → Status moet **"Running"** zijn

3. Als status nog steeds "Quota exceeded":
   - Wacht nog 5-10 minuten (Azure kan even nodig hebben)
   - Refresh de pagina
   - Probeer de App Service te restart: **"Restart"** knop

### Stap 7: Test Deployments Opnieuw

Na het oplossen van quota probleem:

1. **GitHub** → **Actions** tab
2. Ga naar de gefaalde workflows:
   - "Build and deploy ASP.Net Core app to Azure Web App - app-carrecommender-dev"
   - "Build and deploy ASP.Net Core app to Azure Web App - pp-carrecommender-web-dev"
3. Klik op **"Re-run jobs"** → **"Re-run all jobs"**

OF:

1. Maak een kleine wijziging in code (bijv. comment toevoegen)
2. Commit en push:
   ```bash
   git add .
   git commit -m "Trigger deployment after quota fix"
   git push origin main
   ```
3. Dit triggert automatisch nieuwe deployments

## 📋 Checklist

- [ ] Geïdentificeerd hoeveel App Services ik heb (moet ≤ 2 zijn)
- [ ] Geïdentificeerd hoeveel App Service Plans ik heb (moet 1 zijn voor free tier)
- [ ] Beide apps (backend + frontend) zitten in dezelfde App Service Plan
- [ ] Verwijderd ongebruikte App Services
- [ ] Verwijderd ongebruikte App Service Plans (alleen als leeg)
- [ ] Gewacht 5-10 minuten na verwijdering
- [ ] Geverifieerd quota in "Usage + quotas" (binnen limieten)
- [ ] App Services status = "Running" (niet "Quota exceeded")
- [ ] Herstart App Services als status nog niet "Running" is
- [ ] Nieuwe deployments getriggerd (re-run of nieuwe commit)
- [ ] Deployments slagen nu

## 🚨 Belangrijke Waarschuwingen

1. **Verwijder NOOIT een App Service Plan als er nog apps in zitten!**
   - Verwijder eerst de apps
   - Dan pas het App Service Plan

2. **Maak backups/notities voordat je verwijdert:**
   - Noteer welke resources je verwijdert
   - Als je per ongeluk iets verwijdert, kan het lastig zijn om terug te halen

3. **Wacht altijd na verwijderen:**
   - Azure heeft tijd nodig om quota bij te werken
   - Meestal 5-10 minuten, soms langer

## 💡 Alternatieve Oplossing: Upgrade naar Betaalde Tier (Niet Aanbevolen)

Als je echt meerdere App Services nodig hebt:
- Upgrade App Service Plan naar Basic tier (~€10/maand)
- Dan kun je meerdere apps hosten

**Maar voor dit student project:** Eén App Service Plan met 2 apps is voldoende!

## 🔗 Nuttige Links

- [Azure for Students FAQ](https://azure.microsoft.com/free/students/)
- [Azure Free Tier Limits](https://azure.microsoft.com/free/)
- [App Service Limits](https://docs.microsoft.com/azure/azure-resource-manager/management/azure-subscription-service-limits#app-service-limits)



