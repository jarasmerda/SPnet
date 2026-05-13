# ============================================================
# PROD prostředí – spustí PORTAL, CRM, RestAPI1, PLANNING
# Databáze: Smerco_05307970_2025
# ============================================================

$env:ASPNETCORE_ENVIRONMENT = "Production"

$root = $PSScriptRoot

Start-Process powershell -ArgumentList "-NoExit", "-Command", "
    `$env:ASPNETCORE_ENVIRONMENT = 'Production'
    Set-Location '$root\PORTAL'
    Write-Host '[PROD] PORTAL spuštěn (port 5000)' -ForegroundColor White
    dotnet run --no-launch-profile --urls 'http://0.0.0.0:80'
" -WindowStyle Normal

Start-Sleep -Seconds 2

Start-Process powershell -ArgumentList "-NoExit", "-Command", "
    `$env:ASPNETCORE_ENVIRONMENT = 'Production'
    Set-Location '$root\CRM'
    Write-Host '[PROD] CRM spuštěn (port 5127)' -ForegroundColor Yellow
    dotnet run --no-launch-profile --urls 'http://0.0.0.0:5127'
" -WindowStyle Normal

Start-Sleep -Seconds 2

Start-Process powershell -ArgumentList "-NoExit", "-Command", "
    `$env:ASPNETCORE_ENVIRONMENT = 'Production'
    Set-Location '$root\RestAPI1'
    Write-Host '[PROD] RestAPI1 spuštěn (port 5005)' -ForegroundColor Red
    dotnet run --no-launch-profile --urls 'http://0.0.0.0:5005'
" -WindowStyle Normal

Start-Sleep -Seconds 2

Start-Process powershell -ArgumentList "-NoExit", "-Command", "
    `$env:ASPNETCORE_ENVIRONMENT = 'Production'
    Set-Location '$root\PLANNING'
    Write-Host '[PROD] PLANNING spuštěn (port 5010)' -ForegroundColor Magenta
    dotnet run --no-launch-profile --urls 'http://0.0.0.0:5010'
" -WindowStyle Normal

Write-Host ""
Write-Host "=====================================================" -ForegroundColor Yellow
Write-Host "  PROD prostředí spuštěno (ASPNETCORE_ENVIRONMENT=Production)" -ForegroundColor Yellow
Write-Host "  Databáze: Smerco_05307970_2025" -ForegroundColor Yellow
Write-Host "  PORTAL:   http://dotnet.smerco.cz" -ForegroundColor Yellow
Write-Host "  CRM:      http://dotnet.smerco.cz:5127" -ForegroundColor Yellow
Write-Host "  RestAPI1: http://dotnet.smerco.cz:5005" -ForegroundColor Yellow
Write-Host "  PLANNING: http://dotnet.smerco.cz:5010" -ForegroundColor Yellow
Write-Host "=====================================================" -ForegroundColor Yellow
