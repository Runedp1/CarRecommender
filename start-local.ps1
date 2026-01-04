# Script om backend en frontend lokaal te starten
# Gebruik: .\start-local.ps1

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Car Recommender - Lokaal Opstarten" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Controleer of .NET 8.0 SDK geïnstalleerd is
Write-Host "Controleren .NET 8.0 SDK..." -ForegroundColor Yellow
$dotnetVersion = dotnet --version 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "Fout: .NET SDK niet gevonden. Installeer .NET 8.0 SDK eerst." -ForegroundColor Red
    exit 1
}
Write-Host "✓ .NET SDK gevonden: $dotnetVersion" -ForegroundColor Green
Write-Host ""

# Controleer of de projecten bestaan
$backendPath = "backend\CarRecommender.Api\CarRecommender.Api.csproj"
$frontendPath = "frontend\CarRecommender.Web\CarRecommender.Web.csproj"

if (-not (Test-Path $backendPath)) {
    Write-Host "Fout: Backend project niet gevonden op: $backendPath" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path $frontendPath)) {
    Write-Host "Fout: Frontend project niet gevonden op: $frontendPath" -ForegroundColor Red
    exit 1
}

Write-Host "✓ Projecten gevonden" -ForegroundColor Green
Write-Host ""

# Start backend in nieuwe terminal
Write-Host "Starten backend API (poort 5283)..." -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$PWD'; dotnet run --project $backendPath"
Start-Sleep -Seconds 3

# Start frontend in nieuwe terminal
Write-Host "Starten frontend Web (poort 7000)..." -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$PWD'; dotnet run --project $frontendPath"
Start-Sleep -Seconds 2

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "✓ Beide projecten zijn gestart!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Backend API:  http://localhost:5283" -ForegroundColor Cyan
Write-Host "             Swagger: http://localhost:5283/swagger" -ForegroundColor Cyan
Write-Host ""
Write-Host "Frontend Web: http://localhost:7000" -ForegroundColor Cyan
Write-Host ""
Write-Host "Druk op Ctrl+C in de terminal vensters om te stoppen." -ForegroundColor Yellow
Write-Host ""


