using Confluent.Kafka;
using SalesOrder.Application.Services;
using SalesOrder.Infrastructure;
using SalesOrder.Infrastructure.Messaging;
using VehicleEcom.Observability;
using VehicleEcom.Security.Constants;
using VehicleEcom.Security.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddPlatformOpenTelemetryLogging(builder.Configuration, "SalesOrder.API");
builder.Services.AddPlatformObservability(builder.Configuration, "SalesOrder.API");

// 1. Centralized OIDC / OAuth 2.0 JWT Authentication & PBAC/RBAC Authorization
builder.Services.AddVehiclePlatformAuthentication(builder.Configuration);
builder.Services.AddVehiclePlatformAuthorization();

// 2. Service-to-Service M2M Client for Inter-Service Communication
builder.Services.AddServiceTokenClient(
    builder.Configuration,
    clientId: "salesorder-service",
    clientSecret: "SalesOrderServiceSecret2026!",
    defaultScope: VehicleScopes.SalesOrderInternal);

// 3. Infrastructure & Services
builder.Services.AddSalesOrderInfrastructure(builder.Configuration);
builder.Services.AddScoped<ISalesOrderApplicationService, SalesOrderApplicationService>();
builder.Services.AddScoped<SalesOrderApplicationService>();
builder.Services.AddScoped<OrderCancellationService>();

// 4. Kafka Producer
builder.Services.AddSingleton<IProducer<string, string>>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var bootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092";

    var config = new ProducerConfig
    {
        BootstrapServers = bootstrapServers,
        EnableIdempotence = true
    };

    return new ProducerBuilder<string, string>(config).Build();
});

// 5. Dead Letter Queue Publisher
builder.Services.AddSingleton<KafkaDeadLetterPublisher>();

// 6. Controllers & Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddVehicleSwaggerSecurity(
        "Sales Order Service API",
        "v1",
        "Vehicle E-Commerce Sales Order Management Microservice with Centralized OIDC / OAuth 2.0 Protection.");
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UsePlatformObservability();

app.UseAuthentication();
app.UseVehicleSecurityAudit();
app.UseAuthorization();

app.MapControllers();

app.Run();
