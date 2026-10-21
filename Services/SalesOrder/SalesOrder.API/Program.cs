using System.Text;
using Confluent.Kafka;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SalesOrder.Application.Services;
using SalesOrder.Infrastructure;
using SalesOrder.Infrastructure.Messaging;

var builder = WebApplication.CreateBuilder(args);

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

// 2. Authorization policy for internal service calls
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("InternalService", policy =>
        policy.RequireAuthenticatedUser()
              .RequireClaim("scope", "salesorder.internal"));
});

// 3. Infrastructure & Services
builder.Services.AddSalesOrderInfrastructure(builder.Configuration);
builder.Services.AddScoped<ISalesOrderApplicationService, SalesOrderApplicationService>();
builder.Services.AddScoped<SalesOrderApplicationService>();
builder.Services.AddScoped<OrderCancellationService>();

// 4. Kafka Producer
builder.Services.AddSingleton<IProducer<string, string>>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var bootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092";

    var config = new ProducerConfig
    {
        BootstrapServers = bootstrapServers,
        EnableIdempotence = true
    };

    return new ProducerBuilder<string, string>(config).Build();
});

// 5. Dead Letter Queue Publisher
builder.Services.AddSingleton<KafkaDeadLetterPublisher>();

// 6. Controllers & Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
