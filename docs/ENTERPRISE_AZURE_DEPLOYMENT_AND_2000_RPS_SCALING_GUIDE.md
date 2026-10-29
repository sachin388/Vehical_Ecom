# Vehicle E-Commerce Microservices Platform
# Enterprise Azure Deployment & High-Throughput (2,000 RPS) Sizing Manual
### Complete Production Engineering Guide: Basic Containerization to Advanced AKS, Azure Container Apps & Azure VM Scale Sets

---

## Document Control & Executive Summary

| Attribute | Details |
| :--- | :--- |
| **System** | Vehicle E-Commerce Microservices Platform (.NET 8 / React Vite / Kafka / Redis / Azure SQL) |
| **Target Workload Throughput** | **2,000 Requests Per Second (RPS)** Peak Sustained Traffic |
| **Target Latency SLA** | P50 < 25 ms, P95 < 50 ms, P99 < 100 ms at 2,000 RPS |
| **Deployment Options Covered** | **Option A: AKS (Azure Kubernetes Service)** (Recommended) <br> **Option B: Azure Container Apps (ACA)** <br> **Option C: Azure Virtual Machine Scale Sets (VMSS)** |
| **Document Classification** | Enterprise Production Architecture, Deployment Runbook & Capacity Model |
| **Version** | 1.0.0 (Production Release) |

---

## Table of Contents

