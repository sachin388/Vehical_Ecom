# ==================================================================================
# Vehicle E-Commerce Platform - Automated Database Deployment & Seed Runner
# Description: Executes all 9 microservice database scripts in dependency order.
# Each table is initialized with schema and 1,000 realistic records.
# ==================================================================================

param (
    [string]$Server = "localhost",
    [string]$Username = "",
    [string]$Password = ""
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

Write-Host "==================================================================" -ForegroundColor Cyan
Write-Host " Vehicle E-Commerce Platform: Database Initialization & Seeding" -ForegroundColor Cyan
Write-Host " Server: $Server" -ForegroundColor Cyan
Write-Host "==================================================================" -ForegroundColor Cyan

$scripts = @(
    "01_AuthService.sql",
    "02_ItemsService.sql",
    "03_ItemsPriceService.sql",
    "04_ItemsPriceBreakupService.sql",
    "05_AccessoryService.sql",
    "06_SalesOrderService.sql",
    "07_DealerService.sql",
    "08_PaymentService.sql",
    "09_NotificationService.sql"
)

foreach ($script in $scripts) {
    $fullPath = Join-Path $scriptDir $script
    if (-not (Test-Path $fullPath)) {
        Write-Error "Script not found: $fullPath"
    }

    Write-Host "`n>>> Running: $script ..." -ForegroundColor Yellow

    if ([string]::IsNullOrWhiteSpace($Username)) {
        # Windows Authentication (Trusted Connection)
        sqlcmd -S $Server -i $fullPath -b
    } else {
        # SQL Authentication
        sqlcmd -S $Server -U $Username -P $Password -i $fullPath -b
    }

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Execution failed for $script with exit code $LASTEXITCODE"
    }
    Write-Host ">>> Completed: $script successfully." -ForegroundColor Green
}

Write-Host "`n==================================================================" -ForegroundColor Cyan
Write-Host " All 9 services successfully provisioned with 1,000 records/table!" -ForegroundColor Cyan
Write-Host "==================================================================" -ForegroundColor Cyan
