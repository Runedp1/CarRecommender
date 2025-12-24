# Applicatie Start maar URLs Werken Niet

## ✅ Goed Nieuws: Applicatie Start!

Je ziet:
```
warn: Microsoft.AspNetCore.StaticFiles.StaticFileMiddleware[16]
      The WebRootPath was not found: C:\home\site\wwwroot\wwwroot. Static files may be unavailable.
```

**Dit betekent:**
- ✅ Applicatie start!
- ✅ Geen crash!
- ⚠️ Alleen een warning over static files (niet kritiek voor API)

---

## 🔍 Wat Gebeurt Er Daarna?

**Wacht even en kijk verder in de output van `dotnet CarRecommender.Api.dll`:**

Je zou moeten zien:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

**Zie je deze regels?**
- ✅ **Ja** = Applicatie draait volledig!
- ❌ **Nee** = Applicatie crasht na de warning

---

## 🔍 Het Echte Probleem: IIS vs Handmatig Starten

**Wat je nu doet:**
- Handmatig starten via `dotnet CarRecommender.Api.dll` → Werkt!

**Wat IIS doet:**
- Start applicatie via `web.config` → Mogelijk werkt niet

**Dit betekent:** Het probleem zit waarschijnlijk in IIS configuratie, niet in de applicatie zelf.

---

## ✅ Stap 1: Check of Applicatie Volledig Start

**In Kudu console:**

Laat `dotnet CarRecommender.Api.dll` draaien en kijk verder:

**Verwacht output:**
```
warn: Microsoft.AspNetCore.StaticFiles.StaticFileMiddleware[16]
      The WebRootPath was not found...
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

**Als je "Now listening on" ziet:**
- ✅ Applicatie werkt!
- Het probleem is IIS routing

**Als applicatie crasht:**
- ❌ Er is een error na de warning
- Deel de volledige error output

---

## ✅ Stap 2: Test Via Browser (Terwijl Applicatie Draait)

**Terwijl `dotnet CarRecommender.Api.dll` nog draait:**

1. Open in browser: `https://app-carrecommender-dev.azurewebsites.net/api/health`
2. **Wat gebeurt er?**
   - Werkt het nu? (omdat applicatie handmatig draait)
   - Of nog steeds niet?

**Als het WEL werkt terwijl handmatig draait:**
- ✅ Applicatie werkt perfect!
- ❌ IIS kan applicatie niet starten
- Fix: web.config of App Service configuratie

**Als het NOG STEEDS niet werkt:**
- Mogelijk routing probleem
- Of URL is verkeerd

---

## ✅ Stap 3: Check App Service Status & Configuratie

**Via Azure Portal:**

1. App Service → **"Overview"**
2. Check **Status** = **"Running"**
3. Check **"Configuration"** → **"General settings"**:
   - **.NET Version:** `9.0`
   - **Platform:** `64 Bit`
   - **Startup Command:** Leeg (of `dotnet CarRecommender.Api.dll`)

---

## ✅ Stap 4: Check Application Logs Via Azure Portal

**Terwijl je de URL opent:**

1. Azure Portal → App Service → **"Log stream"**
2. Open in andere tab: `https://app-carrecommender-dev.azurewebsites.net/api/health`
3. **Wat zie je in de logs?**
   - Startup messages?
   - Errors?
   - Requests?

---

## 🔧 Mogelijke Oplossingen

### Oplossing 1: Fix WebRootPath Warning (Optioneel)

De warning is niet kritiek, maar je kunt het fixen:

**In Program.cs:**
```csharp
// Voeg toe voor UseStaticFiles:
var webRootPath = builder.Environment.WebRootPath;
if (string.IsNullOrEmpty(webRootPath) || !Directory.Exists(webRootPath))
{
    // Maak wwwroot folder aan of gebruik huidige directory
    builder.Environment.WebRootPath = Path.Combine(builder.Environment.ContentRootPath, "wwwroot");
    Directory.CreateDirectory(builder.Environment.WebRootPath);
}
```

**Maar dit is niet nodig** - de warning is niet kritiek voor een API.

### Oplossing 2: Check IIS Startup

Het probleem is waarschijnlijk dat IIS de applicatie niet kan starten.

**Check:**
1. Azure Portal → App Service → **"Log stream"**
2. Herstart App Service
3. Bekijk logs bij startup
4. **Wat zie je?**

---

## 📋 Wat Ik Nodig Heb

1. **Volledige output van `dotnet CarRecommender.Api.dll`:**
   - Zie je "Now listening on..."?
   - Of crasht het?
   - Deel de volledige output

2. **Test via browser terwijl handmatig draait:**
   - Werkt `https://app-carrecommender-dev.azurewebsites.net/api/health`?
   - Of nog steeds niet?

3. **Azure Portal → Log stream:**
   - Wat zie je bij startup?
   - Zijn er errors?

**Deel deze resultaten, dan kan ik precies zien wat er mis is!**


