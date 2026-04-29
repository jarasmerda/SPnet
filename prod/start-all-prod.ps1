# ============================================================
# PROD prostředí – spustí CRM, RestAPI1, RestAPI2
# Databáze: StwPh_05307970_PROD
# ============================================================

$env:ASPNETCORE_ENVIRONMENT = "Production"

$root = $PSScriptRoot

Start-Process powershell -ArgumentList "-NoExit", "-Command", "
    `$env:ASPNETCORE_ENVIRONMENT = 'Production'
    Set-Location '$root\CRM'
    Write-Host '[PROD] CRM spuštěn (port 5127)' -ForegroundColor Cyan
    dotnet run
" -WindowStyle Normal

Start-Sleep -Seconds 2

Start-Process powershell -ArgumentList "-NoExit", "-Command", "
    `$env:ASPNETCORE_ENVIRONMENT = 'Production'
    Set-Location '$root\RestAPI1'
    Write-Host '[PROD] RestAPI1 spuštěn (port 5005)' -ForegroundColor Green
    dotnet run
" -WindowStyle Normal

Start-Sleep -Seconds 2

Start-Process powershell -ArgumentList "-NoExit", "-Command", "
    `$env:ASPNETCORE_ENVIRONMENT = 'Production'
    Set-Location '$root\RestAPI2'
    Write-Host '[PROD] RestAPI2 spuštěn' -ForegroundColor Yellow
    dotnet run
" -WindowStyle Normal

Write-Host ""
Write-Host "=====================================================" -ForegroundColor Green
Write-Host "  PROD prostředí spuštěno (ASPNETCORE_ENVIRONMENT=Production)" -ForegroundColor Green
Write-Host "  Databáze: StwPh_05307970_PROD" -ForegroundColor Green
Write-Host "  CRM:      http://185.219.164.45:5127" -ForegroundColor Green
Write-Host "  RestAPI1: http://185.219.164.45:5005" -ForegroundColor Green
Write-Host "=====================================================" -ForegroundColor Green
