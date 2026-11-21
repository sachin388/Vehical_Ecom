using System.Threading.RateLimiting;
using ApiGateway.Middleware;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi.Models;
using VehicleEcom.Observability;
using VehicleEcom.Security.Extensions;
using Yarp.ReverseProxy.Configuration;

var builder = WebApplication.CreateBuilder(args);

// -----------------------------------------------------------------------------
// 1. Configuration, Logging & OpenTelemetry Observability (Elastic & Kibana APM)
// -----------------------------------------------------------------------------
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.AddPlatformOpenTelemetryLogging(builder.Configuration, "ApiGateway");

builder.Services.AddPlatformObservability(builder.Configuration, "ApiGateway");

// -----------------------------------------------------------------------------
// 2. CORS Policy
// -----------------------------------------------------------------------------
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[]
{
    "http://localhost:3000",
    "http://localhost:5173",
    "http://localhost:5174",
    "http://localhost:4173",
    "http://localhost:8080"
};

builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCorsPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// -----------------------------------------------------------------------------
// 3. Centralized OIDC / OAuth 2.0 JWT Authentication & Authorization
// -----------------------------------------------------------------------------
builder.Services.AddVehiclePlatformAuthentication(builder.Configuration);
builder.Services.AddVehiclePlatformAuthorization();

// -----------------------------------------------------------------------------
// 4. Rate Limiting (.NET 8 Built-in)
// -----------------------------------------------------------------------------
var rateLimitPermits = builder.Configuration.GetValue<int>("RateLimiting:PermitLimit", 200);
var rateLimitWindow = builder.Configuration.GetValue<int>("RateLimiting:WindowSeconds", 60);
var rateLimitQueue = builder.Configuration.GetValue<int>("RateLimiting:QueueLimit", 50);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter("fixed-window", opt =>
    {
        opt.PermitLimit = rateLimitPermits;
        opt.Window = TimeSpan.FromSeconds(rateLimitWindow);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = rateLimitQueue;
    });

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsync(
            "{\"status\":429,\"error\":\"Too Many Requests\",\"message\":\"Gateway rate limit exceeded. Please retry shortly.\"}",
            token);
    };
});

// -----------------------------------------------------------------------------
// 5. YARP Reverse Proxy
// -----------------------------------------------------------------------------
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// -----------------------------------------------------------------------------
// 6. Swagger / OpenAPI Documentation
// -----------------------------------------------------------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddVehicleSwaggerSecurity(
        "Vehicle E-Commerce API Gateway (YARP)",
        "v1",
        "Enterprise YARP Reverse Proxy & Edge Gateway for Vehicle E-Commerce Microservices. " +
        "Provides centralized OIDC / OAuth 2.0 JWT authentication relay, distributed tracing, rate limiting, and intelligent path routing.");
});

// -----------------------------------------------------------------------------
// 7. Health Checks
// -----------------------------------------------------------------------------
builder.Services.AddHealthChecks();

var app = builder.Build();

// -----------------------------------------------------------------------------
// 8. HTTP Request Pipeline
// -----------------------------------------------------------------------------
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Vehicle E-Commerce API Gateway v1");
    c.RoutePrefix = "swagger";
});

// Custom Middlewares & OpenTelemetry Trace Correlation
app.UsePlatformObservability();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<GatewayLoggingMiddleware>();

app.UseCors("DefaultCorsPolicy");
app.UseRateLimiter();

app.UseAuthentication();
app.UseMiddleware<UserContextRelayMiddleware>();
app.UseVehicleSecurityAudit();
app.UseAuthorization();

// -----------------------------------------------------------------------------
// 9. Informational & Health Endpoints
// -----------------------------------------------------------------------------
app.MapGet("/", () => Results.Ok(new
{
    Gateway = "Vehicle E-Commerce Platform API Gateway",
    Engine = "Microsoft YARP (Yet Another Reverse Proxy)",
    Status = "Online",
    Port = 7000,
    Version = "1.0.0",
    TimestampUtc = DateTime.UtcNow,
    Documentation = "/swagger",
    HealthCheck = "/health",
    RoutesEndpoint = "/api/gateway/routes",
    Services = new[]
    {
        new { Name = "AuthService", Route = "/api/auth/*, /connect/*, /.well-known/*", Cluster = "auth-cluster", Port = 5149 },
        new { Name = "ItemsService", Route = "/api/items/*", Cluster = "items-cluster", Port = 5022 },
        new { Name = "ItemsPriceService", Route = "/api/item-prices/*, /api/pricing/*", Cluster = "itemsprice-cluster", Port = 5193 },
        new { Name = "ItemsPriceBreakupService", Route = "/api/price-breakups/*, /api/pricebreakup/*", Cluster = "itemspricebreakup-cluster", Port = 5142 },
        new { Name = "AccessoryService", Route = "/api/accessories/*", Cluster = "accessory-cluster", Port = 5276 },
        new { Name = "DealerService", Route = "/api/dealers/*", Cluster = "dealer-cluster", Port = 5017 },
        new { Name = "SalesOrderService", Route = "/api/sales-orders/*, /api/salesorders/*", Cluster = "salesorder-cluster", Port = 5236 },
        new { Name = "PaymentService", Route = "/api/payments/*", Cluster = "payment-cluster", Port = 5008 },
        new { Name = "NotificationService", Route = "/api/notifications/*", Cluster = "notification-cluster", Port = 5299 }
    }
}));

app.MapGet("/health", () => Results.Ok(new
{
    Status = "Healthy",
    Service = "ApiGateway",
    TimestampUtc = DateTime.UtcNow,
    ActiveConnections = true
}));

app.MapGet("/api/gateway/routes", (IProxyConfigProvider proxyConfig) =>
{
    var config = proxyConfig.GetConfig();
    var routes = config.Routes.Select(r => new
    {
        r.RouteId,
        MatchPath = r.Match.Path,
        r.ClusterId,
        r.AuthorizationPolicy,
        r.RateLimiterPolicy
    });

    var clusters = config.Clusters.Select(c => new
    {
        c.ClusterId,
        Destinations = c.Destinations?.Select(d => new
        {
            DestinationId = d.Key,
            d.Value.Address
        })
    });

    return Results.Ok(new
    {
        TotalRoutes = routes.Count(),
        TotalClusters = clusters.Count(),
        Routes = routes,
        Clusters = clusters
    });
});

// -----------------------------------------------------------------------------
// 10. Map YARP Reverse Proxy Middleware
// -----------------------------------------------------------------------------
app.MapReverseProxy();

app.Run();
