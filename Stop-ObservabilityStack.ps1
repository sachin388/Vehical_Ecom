# Stop-ObservabilityStack.ps1
# Stops the Elasticsearch, Kibana, and Elastic APM containers

Write-Host "Stopping Vehicle E-Commerce Observability Stack..." -ForegroundColor Cyan
docker compose -f docker-compose.observability.yml down
Write-Host "Observability Stack stopped." -ForegroundColor Green
