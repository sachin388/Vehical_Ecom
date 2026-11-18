using Dealer.API;
using Dealer.Application.Interfaces;
using Dealer.Application.Services;
using VehicleEcom.Observability;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddPlatformOpenTelemetryLogging(builder.Configuration, "Dealer.API");
builder.Services.AddPlatformObservability(builder.Configuration, "Dealer.API");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
app.UseAuthorization();
app.MapControllers();
app.Run();