1. [Architectural Overview & 2,000 RPS Capacity Planning](#1-architectural-overview--2000-rps-capacity-planning)
   - 1.1 Microservices Architecture & Traffic Budget Breakdown
   - 1.2 Mathematical Sizing & Concurrency Derivation for 2,000 RPS
   - 1.3 Deployment Technology Comparison Matrix (AKS vs. ACA vs. VMSS)
   - 1.4 Recommended Cloud Bill of Materials (BoM)
2. [Component-by-Component Infrastructure Sizing](#2-component-by-component-infrastructure-sizing)
   - 2.1 Microservice Pod Replicas & Compute Allocations
   - 2.2 Database Tier Sizing (Azure SQL Database Elastic Pool)
   - 2.3 Distributed Cache Sizing (Azure Cache for Redis Premium)
   - 2.4 Event Bus Sizing (Azure Event Hubs Kafka Protocol)
   - 2.5 Ingress & Web Application Firewall (WAF) Sizing
3. [Phase 1: Basic — Containerization & Image Engineering](#3-phase-1-basic--containerization--image-engineering)
   - 3.1 Production Multi-Stage Dockerfile for .NET 8 Microservices
   - 3.2 Production Dockerfile for React SPA (`vehicle-ecommerce-ui`) with Nginx
   - 3.3 Azure Container Registry (ACR) Setup & Automated Build Pipelines
   - 3.4 Local Multi-Container Sanity Verification (`docker-compose.prod.yml`)
4. [Phase 2: Cloud Foundation Provisioning (Azure Infrastructure Automation)](#4-phase-2-cloud-foundation-provisioning-azure-infrastructure-automation)
   - 4.1 Hub-and-Spoke Virtual Network (VNet) Topology
   - 4.2 Automated Azure CLI Foundation Provisioning Script
   - 4.3 Zero-Trust Private Endpoints & Key Vault Integration
5. [Phase 3: Option A (Recommended) — Azure Kubernetes Service (AKS) Deployment](#5-phase-3-option-a-recommended--azure-kubernetes-service-aks-deployment)
   - 5.1 Enterprise AKS Cluster Provisioning with Azure CNI Overlay
   - 5.2 Workload Identity & Azure Key Vault CSI Secret Store
   - 5.3 Production Kubernetes Manifests (Deployments, Services, ConfigMaps)
   - 5.4 Horizontal Pod Autoscaler (HPA) & Pod Disruption Budgets (PDB)
   - 5.5 Ingress-NGINX Controller & App Gateway (AGIC) with Let's Encrypt / Managed TLS
6. [Phase 4: Option B — Azure Container Apps (ACA) Deployment](#6-phase-4-option-b--azure-container-apps-aca-deployment)
   - 6.1 Container Apps Dedicated Workload Profile Setup
   - 6.2 Microservices Deployment & KEDA HTTP Scaling Rules
   - 6.3 Pros, Cons & Operational Trade-offs at 2,000 RPS
7. [Phase 5: Option C — Azure Virtual Machine Scale Sets (VMSS) Deployment](#7-phase-5-option-c--azure-virtual-machine-scale-sets-vmss-deployment)
   - 7.1 VMSS Architecture & Application Gateway Integration
   - 7.2 Cloud-Init Automated VM Node Provisioning Script
   - 7.3 Systemd Services & Nginx Reverse Proxy Configuration
8. [Phase 6: Advanced — 2,000 RPS Performance Tuning & Production Hardening](#8-phase-6-advanced--2000-rps-performance-tuning--production-hardening)
   - 8.1 .NET 8 Runtime & Kestrel Engine Optimization
   - 8.2 Entity Framework Core 8 & SQL Server Connection Pool Tuning
   - 8.3 Redis Connection Multiplexing & Stampede Protection
   - 8.4 Apache Kafka High-Throughput Producer & Consumer Tuning
   - 8.5 Zero-Downtime Blue/Green and Canary Deployment Strategies
9. [Phase 7: Enterprise Observability, Verification & Site Reliability](#9-phase-7-enterprise-observability-verification--site-reliability)
   - 9.1 OpenTelemetry Distributed Tracing & Azure Monitor Integration
   - 9.2 Prometheus & Grafana High-Load Dashboard Metrics
   - 9.3 2,000 RPS Automated Load Testing Script (`load-test-2000rps.js` with k6)
   - 9.4 Disaster Recovery (DR), Multi-Region Failover & Operational Runbook

---

## 1. Architectural Overview & 2,000 RPS Capacity Planning

### 1.1 Microservices Architecture & Traffic Budget Breakdown

The **Vehicle E-Commerce Platform** consists of 9 distinct backend .NET 8 microservices, a React Vite SPA frontend, and distributed backing state engines (Azure SQL, Redis, Kafka). 

In an enterprise e-commerce platform processing a sustained peak of **2,000 Requests Per Second (RPS)**, user traffic follows an asymmetric distribution: the vast majority of operations are read-heavy catalog searches and pricing evaluations, followed by authenticated operations, and finally transactional write flows (cart checkout, order creation, saga orchestration, payment processing).

```
Total Sustained Workload: 2,000 Requests / Second (Peak)
 ├── Read / Browsing Traffic (70% = 1,400 RPS)
 │    ├── Items.API (Vehicle Catalog & Specs):        600 RPS (85% Redis cache hit -> 90 RPS to DB)
 │    ├── ItemsPrice.API & ItemsPriceBreakup.API:    500 RPS (75% Redis cache hit -> 125 RPS to DB)
 │    ├── Accessory.API (Add-ons & Options):          200 RPS (80% Redis cache hit -> 40 RPS to DB)
 │    └── vehicle-ecommerce-ui (Static Assets):       100 RPS (Served via Edge CDN / Nginx)
 ├── Identity & Session Traffic (20% = 400 RPS)
 │    ├── In-Memory Stateless JWT Verification:       360 RPS (0 DB calls, local cryptographic validation)
 │    └── AuthService.Api (Login, Token Refresh):      40 RPS (PBKDF2 validation, Refresh Token rotation in DB)
 └── Write / Transactional Traffic (10% = 200 RPS)
      ├── SalesOrder.API (Draft, Order Creation):     110 RPS (Transactional Outbox write + Saga initiation)
      ├── Payment.API (Payment Intents, Webhooks):     45 RPS (External gateway communication + DB updates)
      ├── Dealer.API (Stock Reservation, VIN Lock):    45 RPS (Distributed Redis Lock + DB Allocation)
      └── Async Kafka Event Propagation:              ~450 events/sec background throughput
```

---

### 1.2 Mathematical Sizing & Concurrency Derivation for 2,000 RPS

To engineer an enterprise system without under-provisioning (which causes request queuing and cascading timeouts) or over-provisioning (which wastes cloud spend), we apply **Little's Law**:

$$L = \lambda \times W$$

Where:
- $L$ = Average number of concurrent in-flight requests in the system.
- $\lambda$ = Arrival rate ($2,000 \text{ requests/second}$).
- $W$ = Average request service time (Latency).

#### Concurrency Calculation:
- **Fast Path (Redis Cache Hits & Stateless JWT Auth)**: Average latency $W_{\text{read}} = 15 \text{ ms} = 0.015 \text{ s}$ across 1,600 RPS:
  $$L_{\text{read}} = 1,600 \times 0.015 = 24 \text{ concurrent in-flight requests}$$
- **Slow Path (Database Transactions, Sagas, Outbox)**: Average latency $W_{\text{write}} = 65 \text{ ms} = 0.065 \text{ s}$ across 400 RPS:
  $$L_{\text{write}} = 400 \times 0.065 = 26 \text{ concurrent in-flight requests}$$
- **Total Concurrent In-Flight HTTP Requests**: $L_{\text{total}} \approx 50 \text{ to } 75 \text{ concurrent threads}$.

#### Network Bandwidth Requirement:
- Average payload size (request headers + response JSON): $\approx 4.5 \text{ KB}$ per request.
- Bandwidth required: 
  $$\text{Throughput} = 2,000 \text{ req/sec} \times 4.5 \text{ KB} = 9,000 \text{ KB/sec} = 72 \text{ Mbps}$$
- Provisioned headroom (accounting for burst spikes, gzip overhead, TLS handshake): **Minimum 1 Gbps networking with Accelerated Networking enabled**.

#### CPU & ThreadPool Derivation:
- A benchmarked ASP.NET Core 8 Web API running on Kestrel with asynchronous I/O (`async`/`await`) can service **350–500 RPS per vCPU** when leveraging Redis cache and pooled EF Core DbContexts.
- For write-heavy transactional operations with EF Core transactions and outbox writing, throughput is **100–150 RPS per vCPU**.
- Total aggregate compute required across all microservices:
  $$\text{Read Compute} = \frac{1,600 \text{ RPS}}{350 \text{ RPS/vCPU}} \approx 4.6 \text{ vCPUs}$$
  $$\text{Write Compute} = \frac{400 \text{ RPS}}{120 \text{ RPS/vCPU}} \approx 3.3 \text{ vCPUs}$$
  $$\text{Base Compute} \approx 8 \text{ vCPUs}$$
- Applying **Enterprise Production Multipliers**:
  - $N+2$ High Availability (tolerating node/zone failures): Multiplier $\times 2.0$.
  - Peak Traffic Headroom (handling 2x sudden traffic bursts up to 4,000 RPS): Multiplier $\times 2.0$.
  - System Daemons (Ingress, Envoy/Cilium proxy, OpenTelemetry, Log analytics, Node OS): Multiplier $\times 1.5$.
  - **Total Recommended Cluster Compute: 48 to 64 vCPUs and 192 to 256 GB RAM**.

---

### 1.3 Deployment Technology Comparison Matrix (AKS vs. ACA vs. VMSS)

| Evaluation Criteria | Option A: AKS (Azure Kubernetes Service) | Option B: Azure Container Apps (ACA) | Option C: Azure VM Scale Sets (VMSS) |
| :--- | :--- | :--- | :--- |
| **Recommendation for 2,000 RPS** | **STRONGLY RECOMMENDED (Grade A+)** | **VIABLE WITH LIMITATIONS (Grade B+)** | **NOT RECOMMENDED (Grade C)** |
| **Architectural Model** | Native Kubernetes (v1.29+), full control over Pods, Ingress, CNI, HPA, KEDA, Service Mesh. | Serverless Container environment built on managed Kubernetes + Envoy + KEDA. | Traditional Virtual Machines running containers via Docker Engine or systemd services. |
| **Autoscaling Latency** | **Fastest (5–15 seconds)** via HPA & Cluster Autoscaler pre-warmed nodes. | **Moderate (15–45 seconds)**; cold starts can introduce latency spikes at 2,000 RPS. | **Slow (3–8 minutes)** for VM provisioning, OS boot, and application initialization. |
| **Traffic Management & Ingress** | High-performance Ingress-NGINX / Cilium eBPF handling 50,000+ RPS per node with sub-ms routing. | Managed Envoy Ingress; configuration options are abstracted and restricted. | Azure Application Gateway v2 fronting VMSS; manual port routing per service. |
| **Fine-Grained Resource Tuning** | Complete access to sysctl, Linux kernel networking, TCP buffers, affinity, topology spread. | Restricted. Sandboxed container boundaries. | Full OS control, but extreme management overhead across distributed instances. |
| **Zero-Downtime Deployment** | RollingUpdate, Canary (Argo Rollouts), Blue/Green with instant traffic shifting via Ingress. | Built-in revision management and traffic splitting. | Complex custom scripts; slow rolling OS updates. |
| **Operational & Maintenance Cost** | Moderate to High expertise needed, lowest cloud cost per unit compute at sustained load. | Low operational overhead, but higher cost at continuous 2,000 RPS sustained load. | Highest operational burden (OS patching, anti-virus, security hardening on each VM). |

---

### 1.4 Recommended Cloud Bill of Materials (BoM)

For a production-grade 2,000 RPS deployment in Azure (Primary Region: East US / West Europe):

```
┌────────────────────────────────────────────────────────────────────────────────────────┐
│                        Recommended Production Infrastructure BoM                       │
├─────────────────────────────┬───────────────────────────────┬──────────────────────────┤
│ Azure Resource              │ Specification / SKU           │ Redundancy / Capacity    │
├─────────────────────────────┼───────────────────────────────┼──────────────────────────┤
│ AKS Cluster (Compute)       │ 8x Standard_D8s_v5 (User Pool)│ 64 vCPU, 256 GB RAM, AZ  │
│                             │ 3x Standard_D4s_v5 (Sys Pool) │ 12 vCPU, 48 GB RAM, AZ   │
│ Azure SQL Elastic Pool      │ Business Critical - 32 vCores │ NVMe SSD, 12.8k IOPS, HA │
│ Azure Cache for Redis       │ Premium P2 (13 GB memory)     │ Multi-Zone, 30,000 ops/s │
│ Azure Event Hubs (Kafka)    │ Premium Tier (2 PUs)          │ 12 Partitions/Topic, 99.99%│
│ Azure Application Gateway v2│ WAF v2 SKU (Autoscale 5 - 30) │ Zone-redundant, WAF CRS  │
│ Azure Container Registry    │ Premium Tier                  │ Private Link, Geo-Repl   │
│ Azure Key Vault             │ Premium Tier (HSM-backed)     │ Private Endpoint         │
│ Azure Front Door / CDN      │ Standard / Premium            │ Global Edge Caching (UI) │
└─────────────────────────────┴───────────────────────────────┴──────────────────────────┘
```

---

## 2. Component-by-Component Infrastructure Sizing

### 2.1 Microservice Pod Replicas & Compute Allocations

Under AKS, each microservice must have explicit `resources.requests` (for scheduling guarantees) and `resources.limits` (to prevent noisy-neighbor memory leaks).

```yaml
# Pod Sizing & Horizontal Pod Autoscaler (HPA) Planning Matrix for 2,000 RPS
```

| Microservice | Target RPS | Min Replicas | Max Replicas | CPU Request / Limit | Memory Request / Limit | Autoscaling Metric |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| `vehicle-ecommerce-ui` | 100 RPS | 4 | 12 | 250m / 500m | 256Mi / 512Mi | CPU > 65% |
| `AuthService.Api` | 400 RPS | 4 | 10 | 500m / 1000m | 512Mi / 1024Mi | CPU > 60% |
| `Items.API` | 600 RPS | 8 | 18 | 1000m / 2000m| 1024Mi / 2048Mi | CPU > 60% |
| `ItemsPrice.API` | 500 RPS | 6 | 14 | 500m / 1000m | 512Mi / 1024Mi | CPU > 60% |
| `ItemsPriceBreakup.API` | 500 RPS | 6 | 14 | 500m / 1000m | 512Mi / 1024Mi | CPU > 60% |
| `Accessory.API` | 200 RPS | 4 | 10 | 500m / 1000m | 512Mi / 1024Mi | CPU > 60% |
| `SalesOrder.API` | 110 RPS | 8 | 20 | 1000m / 2000m| 1024Mi / 2048Mi | CPU > 55% + KEDA |
| `Dealer.API` | 45 RPS | 4 | 10 | 500m / 1000m | 512Mi / 1024Mi | CPU > 60% |
| `Payment.API` | 45 RPS | 4 | 10 | 500m / 1000m | 512Mi / 1024Mi | CPU > 60% |
| `NotificationSystem.API`| Async | 4 | 12 | 500m / 1000m | 512Mi / 1024Mi | Kafka Consumer Lag |
| **Total Base Cluster** | **2,000 RPS**| **52 Pods** | **130 Pods** | **35.5 vCPU Req** | **45 GB RAM Req** | — |

---

### 2.2 Database Tier Sizing (Azure SQL Database Elastic Pool)

In our microservices platform, there are 9 independent logical databases:
1. `AuthDb`
2. `ItemsCatalogDb`
3. `ItemsPriceDb`
4. `ItemsPriceBreakupDb`
5. `AccessoryDb`
6. `SalesOrderDb`
7. `DealerDb`
8. `PaymentDb`
9. `NotificationDb`

#### Why an Elastic Pool?
Individual standalone databases create severe resource stranding: `ItemsCatalogDb` is read-heavy during peak browsing, while `SalesOrderDb` and `PaymentDb` experience write spikes during order checkout. An **Elastic Pool** allows all 9 databases to share a high-performance vCore and IOPS budget dynamically.

#### Selected Tier: **Business Critical (32 vCores, Gen5)**
- **Storage Type**: Local NVMe SSD storage providing sustained read/write latencies of **< 2 milliseconds**.
- **Max IOPS**: 12,800 IOPS (surpasses the ~1,200 aggregate database operations/sec required at 2,000 RPS).
- **Built-in Read Scale-Out**: Includes 1 free read-only replica. We route read queries (`GET /api/items`, `GET /api/accessories`) to the read-replica via `ApplicationIntent=ReadOnly` in connection strings, doubling read capacity.
- **Connection Limits**: 3,200 concurrent database connections.

```sql
-- Connection String Configuration for Read Scale-Out:
Server=tcp:sql-vehicalecom-prod.database.windows.net,1433;
Database=ItemsCatalogDb;
User ID=sqladmin;
Password=YourStrongSecretKey!;
Trusted_Connection=False;
Encrypt=True;
Connection Timeout=30;
ApplicationIntent=ReadOnly;  -- Directs traffic to free secondary replica
Max Pool Size=120;           -- Per pod instance
Min Pool Size=10;
```

---

### 2.3 Distributed Cache Sizing (Azure Cache for Redis Premium)

At 2,000 RPS, direct database hits for vehicle catalog and price lookups would degrade SQL performance. Redis absorbs 80% of read traffic (~1,200 queries/sec).

#### Selected Tier: **Premium Tier P2 (13 GB memory, 2 shards, multi-zone)**
- **Throughput Capacity**: Benchmarked at **30,000 to 50,000 operations per second** with sub-millisecond latency.
- **High Availability**: Zone-redundant replica deployed in independent Azure Availability Zones with automated failover.
- **Data Persistence**: AOF (Append-Only File) configured to sync every second (`appendfsync everysec`) alongside daily RDB snapshots.
- **Client Library Configuration**:
  ```csharp
  // StackExchange.Redis Production Configuration
  var configurationOptions = new ConfigurationOptions
  {
      EndPoints = { "redis-vehicalecom-prod.redis.cache.windows.net:6380" },
      Password = "PrimaryAccessKey",
      Ssl = true,
      AbortOnConnectFail = false,
      ConnectTimeout = 5000,
      SyncTimeout = 2000,
      KeepAlive = 60,
      DefaultDatabase = 0
  };
  ```

---

### 2.4 Event Bus Sizing (Azure Event Hubs Kafka Protocol)

All integration events (`salesorder.events`, `vehicle.created`, `order.cancellation.commands`) are broadcast asynchronously.

#### Selected Tier: **Azure Event Hubs Premium (2 Processing Units - PUs)**
- **Kafka 1.0+ Wire Protocol**: Zero code changes required for `Confluent.Kafka 2.15.1`. Microservices connect directly via standard Kafka bootstrap endpoints on port 9093.
- **Throughput Capacity**: Each PU provides **1 to 5 MB/sec ingress** and **2 to 10 MB/sec egress**, easily handling our ~450 events/sec (estimated at 600 KB/sec).
- **Partitioning Strategy**:
  - Every production topic must be created with **at least 12 partitions**.
  - *Critical Rule*: In Kafka, consumer parallelism within a consumer group cannot exceed the number of partitions. Having 12 partitions ensures up to 12 consumer pods can process messages concurrently without partition contention.

---

### 2.5 Ingress & Web Application Firewall (WAF) Sizing

- **Azure Application Gateway v2 (WAF v2 SKU)**:
  - Deployed in public subnet as the single public IP edge entry point.
  - **Autoscale Configuration**: `MinCapacity = 5`, `MaxCapacity = 30`. Each instance handles ~2,500 active SSL connections.
  - **OWASP CRS 3.2**: Protection against SQL Injection, Cross-Site Scripting (XSS), and HTTP Request Smuggling.
  - **Connection Reuse**: HTTP/2 enabled on client-facing edge; HTTP/1.1 keep-alive pooling to internal AKS Ingress-NGINX.

---

## 3. Phase 1: Basic — Containerization & Image Engineering

### 3.1 Production Multi-Stage Dockerfile for .NET 8 Microservices

To achieve maximum throughput and security at 2,000 RPS, microservice container images must be optimized for fast startup, minimal memory footprint, and non-root execution.

Save this template as `Dockerfile.service` in the repository root:

```dockerfile
# =======================================================================
# STAGE 1: Build & Restore with Dependency Optimization
# =======================================================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Arguments to specify the target project path
ARG PROJECT_PATH=Services/SalesOrder/SalesOrder.API/SalesOrder.API.csproj
ARG SERVICE_DIR=Services/SalesOrder

# Copy only project files first to leverage Docker layer caching
COPY AuthService/*/*.csproj ./AuthService/
COPY Services/Items/*/*.csproj ./Services/Items/
COPY Services/ItemsPrice/*/*.csproj ./Services/ItemsPrice/
COPY Services/ItemsPriceBreakup/*/*.csproj ./Services/ItemsPriceBreakup/
COPY Services/Accessory/*/*.csproj ./Services/Accessory/
COPY Services/Dealer/*/*.csproj ./Services/Dealer/
COPY Services/Payment/*/*.csproj ./Services/Payment/
COPY Services/SalesOrder/*/*.csproj ./Services/SalesOrder/
COPY Services/NotificationSystem/*/*.csproj ./Services/NotificationSystem/

# Restore dependencies
RUN dotnet restore ${PROJECT_PATH} --runtime linux-x64

# Copy entire source tree
COPY . .

# Build and publish release binaries with trimming and optimizations
RUN dotnet publish ${PROJECT_PATH} \
    -c Release \
    -r linux-x64 \
    --no-restore \
    -o /app/publish \
    /p:UseAppHost=false

# =======================================================================
# STAGE 2: Ultra-Lean Production Runtime
# =======================================================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
WORKDIR /app

# Install native dependencies required for Confluent.Kafka (librdkafka) on Alpine
RUN apk add --no-cache libc6-compat icu-libs

# Configure environment variables for high performance
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_EnableDiagnostics=0 \
    DOTNET_gcServer=1 \
    DOTNET_gcConcurrent=1

# Create non-root system user for security compliance
RUN addgroup -S appgroup && adduser -S appuser -G appgroup
USER appuser

# Copy published artifacts
COPY --from=build --chown=appuser:appgroup /app/publish .

# Expose non-privileged HTTP port
EXPOSE 8080

ENTRYPOINT ["dotnet", "SalesOrder.API.dll"]
```

---

### 3.2 Production Dockerfile for React SPA (`vehicle-ecommerce-ui`) with Nginx

The React frontend must be compiled with Vite and served through an optimized Nginx server capable of gzip/brotli compression and sub-millisecond static file delivery.

Save as `vehicle-ecommerce-ui/Dockerfile`:

```dockerfile
# =======================================================================
# STAGE 1: Compile React TypeScript SPA
# =======================================================================
FROM node:18-alpine AS builder
WORKDIR /app

# Cache package installation
COPY package*.json ./
RUN npm ci

# Copy source and build
COPY . .
RUN npm run build

# =======================================================================
# STAGE 2: High-Performance Nginx Alpine Server
# =======================================================================
FROM nginx:1.25-alpine AS runner

# Remove default configuration
RUN rm -rf /etc/nginx/conf.d/*

# Copy custom enterprise Nginx configuration
COPY <<'EOF' /etc/nginx/conf.d/default.conf
server {
    listen 80;
    server_name _;
    root /usr/share/nginx/html;
    index index.html;

    # Gzip compression for fast loading
    gzip on;
    gzip_vary on;
    gzip_min_length 1024;
    gzip_proxied expired no-cache no-store private auth;
    gzip_types text/plain text/css text/xml text/javascript application/x-javascript application/xml application/javascript;

    # Security Headers
    add_header X-Frame-Options "SAMEORIGIN" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header X-XSS-Protection "1; mode=block" always;

    # SPA Routing: Any route fallback to index.html
    location / {
        try_files $uri $uri/ /index.html;
    }

    # Cache static assets for 1 year
    location ~* \.(js|css|png|jpg|jpeg|gif|svg|ico|woff|woff2)$ {
        expires 1y;
        add_header Cache-Control "public, immutable";
    }

    # Health check endpoint for Kubernetes
    location /healthz {
        access_log off;
        return 200 "OK\n";
    }
}
EOF

# Copy static assets from builder stage
COPY --from=builder /app/dist /usr/share/nginx/html

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
```

---

### 3.3 Azure Container Registry (ACR) Setup & Automated Build Pipelines

```bash
#!/bin/bash
# =======================================================================
# Automated Build and Push Script for All Microservices
# =======================================================================
set -e

RESOURCE_GROUP="rg-vehicalecom-prod"
ACR_NAME="acrvehicalecomprod"
LOCATION="eastus"
TAG="v1.0.0"

echo "[1/3] Creating Premium Azure Container Registry..."
az acr create \
  --resource-group $RESOURCE_GROUP \
  --name $ACR_NAME \
  --sku Premium \
  --admin-enabled false \
  --location $LOCATION

# Log in to ACR
az acr login --name $ACR_NAME

echo "[2/3] Building and Pushing React Frontend..."
az acr build \
  --registry $ACR_NAME \
  --image vehicle-ecommerce-ui:$TAG \
  --image vehicle-ecommerce-ui:latest \
  ./vehicle-ecommerce-ui

echo "[3/3] Building and Pushing .NET Microservices..."
SERVICES=(
  "AuthService:AuthService/AuthService.Api/AuthService.Api.csproj"
  "items-service:Services/Items/Items.API/Items.API.csproj"
  "itemsprice-service:Services/ItemsPrice/ItemsPrice.API/ItemsPrice.API.csproj"
  "itemspricebreakup-service:Services/ItemsPriceBreakup/ItemsPriceBreakup.API/ItemsPriceBreakup.API.csproj"
  "accessory-service:Services/Accessory/Accessory.API/Accessory.API.csproj"
  "dealer-service:Services/Dealer/Dealer.API/Dealer.API.csproj"
  "salesorder-service:Services/SalesOrder/SalesOrder.API/SalesOrder.API.csproj"
  "payment-service:Services/Payment/Payment.API/Payment.API.csproj"
  "notification-service:Services/NotificationSystem/NotificationSystem.API/NotificationSystem.API.csproj"
)

for ENTRY in "${SERVICES[@]}"; do
  NAME="${ENTRY%%:*}"
  PROJECT="${ENTRY##*:}"
  echo ">>> Building $NAME ($PROJECT)..."
  az acr build \
    --registry $ACR_NAME \
    --image ${NAME}:${TAG} \
    --image ${NAME}:latest \
    --build-arg PROJECT_PATH=${PROJECT} \
    -f Dockerfile.service .
done

echo ">>> All container images successfully built and pushed to $ACR_NAME.azurecr.io!"
```

---

## 4. Phase 2: Cloud Foundation Provisioning (Azure Infrastructure Automation)

### 4.1 Hub-and-Spoke Virtual Network (VNet) Topology

To ensure zero-trust isolation and high network performance:
- **VNet Address Space**: `10.240.0.0/16`
  - Subnet `snet-aks-nodes`: `10.240.0.0/20` (Supports up to 4,096 IP addresses for Azure CNI Overlay).
  - Subnet `snet-appgw`: `10.240.16.0/24` (Dedicated to Application Gateway v2).
  - Subnet `snet-private-endpoints`: `10.240.17.0/24` (For SQL, Redis, Key Vault, and Event Hubs private IPs).
  - Subnet `snet-bastion`: `10.240.18.0/26` (For secure operational jumpbox).

---

### 4.2 Automated Azure CLI Foundation Provisioning Script

```bash
#!/bin/bash
# =======================================================================
# Complete Foundation Infrastructure Deployment Script
# =======================================================================
set -e

RG="rg-vehicalecom-prod"
LOC="eastus"
VNET_NAME="vnet-vehicalecom-prod"
SQL_SERVER="sql-vehicalecom-prod"
SQL_POOL="pool-vehicalecom-prod"
REDIS_NAME="redis-vehicalecom-prod"
EVENTHUBS_NS="eh-vehicalecom-prod"
KEYVAULT_NAME="kv-vehicalecom-prod"

echo "[1/7] Creating Resource Group..."
az group create --name $RG --location $LOC

echo "[2/7] Creating Virtual Network and Subnets..."
az network vnet create \
  --resource-group $RG \
  --name $VNET_NAME \
  --address-prefixes 10.240.0.0/16 \
  --subnet-name snet-aks-nodes \
  --subnet-prefixes 10.240.0.0/20

az network vnet subnet create \
  --resource-group $RG \
  --vnet-name $VNET_NAME \
  --name snet-appgw \
  --address-prefixes 10.240.16.0/24

az network vnet subnet create \
  --resource-group $RG \
  --vnet-name $VNET_NAME \
  --name snet-private-endpoints \
  --address-prefixes 10.240.17.0/24

echo "[3/7] Provisioning Azure Key Vault..."
az keyvault create \
  --name $KEYVAULT_NAME \
  --resource-group $RG \
  --location $LOC \
  --enable-rbac-authorization true

echo "[4/7] Provisioning Azure SQL Server & Business Critical Elastic Pool (32 vCores)..."
az sql server create \
  --name $SQL_SERVER \
  --resource-group $RG \
  --location $LOC \
  --admin-user "sqladmin" \
  --admin-password "P@ssw0rdVehical2026!Secure"

az sql elastic-pool create \
  --resource-group $RG \
  --server $SQL_SERVER \
  --name $SQL_POOL \
  --edition BusinessCritical \
  --family Gen5 \
  --capacity 32 \
  --zone-redundant true

# Create the 9 microservice databases inside the elastic pool
DATABASES=("AuthDb" "ItemsCatalogDb" "ItemsPriceDb" "ItemsPriceBreakupDb" "AccessoryDb" "SalesOrderDb" "DealerDb" "PaymentDb" "NotificationDb")
for DB in "${DATABASES[@]}"; do
  az sql db create \
    --resource-group $RG \
    --server $SQL_SERVER \
    --name $DB \
    --elastic-pool $SQL_POOL
done

echo "[5/7] Provisioning Azure Cache for Redis Premium P2..."
az redis create \
  --resource-group $RG \
  --name $REDIS_NAME \
  --location $LOC \
  --sku Premium \
  --vm-size P2 \
  --enable-non-ssl-port false \
  --zones 1 2 3

echo "[6/7] Provisioning Azure Event Hubs (Premium Kafka Protocol)..."
az eventhubs namespace create \
  --resource-group $RG \
  --name $EVENTHUBS_NS \
  --location $LOC \
  --sku Premium \
  --capacity 2 \
  --zone-redundant true

# Create Kafka topics with 12 partitions each for 2,000 RPS concurrency
TOPICS=("vehicle.created" "salesorder.events" "order.cancellation.commands" "salesorder.events.dlq")
for TOPIC in "${TOPICS[@]}"; do
  az eventhubs eventhub create \
    --resource-group $RG \
    --namespace-name $EVENTHUBS_NS \
    --name $TOPIC \
    --partition-count 12
done

echo "[7/7] Foundation Infrastructure Provisioning Complete!"
```

---

## 5. Phase 3: Option A (Recommended) — Azure Kubernetes Service (AKS) Deployment

### 5.1 Enterprise AKS Cluster Provisioning with Azure CNI Overlay

For 2,000 RPS sustained throughput, the AKS cluster must leverage **Azure CNI Overlay** (eliminating VNet IP exhaustion) and **Azure Linux (CBL-Mariner)** container host for optimal kernel networking.

```bash
#!/bin/bash
# =======================================================================
# Production AKS Cluster Deployment Script
# =======================================================================
set -e

RG="rg-vehicalecom-prod"
LOC="eastus"
CLUSTER_NAME="aks-vehicalecom-prod"
VNET_NAME="vnet-vehicalecom-prod"
ACR_NAME="acrvehicalecomprod"

SUBNET_ID=$(az network vnet subnet show \
  --resource-group $RG \
  --vnet-name $VNET_NAME \
  --name snet-aks-nodes \
  --query id -o tsv)

echo "[1/4] Provisioning Production AKS Cluster..."
az aks create \
  --resource-group $RG \
  --name $CLUSTER_NAME \
  --location $LOC \
  --node-count 3 \
  --node-vm-size Standard_D4s_v5 \
  --os-sku AzureLinux \
  --network-plugin azure \
  --network-plugin-mode overlay \
  --vnet-subnet-id $SUBNET_ID \
  --enable-managed-identity \
  --enable-oidc-issuer \
  --enable-workload-identity \
  --enable-addons monitoring,azure-keyvault-secrets-provider \
  --attach-acr $ACR_NAME \
  --zones 1 2 3 \
  --uptime-sla \
  --generate-ssh-keys

echo "[2/4] Adding Dedicated High-Performance User Node Pool (Standard_D8s_v5)..."
az aks nodepool add \
  --resource-group $RG \
  --cluster-name $CLUSTER_NAME \
  --name userapps \
  --node-vm-size Standard_D8s_v5 \
  --node-count 8 \
  --min-count 6 \
  --max-count 14 \
  --enable-cluster-autoscaler \
  --zones 1 2 3 \
  --mode User \
  --labels workload=microservices

echo "[3/4] Connecting to AKS Cluster via kubectl..."
az aks get-credentials --resource-group $RG --name $CLUSTER_NAME --overwrite-existing

echo "[4/4] Verifying Cluster Nodes:"
kubectl get nodes -o wide
```

---

### 5.2 Workload Identity & Azure Key Vault CSI Secret Store

Configure Azure Key Vault integration so microservices read database passwords and JWT secrets securely without storing credentials in source control or plain text ConfigMaps.

Save as `k8s/secret-provider.yaml`:

```yaml
apiVersion: secrets-store.csi.x-k8s.io/v1
kind: SecretProviderClass
metadata:
  name: azure-kv-secrets
  namespace: vehicalecom
spec:
  provider: azure
  parameters:
    usePodIdentity: "false"
    useVMManagedIdentity: "false"
    clientID: "<USER_ASSIGNED_MANAGED_IDENTITY_CLIENT_ID>"
    keyvaultName: "kv-vehicalecom-prod"
    cloudName: ""
    objects: |
      array:
        - |
          objectName: SqlAdminPassword
          objectType: secret
        - |
          objectName: JwtSecretKey
          objectType: secret
        - |
          objectName: RedisPrimaryKey
          objectType: secret
        - |
          objectName: EventHubsConnectionString
          objectType: secret
    tenantId: "<AZURE_TENANT_ID>"
  secretObjects:
  - secretName: app-secrets
    type: Opaque
    data:
    - objectName: SqlAdminPassword
      keyName: SQL_PASSWORD
    - objectName: JwtSecretKey
      keyName: JWT_SECRET
    - objectName: RedisPrimaryKey
      keyName: REDIS_PASSWORD
    - objectName: EventHubsConnectionString
      keyName: EVENTHUBS_CONNECTION
```

---

### 5.3 Production Kubernetes Manifests (Deployments, Services, ConfigMaps)

Save as `k8s/salesorder-service.yaml` (example representing the critical transactional service):

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: salesorder-service
  namespace: vehicalecom
  labels:
    app: salesorder-service
    tier: backend
spec:
  replicas: 8
  strategy:
    type: RollingUpdate
    rollingUpdate:
      maxSurge: 25%
      maxUnavailable: 0  # Zero-downtime rolling updates
  selector:
    matchLabels:
      app: salesorder-service
  template:
    metadata:
      labels:
        app: salesorder-service
    spec:
      affinity:
        podAntiAffinity:
          preferredDuringSchedulingIgnoredDuringExecution:
          - weight: 100
            podAffinityTerm:
              labelSelector:
                matchExpressions:
                - key: app
                  operator: In
                  values:
                  - salesorder-service
              topologyKey: "topology.kubernetes.io/zone"  # Distribute pods across Availability Zones
      containers:
      - name: salesorder-service
        image: acrvehicalecomprod.azurecr.io/salesorder-service:v1.0.0
        imagePullPolicy: IfNotPresent
        ports:
        - containerPort: 8080
          name: http
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: DOTNET_gcServer
          value: "1"
        - name: ConnectionStrings__SalesOrderConnection
          value: "Server=tcp:sql-vehicalecom-prod.database.windows.net,1433;Database=SalesOrderDb;User ID=sqladmin;Password=$(SQL_PASSWORD);Encrypt=True;Max Pool Size=120;Min Pool Size=10;"
        - name: Redis__Configuration
          value: "redis-vehicalecom-prod.redis.cache.windows.net:6380,password=$(REDIS_PASSWORD),ssl=True,abortConnect=False"
        - name: Kafka__BootstrapServers
          value: "eh-vehicalecom-prod.servicebus.windows.net:9093"
        - name: Kafka__SaslPassword
          value: "$(EVENTHUBS_CONNECTION)"
        envFrom:
        - secretRef:
            name: app-secrets
        resources:
          requests:
            cpu: "1000m"
            memory: "1024Mi"
          limits:
            cpu: "2000m"
            memory: "2048Mi"
        # Probes tuned for 2,000 RPS high concurrency
        startupProbe:
          httpGet:
            path: /health/startup
            port: 8080
          initialDelaySeconds: 5
          periodSeconds: 3
          failureThreshold: 20
        readinessProbe:
          httpGet:
            path: /health/ready
            port: 8080
          periodSeconds: 5
          timeoutSeconds: 2
          successThreshold: 1
          failureThreshold: 3
        livenessProbe:
          httpGet:
            path: /health/live
            port: 8080
          periodSeconds: 10
          timeoutSeconds: 2
          failureThreshold: 3
---
apiVersion: v1
kind: Service
metadata:
  name: salesorder-service
  namespace: vehicalecom
spec:
  type: ClusterIP
  selector:
    app: salesorder-service
  ports:
  - port: 80
    targetPort: 8080
    name: http
```

---

### 5.4 Horizontal Pod Autoscaler (HPA) & Pod Disruption Budgets (PDB)

Save as `k8s/salesorder-hpa-pdb.yaml`:

```yaml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: salesorder-service-hpa
  namespace: vehicalecom
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: salesorder-service
  minReplicas: 8
  maxReplicas: 20
  metrics:
  - type: Resource
    resource:
      name: cpu
      target:
        type: Utilization
        averageUtilization: 60
  - type: Resource
    resource:
      name: memory
      target:
        type: Utilization
        averageUtilization: 75
  behavior:
    scaleUp:
      stabilizationWindowSeconds: 0
      policies:
      - type: Percent
        value: 50
        periodSeconds: 15
    scaleDown:
      stabilizationWindowSeconds: 300
      policies:
      - type: Percent
        value: 10
        periodSeconds: 60
---
apiVersion: policy/v1
kind: PodDisruptionBudget
metadata:
  name: salesorder-service-pdb
  namespace: vehicalecom
spec:
  minAvailable: 6  # Guarantees at least 6 pods remain running during cluster node drainage or OS upgrades
  selector:
    matchLabels:
      app: salesorder-service
```

---

### 5.5 Ingress-NGINX Controller & App Gateway (AGIC) with Let's Encrypt / Managed TLS

Save as `k8s/ingress-routes.yaml`:

```yaml
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: vehicalecom-ingress
  namespace: vehicalecom
  annotations:
    kubernetes.io/ingress.class: nginx
    nginx.ingress.kubernetes.io/proxy-body-size: "10m"
    nginx.ingress.kubernetes.io/proxy-connect-timeout: "5"
    nginx.ingress.kubernetes.io/proxy-read-timeout: "15"
    nginx.ingress.kubernetes.io/proxy-send-timeout: "15"
    nginx.ingress.kubernetes.io/proxy-buffering: "on"
    nginx.ingress.kubernetes.io/proxy-buffers-number: "8"
    nginx.ingress.kubernetes.io/proxy-buffer-size: "16k"
    nginx.ingress.kubernetes.io/enable-real-ip: "true"
    cert-manager.io/cluster-issuer: "letsencrypt-prod"
spec:
  tls:
  - hosts:
    - api.vehicalecom.com
    - vehicalecom.com
    secretName: vehicalecom-tls-cert
  rules:
  - host: vehicalecom.com
    http:
      paths:
      - path: /
        pathType: Prefix
        backend:
          service:
            name: vehicle-ecommerce-ui
            port:
              number: 80
  - host: api.vehicalecom.com
    http:
      paths:
      - path: /api/auth
        pathType: Prefix
        backend:
          service:
            name: auth-service
            port:
              number: 80
      - path: /api/items
        pathType: Prefix
        backend:
          service:
            name: items-service
            port:
              number: 80
      - path: /api/pricing
        pathType: Prefix
        backend:
          service:
            name: itemsprice-service
            port:
              number: 80
      - path: /api/pricebreakup
        pathType: Prefix
        backend:
          service:
            name: itemspricebreakup-service
            port:
              number: 80
      - path: /api/accessories
        pathType: Prefix
        backend:
          service:
            name: accessory-service
            port:
              number: 80
      - path: /api/dealers
        pathType: Prefix
        backend:
          service:
            name: dealer-service
            port:
              number: 80
      - path: /api/salesorders
        pathType: Prefix
        backend:
          service:
            name: salesorder-service
            port:
              number: 80
      - path: /api/payments
        pathType: Prefix
        backend:
          service:
            name: payment-service
            port:
              number: 80
```

---

## 6. Phase 4: Option B — Azure Container Apps (ACA) Deployment

Azure Container Apps (ACA) offers a serverless container environment built on managed Kubernetes. While convenient, handling **2,000 sustained RPS** requires configuring **Dedicated Workload Profiles** to prevent noisy-neighbor throttling and severe cold starts.

### 6.1 Container Apps Dedicated Workload Profile Setup

```bash
#!/bin/bash
# =======================================================================
# Deploying High-Throughput Azure Container Apps Environment
# =======================================================================
set -e

RG="rg-vehicalecom-prod"
LOC="eastus"
ENV_NAME="aca-env-vehicalecom-prod"
VNET_NAME="vnet-vehicalecom-prod"

SUBNET_ID=$(az network vnet subnet show \
  --resource-group $RG \
  --vnet-name $VNET_NAME \
  --name snet-aks-nodes \
  --query id -o tsv)

echo "[1/2] Creating Container Apps Managed Environment with Dedicated D8 Profile..."
az containerapp env create \
  --name $ENV_NAME \
  --resource-group $RG \
  --location $LOC \
  --infrastructure-subnet-resource-id $SUBNET_ID \
  --enable-workload-profiles true

# Add Dedicated compute profile
az containerapp env workload-profile add \
  --name $ENV_NAME \
  --resource-group $RG \
  --workload-profile-name "Dedicated-D8" \
  --workload-profile-type "D8" \
  --min-nodes 4 \
  --max-nodes 10
```

---

### 6.2 Microservices Deployment & KEDA HTTP Scaling Rules

Under ACA, configure KEDA HTTP concurrent request scaling so pods autoscale proactively:

```bash
echo "[2/2] Deploying SalesOrder Service to Container Apps..."
az containerapp create \
  --name salesorder-service \
  --resource-group $RG \
  --environment $ENV_NAME \
  --workload-profile-name "Dedicated-D8" \
  --image acrvehicalecomprod.azurecr.io/salesorder-service:v1.0.0 \
  --target-port 8080 \
  --ingress external \
  --min-replicas 8 \
  --max-replicas 20 \
  --cpu 1.0 \
  --memory 2.0Gi \
  --scale-rule-name http-rule \
  --scale-rule-type http \
  --scale-rule-metadata concurrentRequests=80 \
  --env-vars \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_gcServer=1
```

---

### 6.3 Pros, Cons & Operational Trade-offs at 2,000 RPS

- **Pros**: Reduced infrastructure management; built-in Envoy ingress and canary traffic splitting; zero cluster patching.
- **Cons**: 
  1. Cold starts can introduce latency spikes ($> 2 \text{ seconds}$) during rapid traffic surges unless high minimum replicas are constantly pre-warmed.
  2. Limited access to low-level Linux networking knobs (`SO_REUSEPORT`, TCP keep-alive buffers).
  3. Cost: At 2,000 sustained RPS, dedicated workload profiles run continuously and can be 15–25% more expensive than reserved-instance AKS nodes.

---

## 7. Phase 5: Option C — Azure Virtual Machine Scale Sets (VMSS) Deployment

For legacy compliance or environments where container orchestrators (Kubernetes/ACA) are prohibited by enterprise policy, Virtual Machine Scale Sets (VMSS) provide bare-metal VM compute fronted by Azure Application Gateway.

### 7.1 VMSS Architecture & Application Gateway Integration

```
                                  [Internet]
                                      │
                                      ▼
                        [Azure Application Gateway v2]
                         (WAF, SSL Termination, 2k RPS)
                                      │
           ┌──────────────────────────┴──────────────────────────┐
           ▼                                                     ▼
[VMSS Instance 1: Standard_D16s_v5]            [VMSS Instance 2: Standard_D16s_v5]
  ├── Nginx Reverse Proxy (Port 80/443)          ├── Nginx Reverse Proxy (Port 80/443)
  ├── Systemd: AuthService.service (Port 5001)   ├── Systemd: AuthService.service (Port 5001)
  ├── Systemd: Items.service (Port 5002)         ├── Systemd: Items.service (Port 5002)
  ├── Systemd: ItemsPrice.service (Port 5003)    ├── Systemd: ItemsPrice.service (Port 5003)
  ├── Systemd: SalesOrder.service (Port 5007)    ├── Systemd: SalesOrder.service (Port 5007)
  └── UI Static Files (/var/www/html)            └── UI Static Files (/var/www/html)
```

---

### 7.2 Cloud-Init Automated VM Node Provisioning Script

Save as `cloud-init.yaml`:

```yaml
#cloud-config
package_upgrade: true
packages:
  - nginx
  - apt-transport-https
  - aspnetcore-runtime-8.0
  - jq

write_files:
  - path: /etc/systemd/system/salesorder.service
    permissions: '0644'
    content: |
      [Unit]
      Description=Vehicle E-Commerce SalesOrder Microservice
      After=network.target

      [Service]
      WorkingDirectory=/var/www/salesorder
      ExecStart=/usr/bin/dotnet /var/www/salesorder/SalesOrder.API.dll
      Restart=always
      RestartSec=10
      KillSignal=SIGINT
      SyslogIdentifier=salesorder-api
      User=www-data
      Environment=ASPNETCORE_ENVIRONMENT=Production
      Environment=ASPNETCORE_URLS=http://127.0.0.1:5007
      Environment=DOTNET_gcServer=1

      [Install]
      WantedBy=multi-user.target

  - path: /etc/nginx/sites-available/default
    permissions: '0644'
    content: |
      upstream salesorder_upstream {
          server 127.0.0.1:5007 max_fails=3 fail_timeout=10s;
          keepalive 64;
      }

      server {
          listen 80 default_server;
          server_name _;

          location /api/salesorders {
              proxy_pass http://salesorder_upstream;
              proxy_http_version 1.1;
              proxy_set_header Connection "";
              proxy_set_header Host $host;
              proxy_set_header X-Real-IP $remote_addr;
              proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
          }
      }

runcmd:
  - systemctl daemon-reload
  - systemctl enable salesorder.service
  - systemctl restart nginx
```

---

### 7.3 VMSS Autoscaling Script with Azure CLI

```bash
# Provision VMSS with 6 nodes min, 12 nodes max
az vmss create \
  --resource-group rg-vehicalecom-prod \
  --name vmss-vehicalecom-prod \
  --image Ubuntu2204 \
  --vm-sku Standard_D16s_v5 \
  --instance-count 6 \
  --custom-data cloud-init.yaml \
  --upgrade-policy-mode Automatic \
  --zones 1 2 3 \
  --app-gateway appgw-vehicalecom-prod

# Scale out when average CPU > 65% across 3 minutes
az monitor autoscale-rule create \
  --resource-group rg-vehicalecom-prod \
  --autoscale-name autoscale-vmss \
  --scale out 2 \
  --condition "Percentage CPU > 65 avg 3m"
```

---

## 8. Phase 6: Advanced — 2,000 RPS Performance Tuning & Production Hardening

### 8.1 .NET 8 Runtime & Kestrel Engine Optimization

At 2,000 RPS, default threadpool and GC settings can cause thread starvation. Apply these runtime variables in `Program.cs` and container environments:

```csharp
// Program.cs optimization for high-throughput concurrency
public static void Main(string[] args)
{
    // Pre-allocate ThreadPool worker and I/O completion threads to eliminate ramp-up latency
    ThreadPool.SetMinThreads(workerThreads: 500, completionPortThreads: 500);

    var builder = WebApplication.CreateBuilder(args);

    // Kestrel High-Throughput Socket Engine Tuning
    builder.WebHost.ConfigureKestrel(serverOptions =>
    {
        serverOptions.Limits.MaxConcurrentConnections = 10000;
        serverOptions.Limits.MaxConcurrentUpgradedConnections = 10000;
        serverOptions.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10MB
        serverOptions.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);
        serverOptions.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(15);
        serverOptions.AddServerHeader = false; // Security hardening
    });
```

---

### 8.2 Entity Framework Core 8 & SQL Server Connection Pool Tuning

To eliminate DbContext instantiation garbage collection overhead under 2,000 RPS, use **DbContext Pooling**:

```csharp
// In ServiceRegistration.cs:
builder.Services.AddDbContextPool<SalesOrderDbContext>(options =>
{
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(3),
            errorNumbersToAdd: null);
        sqlOptions.CommandTimeout(15);
    });
}, poolSize: 1024); // Pre-warms 1,024 DbContext instances
```

---

### 8.3 Redis Connection Multiplexing & Stampede Protection

To mitigate the **Cache Stampede (Thundering Herd)** problem when popular vehicle catalog keys expire:

```csharp
public async Task<VehicleDto> GetVehicleByIdAsync(int id, CancellationToken ct)
{
    string cacheKey = $"vehicle:item:{id}";
    
    // 1. Try cache read
    var cached = await _redisDb.StringGetAsync(cacheKey);
    if (!cached.IsNullOrEmpty)
        return JsonSerializer.Deserialize<VehicleDto>(cached!)!;

    // 2. Cache Miss: Acquire distributed lock to prevent thundering herd
    string lockKey = $"lock:vehicle:{id}";
    string lockToken = Guid.NewGuid().ToString();
    bool acquired = await _redisDb.LockTakeAsync(lockKey, lockToken, TimeSpan.FromSeconds(5));

    if (acquired)
    {
        try
        {
            // Query SQL Server
            var vehicle = await _dbContext.Items.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
            if (vehicle != null)
            {
                await _redisDb.StringSetAsync(cacheKey, JsonSerializer.Serialize(vehicle), TimeSpan.FromMinutes(30));
            }
            return vehicle;
        }
        finally
        {
            await _redisDb.LockReleaseAsync(lockKey, lockToken);
        }
    }
    else
    {
        // Wait briefly and read populated cache
        await Task.Delay(50, ct);
        return await GetVehicleByIdAsync(id, ct);
    }
}
```

---

### 8.4 Apache Kafka High-Throughput Producer & Consumer Tuning

```csharp
// High-Throughput Producer Configuration for 2,000 RPS
var producerConfig = new ProducerConfig
{
    BootstrapServers = configuration["Kafka:BootstrapServers"],
    SecurityProtocol = SecurityProtocol.SaslSsl,
    SaslMechanism = SaslMechanism.Plain,
    SaslUsername = "$ConnectionString",
    SaslPassword = configuration["Kafka:SaslPassword"],
    
    // Throughput & Durability Tuning
    Acks = Acks.All,                 // Guarantee zero message loss
    EnableIdempotence = true,         // Exactly-once delivery semantics
    LingerMs = 5,                    // Micro-batching window (5ms batching)
    BatchSize = 64 * 1024,           // 64 KB batch size
    CompressionType = CompressionType.Lz4, // High-speed, low-CPU compression
    QueueBufferingMaxMessages = 200000,
    MessageTimeoutMs = 10000
};
```

---

### 8.5 Zero-Downtime Blue/Green and Canary Deployment Strategies

Using **Argo Rollouts** or **Ingress-NGINX Canary Annotations**, route 10% of 2,000 RPS traffic to a canary release to validate error rates before promoting:

```yaml
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: salesorder-canary-ingress
  namespace: vehicalecom
  annotations:
    kubernetes.io/ingress.class: nginx
    nginx.ingress.kubernetes.io/canary: "true"
    nginx.ingress.kubernetes.io/canary-weight: "10" # 10% traffic directed to Canary
spec:
  rules:
  - host: api.vehicalecom.com
    http:
      paths:
      - path: /api/salesorders
        pathType: Prefix
        backend:
          service:
            name: salesorder-service-canary
            port:
              number: 80
```

---

## 9. Phase 7: Enterprise Observability, Verification & Site Reliability

### 9.1 OpenTelemetry Distributed Tracing & Azure Monitor Integration

Instrument all microservices with W3C TraceContext standards:

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
    {
        tracerProviderBuilder
            .AddSource("VehicleEcom.*")
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("SalesOrder.API"))
            .AddAspNetCoreInstrumentation(opts => opts.RecordException = true)
            .AddHttpClientInstrumentation()
            .AddEntityFrameworkCoreInstrumentation()
            .AddRedisInstrumentation()
            .AddOtlpExporter(opt => opt.Endpoint = new Uri("http://otel-collector.monitoring:4317"));
    });
```

---

### 9.2 Prometheus & Grafana High-Load Dashboard Metrics

Monitor these four golden signals at 2,000 RPS:
1. **Latency (P50, P95, P99)**: Alert if P99 > 150 ms over 2 minutes.
2. **Traffic (RPS)**: Monitored via `rate(http_requests_total[1m])`.
3. **Errors**: Alert if HTTP 5xx error rate > 0.5%.
4. **Saturation**: Node CPU > 75%, SQL Pool DTU > 75%, Redis Memory > 80%, Kafka Consumer Lag > 500 messages.

---

### 9.3 2,000 RPS Automated Load Testing Script (`load-test-2000rps.js` with k6)

Save this k6 script to validate the deployment:

```javascript
import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  scenarios: {
    sustained_2000_rps: {
      executor: 'constant-arrival-rate',
      rate: 2000,
      timeUnit: '1s',
      duration: '5m',
      preAllocatedVUs: 200,
      maxVUs: 1000,
    },
  },
  thresholds: {
    http_req_duration: ['p(95)<50', 'p(99)<100'], // 95% of requests under 50ms
    http_req_failed: ['rate<0.005'],               // Less than 0.5% errors
  },
};

const BASE_URL = 'https://api.vehicalecom.com';

export default function () {
  const rand = Math.random();

  // 70% Read Traffic: Vehicle Catalog & Pricing
  if (rand < 0.70) {
    const itemId = Math.floor(Math.random() * 50) + 1;
    const res = http.get(`${BASE_URL}/api/items/${itemId}`);
    check(res, { 'status is 200': (r) => r.status === 200 });
  } 
  // 20% Identity Verification / Token
  else if (rand < 0.90) {
    const payload = JSON.stringify({ email: 'testuser@vehicalecom.com', password: 'Password123!' });
    const res = http.post(`${BASE_URL}/api/auth/login`, payload, {
      headers: { 'Content-Type': 'application/json' },
    });
    check(res, { 'status is 200 or 401': (r) => r.status === 200 || r.status === 401 });
  } 
  // 10% Transactional Write: Order Creation / Sagas
  else {
    const orderPayload = JSON.stringify({
      customerId: 'CUST-1002',
      lines: [{ itemId: 1, quantity: 1, selectedAccessoryIds: [10, 12] }],
    });
    const res = http.post(`${BASE_URL}/api/salesorders`, orderPayload, {
      headers: {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer <PRE_GENERATED_BENCHMARK_JWT>',
      },
    });
    check(res, { 'status is 201 or 200': (r) => r.status === 201 || r.status === 200 });
  }
}
```

---

### 9.4 Disaster Recovery (DR), Multi-Region Failover & Operational Runbook

| Scenario | Impact | Automated Recovery Action | RPO / RTO |
| :--- | :--- | :--- | :--- |
| **Single Pod Crash** | Zero client impact | Kubernetes restarts pod via liveness probe; other 7 replicas handle traffic. | RPO = 0 / RTO < 3s |
| **Availability Zone Outage** | Zero client impact | Multi-AZ AKS nodes & Zone-redundant SQL/Redis absorb load across remaining zones. | RPO = 0 / RTO = 0s |
| **Azure Primary Region Failure** | Traffic rerouted | Azure Front Door shifts traffic to Secondary Region (e.g., West US 2); SQL Active Geo-Replication failover. | RPO < 5s / RTO < 3 min |
| **Database Deadlock / Corruption** | Transaction errors | Point-In-Time Restore (PITR) from automated Azure SQL transactional log backups. | RPO < 1m / RTO < 15 min |

---

*Vehicle E-Commerce Microservices Platform — Enterprise Azure Production Manual*
