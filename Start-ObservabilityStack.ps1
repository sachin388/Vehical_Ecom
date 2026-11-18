# Start-ObservabilityStack.ps1
# Launches Elasticsearch, Kibana, and Elastic APM Server via Docker Compose

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " Starting Vehicle E-Commerce Observability Stack (Elastic)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Verify Docker Engine Availability
$dockerRunning = $false
try {
    $null = docker info 2>&1
    if ($LASTEXITCODE -eq 0) {
        $dockerRunning = $true
    }
} catch {
    $dockerRunning = $false
}

if (-not $dockerRunning) {
    Write-Warning "Docker daemon is not currently accessible or running."
    Write-Host "Please start Docker Desktop and run this script again." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Docker Compose file: docker-compose.observability.yml" -ForegroundColor DarkGray
    exit 1
}

# 2. Launch Containers via Docker Compose
Write-Host "[1/4] Launching Elasticsearch, Kibana, and Elastic APM containers..." -ForegroundColor Green
docker compose -f docker-compose.observability.yml up -d

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to launch Docker Compose stack."
    exit 1
}

# 3. Wait for Elasticsearch Health
Write-Host "[2/4] Waiting for Elasticsearch cluster health on http://localhost:9200..." -ForegroundColor Yellow
$esReady = $false
for ($i = 0; $i -lt 30; $i++) {
    try {
        $response = Invoke-RestMethod -Uri "http://localhost:9200/_cluster/health" -Method Get -TimeoutSec 3 -ErrorAction SilentlyContinue
        if ($response -and ($response.status -eq "green" -or $response.status -eq "yellow")) {
            $esReady = $true
            break
        }
    } catch { }
    Start-Sleep -Seconds 2
}

if ($esReady) {
    Write-Host "[OK] Elasticsearch is healthy and accepting queries!" -ForegroundColor Green
} else {
    Write-Warning "Elasticsearch is taking longer than expected to initialize. Check logs: docker logs vehicalecom-elasticsearch"
}

# 4. Wait for Elastic APM Server OTLP endpoint
Write-Host "[3/4] Waiting for Elastic APM Server on http://localhost:8200..." -ForegroundColor Yellow
$apmReady = $false
for ($i = 0; $i -lt 30; $i++) {
    try {
        $apmResponse = Invoke-WebRequest -Uri "http://localhost:8200/" -Method Get -TimeoutSec 3 -UseBasicParsing -ErrorAction SilentlyContinue
        if ($apmResponse.StatusCode -eq 200) {
            $apmReady = $true
            break
        }
    } catch { }
    Start-Sleep -Seconds 2
}

if ($apmReady) {
    Write-Host "[OK] Elastic APM Server is online and accepting OpenTelemetry traces!" -ForegroundColor Green
} else {
    Write-Warning "Elastic APM Server still initializing. Check logs: docker logs vehicalecom-apm-server"
}

# 5. Summary & Links
Write-Host ""
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " Observability Endpoints & Dashboards" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " - Kibana APM Dashboard:    http://localhost:5601/app/apm" -ForegroundColor White
Write-Host " - Kibana Home:             http://localhost:5601" -ForegroundColor White
Write-Host " - Elasticsearch REST API:  http://localhost:9200" -ForegroundColor White
Write-Host " - Elastic APM OTLP (HTTP): http://localhost:8200" -ForegroundColor White
Write-Host " - OpenTelemetry gRPC:      localhost:4317" -ForegroundColor White
Write-Host " - OpenTelemetry HTTP:      http://localhost:4318" -ForegroundColor White
Write-Host "==========================================================" -ForegroundColor Cyan
