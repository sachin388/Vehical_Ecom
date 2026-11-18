# Vehicle E-Commerce API Gateway (Microsoft YARP)

This project provides the centralized **API Reverse Proxy and Edge Gateway** for the Vehicle E-Commerce microservices platform built using **.NET 8** and **Microsoft YARP (Yet Another Reverse Proxy)**.

---

## Architecture Overview

```
                      +---------------------------------------+
                      |       Web / Mobile Clients            |
                      |  (Vite / React: http://localhost:5173) |
                      +-------------------+-------------------+
                                          |
                                          v
                      +---------------------------------------+
                      |           API GATEWAY (YARP)          |
                      |      http://localhost:7000            |
                      |      https://localhost:7001           |
                      +-------------------+-------------------+
                                          |
        +---------------------------------+---------------------------------+
        |                 |               |               |                 |
        v                 v               v               v                 v
+---------------+ +---------------+ +-----------+ +---------------+ +---------------+
|  AuthService  | |   Items.API   | |ItemsPrice | | Accessory.API | |  Dealer.API   |
|   Port 5149   | |   Port 5022   | | Port 5193 | |   Port 5276   | |   Port 5017   |
+---------------+ +---------------+ +-----------+ +---------------+ +---------------+
        |                 |               |
        v                 v               v
+---------------+ +---------------+ +-----------------------+
|SalesOrder.API | |  Payment.API  | | NotificationSystem.API|
|   Port 5236   | |   Port 5008   | |       Port 5299       |
+---------------+ +---------------+ +-----------------------+
```

---

## Core Capabilities & Middleware Pipeline

1. **Intelligent Path Routing & Transforms**:
   - Centralized routing through `ReverseProxy` configuration in `appsettings.json`.
   - Path transforms supported for legacy and shorthand endpoints (e.g., `/api/pricing/*` -> `/api/item-prices/*`).
   - Injects `X-Forwarded-Gateway: YARP-VehicleEcommerce` downstream.

2. **Distributed Tracing (`X-Correlation-Id`)**:
   - `CorrelationIdMiddleware` checks for incoming `X-Correlation-Id` or `traceparent` headers.
   - If missing, generates a unique `Guid` correlation identifier, appends it to `HttpContext.TraceIdentifier`, downstream request headers, and response headers.

3. **User Context Relay**:
   - `UserContextRelayMiddleware` extracts validated JWT claims (`sub`/`NameIdentifier`, `email`, `role`) and injects them into downstream HTTP request headers (`X-User-Id`, `X-User-Email`, `X-User-Roles`).
   - Allows backend microservices to access user credentials without redundant JWT parsing.

4. **Built-in Rate Limiting (.NET 8 Fixed Window)**:
   - Configurable permits (default: 200 requests per 60s per window).
   - Queue limit of 50 requests with `OldestFirst` processing.
   - Returns clean JSON 429 Too Many Requests response if threshold is exceeded.

5. **Cross-Origin Resource Sharing (CORS)**:
   - Configured `DefaultCorsPolicy` allowing credentials, all headers, and methods from frontend web applications (`http://localhost:5173`, `http://localhost:3000`, `http://localhost:5174`, etc.).

6. **Interactive Documentation & Observability**:
   - Swagger / OpenAPI UI at `/swagger` with Bearer Token authorization support.
   - Health check endpoint at `/health` returning status and timestamp.
   - Dynamic route catalog at `/api/gateway/routes` listing all active YARP routes and cluster destinations.
   - Root welcome & directory endpoint at `/`.

---

## Service Routing Map

| Gateway Route Path | Target Microservice | Destination Address | Transforms / Aliases |
| :--- | :--- | :--- | :--- |
| `/api/auth/{**catch-all}` | `AuthService` | `http://localhost:5149` | Direct proxy |
| `/api/items/{**catch-all}` | `Items.API` | `http://localhost:5022` | Direct proxy |
| `/api/item-prices/{**catch-all}` | `ItemsPrice.API` | `http://localhost:5193` | Direct proxy |
| `/api/pricing/{**catch-all}` | `ItemsPrice.API` | `http://localhost:5193` | Rewrites to `/api/item-prices/{**catch-all}` |
| `/api/price-breakups/{**catch-all}` | `ItemsPriceBreakup.API` | `http://localhost:5142` | Direct proxy |
| `/api/pricebreakup/{**catch-all}` | `ItemsPriceBreakup.API` | `http://localhost:5142` | Rewrites to `/api/price-breakups/{**catch-all}` |
| `/api/accessories/{**catch-all}` | `Accessory.API` | `http://localhost:5276` | Direct proxy |
| `/api/dealers/{**catch-all}` | `Dealer.API` | `http://localhost:5017` | Direct proxy |
| `/api/sales-orders/{**catch-all}` | `SalesOrder.API` | `http://localhost:5236` | Direct proxy |
| `/api/salesorders/{**catch-all}` | `SalesOrder.API` | `http://localhost:5236` | Rewrites to `/api/sales-orders/{**catch-all}` |
| `/internal/sales-orders/{**catch-all}`| `SalesOrder.API` | `http://localhost:5236` | Internal service routing |
| `/api/payments/{**catch-all}` | `Payment.API` | `http://localhost:5008` | Direct proxy |
| `/api/notifications/{**catch-all}` | `NotificationSystem.API` | `http://localhost:5299` | Direct proxy |

---

## How to Run

### Using .NET CLI
```bash
# Navigate to solution directory
cd d:\WorkSpace\Vehical_Ecom\Vehical_Ecom

# Build the Gateway
dotnet build ApiGateway/ApiGateway.csproj

# Run the Gateway
dotnet run --project ApiGateway/ApiGateway.csproj
```

The gateway will start listening on:
- **HTTP**: `http://localhost:7000`
- **HTTPS**: `https://localhost:7001`

---

## Verification Endpoints

1. **Gateway Overview**:
   ```bash
   curl http://localhost:7000/
   ```
2. **Health Check**:
   ```bash
   curl http://localhost:7000/health
   ```
3. **Active YARP Route Discovery**:
   ```bash
   curl http://localhost:7000/api/gateway/routes
   ```
4. **Interactive Swagger UI**:
   Open browser at: `http://localhost:7000/swagger`
