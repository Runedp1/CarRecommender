# Hoe de applicatie te starten

## Stap 1: Open PowerShell in de juiste directory

Navigeer naar het project:
```powershell
cd "C:\Users\runed\OneDrive - Thomas More\Recommendation System\CarRecommender.Api\CarRecommender.Web"
```

## Stap 2: Start de applicatie

```powershell
dotnet run
```

## Stap 3: Wacht op de output

Je zou moeten zien:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:7158
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5227
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

## Stap 4: Open in browser

- **HTTPS**: https://localhost:7158
- **HTTP**: http://localhost:5227

## Als je ERR_CONNECTION_REFUSED krijgt:

1. **Controleer of de server draait**: Je moet de "Now listening on" berichten zien
2. **Controleer de poort**: Misschien gebruikt een andere applicatie de poort
3. **Probeer een andere poort**: Pas `launchSettings.json` aan of gebruik:
   ```powershell
   dotnet run --urls "http://localhost:5000"
   ```

## Troubleshooting

### Poort al in gebruik?
```powershell
# Check welke poort wordt gebruikt
netstat -ano | findstr :7158
netstat -ano | findstr :5227
```

### Start met specifieke poort:
```powershell
dotnet run --urls "http://localhost:5000;https://localhost:5001"
```

### Check voor errors:
Als je errors ziet in de output, deel deze dan zodat we kunnen helpen!


