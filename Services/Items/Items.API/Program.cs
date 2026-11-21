using Items.Application.Services;
using Items.Infrastructure;
using VehicleEcom.Observability;
using VehicleEcom.Security.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddPlatformOpenTelemetryLogging(builder.Configuration, "Items.API");
builder.Services.AddPlatformObservability(builder.Configuration, "Items.API");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddVehicleSwaggerSecurity(
        "Vehicle Catalog & Items Service API",
        "v1",
        "Vehicle E-Commerce Catalog & Items Microservice protected by Centralized OIDC / OAuth 2.0.");
});

builder.Services.AddItemInfrastructure(builder.Configuration);
builder.Services.AddScoped<ItemApplicationService>();

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
