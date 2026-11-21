using Payment.Application.Interfaces;
using Payment.Application.Services;
using Payment.Infrastructure;
using VehicleEcom.Observability;
using VehicleEcom.Security.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddPlatformOpenTelemetryLogging(builder.Configuration, "Payment.API");
builder.Services.AddPlatformObservability(builder.Configuration, "Payment.API");

// 1. Centralized OIDC / OAuth 2.0 JWT Authentication & Authorization
builder.Services.AddVehiclePlatformAuthentication(builder.Configuration);
builder.Services.AddVehiclePlatformAuthorization();

// 2. Infrastructure & Application Services
builder.Services.AddPaymentInfrastructure(builder.Configuration);
builder.Services.AddScoped<IPaymentApplicationService, PaymentApplicationService>();

// 3. Controllers & Swagger with Bearer Auth
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddVehicleSwaggerSecurity(
        "Payment Service API (PayU Gateway)",
        "v1",
        "Vehicle E-Commerce Payment Processing & PayU Integration Microservice protected by Centralized OIDC / OAuth 2.0.");
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Payment Service API v1");
    });
}

app.UseHttpsRedirection();
app.UsePlatformObservability();

app.UseAuthentication();
app.UseVehicleSecurityAudit();
app.UseAuthorization();

app.MapControllers();

app.Run();
