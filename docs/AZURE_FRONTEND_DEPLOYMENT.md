# Azure Frontend Deployment Guide - CarRecommender.Web

## Overzicht

Deze guide helpt je om het **CarRecommender.Web** frontend project te deployen naar Azure App Service, zodat je docenten via één publieke URL de website kunnen openen.

**API URL (al geconfigureerd):** `https://app-carrecommender-dev-h0agdkbfbcb3fxg7.westeurope-01.azurewebsites.net`

---

## ✅ Pre-deployment Checklist

### Configuratie Controle

- ✅ **appsettings.json** - Bevat de Azure API URL
- ✅ **appsettings.Development.json** - Bevat de Azure API URL voor lokale ontwikkeling
- ✅ **appsettings.Production.json** - Bevat de Azure API URL voor productie
- ✅ **Program.cs** - Configureert HttpClient met BaseAddress uit configuratie
- ✅ **CarApiClient.cs** - Gebruikt HttpClient met relatieve paths (geen hard-coded URLs)
- ✅ **wwwroot** - Statische bestanden zijn aanwezig en geconfigureerd
- ✅ **Geen localhost referenties** - Alle API calls gebruiken configuratie

---

## 📋 Stappenplan: Deploy naar Azure App Service

### Stap 1: Open het Project in Visual Studio

1. Open **Visual Studio 2022** (of nieuwer)
2. Open de solution: `CarRecommender.sln`
3. Zorg dat het project **CarRecommender.Web** zichtbaar is in Solution Explorer

### Stap 2: Maak een Publish Profile aan

1. **Rechtsklik** op het project **CarRecommender.Web** in Solution Explorer
2. Selecteer **"Publish"** (of **"Publiceren"**)
3. In het Publish dialoogvenster:
   - Kies **"Azure"** als target
   - Selecteer **"Azure App Service (Windows)"** of **"Azure App Service (Linux)"**
   - Klik op **"Next"**

### Stap 3: Configureer Azure Resources

#### Optie A: Nieuwe App Service aanmaken

1. Klik op **"Create new"** of **"Nieuwe maken"**
2. Vul de volgende gegevens in:
   - **Name (Naam):** `app-carrecommender-web-dev` (of een andere unieke naam)
   - **Subscription:** Selecteer je Azure subscription
   - **Resource Group:** 
     - Kies dezelfde resource group als je API (bijv. `carrecommender-dev-rg`)
     - OF maak een nieuwe aan: `carrecommender-dev-rg`
   - **Hosting Plan (App Service Plan):**
     - Kies dezelfde App Service Plan als je API (als die bestaat)
     - OF maak een nieuwe aan:
       - **Name:** `plan-carrecommender-dev`
       - **Location:** `West Europe` (zelfde als je API)
       - **Pricing Tier:** 
         - **Free (F1)** - voor testen (beperkt)
         - **Basic (B1)** - voor productie (aanbevolen, ~€10/maand)
         - **Standard (S1)** - voor meer verkeer (~€50/maand)
3. Klik op **"Create"** of **"Maken"**

#### Optie B: Bestaande App Service gebruiken

1. Klik op **"Select existing"** of **"Bestaande selecteren"**
2. Selecteer je subscription
3. Kies de resource group
4. Selecteer een bestaande App Service (als je die hebt)

### Stap 4: Review Publish Settings

1. Controleer de **Publish method:**
   - **Deployment method:** "Deploy to App Service" (aanbevolen)
   - **Target:** De App Service die je net hebt aangemaakt
2. Klik op **"Finish"** of **"Voltooien"**

### Stap 5: Configureer Deployment Settings (Optioneel)

1. In het Publish profiel venster, klik op **"Edit"** of **"Bewerken"**
2. Controleer de volgende instellingen:
   - **Configuration:** `Release` (voor productie)
   - **Target Framework:** `net9.0`
   - **Deployment Mode:** `Framework-Dependent` (aanbevolen) of `Self-Contained`
3. Klik op **"Save"** of **"Opslaan"**

### Stap 6: Publish het Project

