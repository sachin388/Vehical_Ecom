using VehicleEcom.Observability;
using VehicleEcom.Security.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add OpenTelemetry Observability (Elastic & Kibana APM)
builder.Logging.AddPlatformOpenTelemetryLogging(builder.Configuration, "NotificationSystem.API");
builder.Services.AddPlatformObservability(builder.Configuration, "NotificationSystem.API");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddVehicleSwaggerSecurity(
        "Notification Service API",
        "v1",
        "Vehicle E-Commerce Notifications Microservice protected by Centralized OIDC / OAuth 2.0.");
});

// Centralized OIDC & OAuth 2.0 Token Validation & Authorization
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
