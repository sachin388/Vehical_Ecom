// DealerService.API/Program.cs
using Dealer.Application.Services;
using DealerService.Application.Interfaces;
using DealerService.Application.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register concrete implementations in the composition root.
// Replace these example registrations with your actual Infrastructure classes.
builder.Services.AddScoped<DealerApplicationService, DealerApplicationService>();
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