1. Klik op **"Publish"** of **"Publiceren"** in het Publish profiel venster
2. Visual Studio zal nu:
   - Het project builden
   - De bestanden naar Azure uploaden
   - De App Service configureren
3. Wacht tot de deployment voltooid is (kan 2-5 minuten duren)

### Stap 7: Test de Deployment

1. Na een succesvolle deployment, zal Visual Studio de URL tonen (bijv. `https://app-carrecommender-web-dev.azurewebsites.net`)
2. Klik op de link of open de URL in je browser
3. Test de volgende functionaliteiten:
   - ✅ Homepage laadt correct
   - ✅ Navigatie werkt
   - ✅ API calls werken (test de search functionaliteit)
   - ✅ Statische bestanden (CSS, JS) laden correct

---

## 🔧 Post-Deployment Configuratie

### App Settings in Azure Portal (Optioneel)

Als je de API URL via Azure Portal wilt configureren (in plaats van appsettings.json):

1. Ga naar [Azure Portal](https://portal.azure.com)
2. Navigeer naar je App Service: **App Services** → `app-carrecommender-web-dev`
3. Ga naar **Configuration** → **Application settings**
4. Voeg een nieuwe Application Setting toe:
   - **Name:** `ApiSettings__BaseUrl` (let op: dubbele underscore!)
   - **Value:** `https://app-carrecommender-dev-h0agdkbfbcb3fxg7.westeurope-01.azurewebsites.net`
5. Klik op **Save** en wacht tot de app herstart

### CORS Configuratie (indien nodig)

Als je CORS errors krijgt, controleer of de API CORS correct heeft geconfigureerd:

1. Ga naar je **API App Service** in Azure Portal
2. Controleer of CORS is ingesteld om requests van je frontend URL toe te staan

---

## 🧪 Lokale Test (voor Deployment)

Voordat je deployt, test lokaal of alles werkt:

```bash
# Navigeer naar het Web project
cd "CarRecommender.Api\CarRecommender.Web"

# Run het project
dotnet run
```

Open `https://localhost:7001` en test of de API calls werken.

---

## 📝 Belangrijke URLs

- **API URL:** `https://app-carrecommender-dev-h0agdkbfbcb3fxg7.westeurope-01.azurewebsites.net`
- **Frontend URL (na deployment):** `https://app-carrecommender-web-dev.azurewebsites.net` (of jouw gekozen naam)

---

## 🐛 Troubleshooting

### Probleem: API calls falen met CORS errors

**Oplossing:** Controleer of de API CORS heeft geconfigureerd om requests van je frontend domain toe te staan.

### Probleem: Statische bestanden laden niet

**Oplossing:** 
- Controleer of `wwwroot` folder aanwezig is in het project
- Controleer of `MapStaticAssets()` in Program.cs staat

### Probleem: Deployment faalt

**Oplossing:**
- Controleer of je ingelogd bent in Visual Studio met je Azure account
- Controleer of je de juiste subscription hebt geselecteerd
- Controleer of je voldoende rechten hebt op de resource group

### Probleem: App start niet na deployment

**Oplossing:**
- Controleer de **Log stream** in Azure Portal (App Service → Log stream)
- Controleer de **Application Insights** (als geconfigureerd)
- Controleer of de .NET runtime versie correct is ingesteld

---

## 📚 Aanvullende Resources

- [Azure App Service Documentation](https://docs.microsoft.com/azure/app-service/)
- [Deploy ASP.NET Core to Azure](https://docs.microsoft.com/aspnet/core/host-and-deploy/azure-apps/)
- [Azure App Service Pricing](https://azure.microsoft.com/pricing/details/app-service/windows/)

---

## ✅ Deployment Checklist

- [ ] Project build succesvol lokaal
- [ ] Publish profile aangemaakt
- [ ] App Service aangemaakt in Azure
- [ ] Deployment succesvol voltooid
- [ ] Website laadt in browser
- [ ] API calls werken correct
- [ ] Statische bestanden laden
- [ ] URL gedeeld met docenten

---

**Laatste update:** $(date)
**Project:** CarRecommender.Web
**Target Framework:** .NET 9.0



