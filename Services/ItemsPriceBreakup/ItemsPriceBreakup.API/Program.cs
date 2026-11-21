using ItemsPriceBreakup.Infrastruture;
using VehicleEcom.Observability;
using VehicleEcom.Security.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddPlatformOpenTelemetryLogging(builder.Configuration, "ItemsPriceBreakup.API");
builder.Services.AddPlatformObservability(builder.Configuration, "ItemsPriceBreakup.API");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddVehicleSwaggerSecurity(
        "Vehicle Price Breakup Service API",
        "v1",
        "Vehicle E-Commerce Price Breakup & Tax Calculation Microservice protected by Centralized OIDC / OAuth 2.0.");
});

builder.Services.AddInfrastructure(builder.Configuration);

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
