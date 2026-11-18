using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Payment.Application.Interfaces;
using Payment.Application.Services;
using Payment.Infrastructure;
using VehicleEcom.Observability;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddPlatformOpenTelemetryLogging(builder.Configuration, "Payment.API");
builder.Services.AddPlatformObservability(builder.Configuration, "Payment.API");

// 1. JWT Authentication
var jwtKey = builder.Configuration["Jwt:SecretKey"] ?? "VehicleEcommerce_SuperSecretKey_ForJwtTokenGeneration_2026!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "VehicleEcommerce";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "VehicleEcommerce";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        if (!string.IsNullOrEmpty(builder.Configuration["Authentication:Authority"]))
        {
            options.Authority = builder.Configuration["Authentication:Authority"];
            options.Audience = builder.Configuration["Authentication:Audience"];
            options.RequireHttpsMetadata = true;
        }

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

// 2. Infrastructure & Application Services
builder.Services.AddPaymentInfrastructure(builder.Configuration);
builder.Services.AddScoped<IPaymentApplicationService, PaymentApplicationService>();

// 3. Controllers & JSON
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// 4. Swagger with Bearer Auth
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Payment Service API (PayU Gateway)",
        Version = "v1",
        Description = "Microservice for managing payments and PayU gateway integrations."
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT Bearer token."
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Payment Service API v1");
    });
}

app.UseHttpsRedirection();

app.UsePlatformObservability();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
