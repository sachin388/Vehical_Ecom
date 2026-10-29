using Dealer.API;
using Dealer.Application.Interfaces;
using Dealer.Application.Services;

var builder = WebApplication.CreateBuilder(args);

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
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
