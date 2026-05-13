# ============================================================
# DEV prostředí – spustí PORTAL, CRM, RestAPI1, PLANNING
# Databáze: Smerco_05307970_2025_DEV
# ============================================================

$env:ASPNETCORE_ENVIRONMENT = "Development"

$root = $PSScriptRoot

Start-Process powershell -ArgumentList "-NoExit", "-Command", "
    `$env:ASPNETCORE_ENVIRONMENT = 'Development'
    Set-Location '$root\PORTAL'
    Write-Host '[DEV] PORTAL spuštěn (port 6003)' -ForegroundColor White
    dotnet run --urls 'http://0.0.0.0:6003'
" -WindowStyle Normal

Start-Sleep -Seconds 2

Start-Process powershell -ArgumentList "-NoExit", "-Command", "
    `$env:ASPNETCORE_ENVIRONMENT = 'Development'
    Set-Location '$root\CRM'
    Write-Host '[DEV] CRM spuštěn (port 6127)' -ForegroundColor Cyan
    dotnet run --urls 'http://0.0.0.0:6127'
" -WindowStyle Normal

Start-Sleep -Seconds 2

Start-Process powershell -ArgumentList "-NoExit", "-Command", "
    `$env:ASPNETCORE_ENVIRONMENT = 'Development'
    Set-Location '$root\RestAPI1'
    Write-Host '[DEV] RestAPI1 spuštěn (port 6005)' -ForegroundColor Green
    dotnet run --urls 'http://0.0.0.0:6005'
" -WindowStyle Normal

Start-Sleep -Seconds 2

Start-Process powershell -ArgumentList "-NoExit", "-Command", "
    `$env:ASPNETCORE_ENVIRONMENT = 'Development'
    Set-Location '$root\PLANNING'
    Write-Host '[DEV] PLANNING spuštěn (port 6010)' -ForegroundColor Magenta
    dotnet run --urls 'http://0.0.0.0:6010'
" -WindowStyle Normal

Write-Host ""
Write-Host "=====================================================" -ForegroundColor Cyan
Write-Host "  DEV prostředí spuštěno (ASPNETCORE_ENVIRONMENT=Development)" -ForegroundColor Cyan
Write-Host "  Databáze: Smerco_05307970_2025_DEV" -ForegroundColor Cyan
Write-Host "  PORTAL:   http://192.168.1.55:6003" -ForegroundColor Cyan
Write-Host "  CRM:      http://192.168.1.55:6127" -ForegroundColor Cyan
Write-Host "  RestAPI1: http://192.168.1.55:6005" -ForegroundColor Cyan
Write-Host "  PLANNING: http://192.168.1.55:6010" -ForegroundColor Cyan
Write-Host "=====================================================" -ForegroundColor Cyan
