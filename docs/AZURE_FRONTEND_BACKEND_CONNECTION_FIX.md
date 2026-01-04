# Azure Frontend-Backend Verbinding Fix

## 🔴 Probleem
De frontend in Azure kan de backend API niet vinden. Backend geeft 500.37 error.

## ✅ Oplossing 1: Frontend Configuratie (GEDAAN)

### appsettings.json bijgewerkt
De `appsettings.json` is bijgewerkt met de juiste Azure backend URL:
```json
{
  "ApiSettings": {
    "BaseUrl": "https://app-carrecommender-dev-dxfgd4csg4ekaxgs.francecentral-01.azurewebsites.net"
  }
}
```

## ✅ Oplossing 2: Azure App Settings (AANBEVOLEN)

Voor extra zekerheid, configureer de API URL ook via Azure Portal:

1. **Azure Portal** → **App Services** → **`pp-carrecommender-web-dev`**
2. **Configuration** → **Application settings**
3. Voeg nieuwe setting toe:
   - **Name:** `ApiSettings__BaseUrl` (let op: dubbele underscore!)
   - **Value:** `https://app-carrecommender-dev-dxfgd4csg4ekaxgs.francecentral-01.azurewebsites.net`
4. Klik **Save** en wacht tot app herstart

## 🔴 Backend 500.37 Error - Diagnose

De backend geeft een 500.37 error, wat betekent dat de ASP.NET Core applicatie niet start.

### Mogelijke Oorzaken:
1. **Verkeerde .NET versie** - Azure moet .NET 8.0 hebben
2. **Missing dependencies** - Runtime dependencies ontbreken
3. **Missing data files** - CSV bestand ontbreekt in `data/` folder
4. **Configuratie probleem** - web.config of appsettings.json probleem

### Diagnose Stappen:

#### Stap 1: Check .NET Version
1. **Azure Portal** → **App Services** → **`app-carrecommender-dev`**
2. **Configuration** → **General settings**
3. Check **".NET Version"**:
   - ✅ Moet **`8.0`** of **`8.x`** zijn
   - ❌ Als **`9.0`** of leeg: Wijzig naar **`8.0`** en klik **Save**
4. **Herstart** App Service

#### Stap 2: Check Application Logs
1. **Azure Portal** → **App Services** → **`app-carrecommender-dev`**
2. **Log stream** (in menu links)
3. Kijk naar startup errors

#### Stap 3: Check Data Files (via Kudu)
1. Ga naar: `https://app-carrecommender-dev.scm.azurewebsites.net`
2. **Debug console** → **CMD**
3. Navigeer naar `site/wwwroot/data`
4. Check of CSV bestand aanwezig is:
   ```cmd
   cd site\wwwroot\data
   dir
   ```
5. **Verwacht:** `df_master_v8_def.csv` of vergelijkbaar CSV bestand

#### Stap 4: Test Handmatig Starten (via Kudu)
1. **Kudu Console** → **CMD**
2. Navigeer naar `site/wwwroot`
3. Test handmatig starten:
   ```cmd
   cd site\wwwroot
   dotnet CarRecommender.Api.dll
   ```
4. **Als het werkt:** Je ziet "Now listening on: http://localhost:5000"
5. **Als het crasht:** Deel de exacte error

## 📋 Checklist

- [x] Frontend `appsettings.json` bijgewerkt met Azure backend URL
- [ ] Azure App Settings geconfigureerd voor frontend (`ApiSettings__BaseUrl`)
- [ ] Backend .NET versie gecontroleerd (moet 8.0 zijn)
- [ ] Backend application logs gecontroleerd voor errors
- [ ] Data files aanwezig in backend `data/` folder
- [ ] Backend handmatig getest via Kudu
- [ ] Frontend kan backend API aanroepen (test via browser console)

## 🔗 URLs

- **Frontend:** `https://pp-carrecommender-web-dev-gaaehxe3hahvejah.francecentral-01.azurewebsites.net`
- **Backend:** `https://app-carrecommender-dev-dxfgd4csg4ekaxgs.francecentral-01.azurewebsites.net`
- **Backend Health:** `https://app-carrecommender-dev-dxfgd4csg4ekaxgs.francecentral-01.azurewebsites.net/api/health`

## 🚀 Volgende Stappen

1. **Deploy de frontend wijzigingen** (appsettings.json update)
2. **Diagnoseer backend 500.37 error** (volg diagnose stappen hierboven)
3. **Test verbinding** tussen frontend en backend
4. **Configureer Azure App Settings** voor extra zekerheid


