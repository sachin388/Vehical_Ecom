using ItemsPrice.Application.Services.NewFolder;
using ItemsPrice.Infrastructure;
using VehicleEcom.Observability;
using VehicleEcom.Security.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddPlatformOpenTelemetryLogging(builder.Configuration, "ItemsPrice.API");
builder.Services.AddPlatformObservability(builder.Configuration, "ItemsPrice.API");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.AddVehicleSwaggerSecurity(
        "Vehicle Pricing Service API",
        "v1",
        "Vehicle E-Commerce Pricing Microservice protected by Centralized OIDC / OAuth 2.0.");
});

builder.Services.AddItemPriceInfrastructure(builder.Configuration);
builder.Services.AddScoped<ItemPriceApplicationService>();

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
