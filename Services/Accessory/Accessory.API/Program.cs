using Accessory.Infrastructure;
using VehicleEcom.Observability;
using VehicleEcom.Security.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddPlatformOpenTelemetryLogging(builder.Configuration, "Accessory.API");
builder.Services.AddPlatformObservability(builder.Configuration, "Accessory.API");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddVehicleSwaggerSecurity(
        "Accessory Service API",
        "v1",
        "Vehicle E-Commerce Accessories Management Microservice protected by Centralized OIDC / OAuth 2.0.");
});

builder.Services.AddInfrastructure(builder.Configuration);

// Centralized OIDC & OAuth 2.0 Token Validation & PBAC/RBAC
builder.Services.AddVehiclePlatformAuthentication(builder.Configuration);
builder.Services.AddVehiclePlatformAuthorization();

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
