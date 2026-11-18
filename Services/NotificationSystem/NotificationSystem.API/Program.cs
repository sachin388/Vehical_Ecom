using VehicleEcom.Observability;

var builder = WebApplication.CreateBuilder(args);

// Add OpenTelemetry Observability (Elastic & Kibana APM)
builder.Logging.AddPlatformOpenTelemetryLogging(builder.Configuration, "NotificationSystem.API");
builder.Services.AddPlatformObservability(builder.Configuration, "NotificationSystem.API");

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UsePlatformObservability();

app.UseAuthorization();

app.MapControllers();

app.Run();
