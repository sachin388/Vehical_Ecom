using Dealer.API;
using Dealer.Application.Interfaces;
using Dealer.Application.Services;
using VehicleEcom.Observability;
using VehicleEcom.Security.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddPlatformOpenTelemetryLogging(builder.Configuration, "Dealer.API");
builder.Services.AddPlatformObservability(builder.Configuration, "Dealer.API");

// Centralized OIDC & OAuth 2.0 Token Validation & Authorization
builder.Services.AddVehiclePlatformAuthentication(builder.Configuration);
builder.Services.AddVehiclePlatformAuthorization();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddVehicleSwaggerSecurity(
        "Dealer Service API",
        "v1",
        "Vehicle E-Commerce Dealership & Inventory Management Microservice protected by Centralized OIDC / OAuth 2.0.");
});

builder.Services.AddSingleton<IDealerSelectionStrategy, DefaultDealerSelectionStrategy>();
builder.Services.AddSingleton<IDealerRepository, InMemoryDealerRepository>();
builder.Services.AddSingleton<IDealerInventoryRepository, InMemoryDealerInventoryRepository>();
builder.Services.AddSingleton<IEventPublisher, NoOpEventPublisher>();

builder.Services.AddScoped<DealerApplicationService>();
builder.Services.AddScoped<IDealerInventoryApplicationService, DealerInventoryApplicationService>();

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
