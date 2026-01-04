# Azure Quota Exceeded - Oplossing

## 🔴 Probleem: "Quota exceeded" Status

Als je in Azure Portal ziet dat de status "Quota exceeded" is, kan de App Service niet starten, ook al zegt de start knop "successfully started".

## 🔍 Oorzaak

Azure for Students heeft limieten op:
- Aantal App Services (meestal 1-2 gratis)
- App Service Plan tier (Free tier heeft beperkingen)
- Totale compute resources

## ✅ Oplossing 1: Check Subscription Quota

1. **Azure Portal** → **Subscriptions** → **Azure for Students**
2. Klik op **"Usage + quotas"** of **"Resource usage"**
3. Bekijk welke resources overschreden zijn:
   - App Service plans
   - App Services
   - Storage accounts
   - etc.

## ✅ Oplossing 2: Delete Ongebruikte Resources

Als je te veel resources hebt:

1. **Azure Portal** → **Resource groups**
2. Check alle resource groups voor oude/ongebruikte App Services
3. **Delete** oude App Services die je niet meer gebruikt
4. Wacht 5-10 minuten
5. Probeer opnieuw te starten

## ✅ Oplossing 3: Check App Service Plan

1. **Azure Portal** → **App Services** → **pp-carrecommender-web-dev**
2. Klik op **App Service plan** link (ASP-carrecommenderdevrg-9b05)
3. Check de **Pricing tier**:
   - **Free (F1)** = Gratis maar beperkt
   - Als je op een betaalde tier zit, switch naar Free
4. Check **Scale out (App Service plan)**:
   - **Instance count** moet 1 zijn (voor Free tier)

## ✅ Oplossing 4: Restart App Service Plan

Soms helpt een restart:

1. **Azure Portal** → **App Service plans** → **ASP-carrecommenderdevrg-9b05**
2. Klik **"Restart"**
3. Wacht 2-3 minuten
4. Probeer App Service opnieuw te starten

## ✅ Oplossing 5: Check Resource Group Limits

1. **Azure Portal** → **Resource groups** → **carrecommender-dev-rg**
2. Bekijk alle resources in de groep
3. Als je meerdere App Services hebt, overweeg om ongebruikte te verwijderen

## 🚨 Meest Waarschijnlijke Oorzaak

**Je hebt te veel App Services of App Service Plans.**

Voor Azure for Students:
- Meestal maar **1 gratis App Service** toegestaan
- Als je er 2+ hebt, overschrijd je het quota

## 📋 Checklist

- [ ] Check "Usage + quotas" in Subscription
- [ ] Delete ongebruikte App Services
- [ ] Check App Service Plan pricing tier (moet Free zijn)
- [ ] Check instance count (moet 1 zijn)
- [ ] Restart App Service Plan
- [ ] Wacht 5-10 minuten na het verwijderen van resources
- [ ] Probeer App Service opnieuw te starten

## 💡 Alternatieve Oplossing: Combineer Frontend en Backend

Als je quota beperkt zijn, overweeg om:
- Frontend en backend in dezelfde App Service te hosten
- Of gebruik alleen de backend API en test frontend lokaal

## 🔗 Nuttige Links

- [Azure for Students FAQ](https://azure.microsoft.com/free/students/)
- [Azure Free Tier Limits](https://azure.microsoft.com/free/)
- [App Service Limits](https://docs.microsoft.com/azure/azure-resource-manager/management/azure-subscription-service-limits#app-service-limits)



