using AuthService.Application.Interfaces;
using AuthService.Infrastructure;
using VehicleEcom.Observability;
using VehicleEcom.Security.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddPlatformOpenTelemetryLogging(builder.Configuration, "AuthService");
builder.Services.AddPlatformObservability(builder.Configuration, "AuthService");

// CORS for web clients
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[]
{
    "http://localhost:3000",
    "http://localhost:5173",
    "http://localhost:5174",
    "http://localhost:4173",
    "http://localhost:8080"
};

builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCorsPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.AddVehicleSwaggerSecurity(
        "Vehicle E-Commerce AuthService (OIDC / OAuth 2.0 IdP)",
        "v1",
        "Centralized Identity Provider & Security Token Service implementing OIDC and OAuth 2.0 for Web, Mobile, and Microservices.");
});

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<IAuthService, AuthService.Application.Services.AuthService>();

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
app.UseCors("DefaultCorsPolicy");

app.UseAuthentication();
app.UseVehicleSecurityAudit();
app.UseAuthorization();

app.MapControllers();

app.Run();
