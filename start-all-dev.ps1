# ============================================================
# DEV prostředí – spustí CRM, RestAPI1
# Databáze: Smerco_05307970_2025_DEV
# ============================================================

$env:ASPNETCORE_ENVIRONMENT = "Development"

$root = $PSScriptRoot

Start-Process powershell -ArgumentList "-NoExit", "-Command", "
    `$env:ASPNETCORE_ENVIRONMENT = 'Development'
    Set-Location '$root\CRM'
    Write-Host '[DEV] CRM spuštěn (port 6127)' -ForegroundColor Cyan
    dotnet run
" -WindowStyle Normal

Start-Sleep -Seconds 2

Start-Process powershell -ArgumentList "-NoExit", "-Command", "
    `$env:ASPNETCORE_ENVIRONMENT = 'Development'
    Set-Location '$root\RestAPI1'
    Write-Host '[DEV] RestAPI1 spuštěn (port 6006)' -ForegroundColor Green
    dotnet run
" -WindowStyle Normal

Write-Host ""
Write-Host "=====================================================" -ForegroundColor Cyan
Write-Host "  DEV prostředí spuštěno (ASPNETCORE_ENVIRONMENT=Development)" -ForegroundColor Cyan
Write-Host "  Databáze: Smerco_05307970_2025_DEV" -ForegroundColor Cyan
Write-Host "  CRM:      http://localhost:6127" -ForegroundColor Cyan
Write-Host "  RestAPI1: http://localhost:6006" -ForegroundColor Cyan
Write-Host "=====================================================" -ForegroundColor Cyan
