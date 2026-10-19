
using Confluent.Kafka;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SalesOrder.Infrastructure.Messaging;
using SalesOrder.Infrastructure.Messaging;

var builder = WebApplication.CreateBuilder(args);

// 1. JWT Authentication
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority =
            builder.Configuration["Authentication:Authority"];

        options.Audience =
            builder.Configuration["Authentication:Audience"];

        options.RequireHttpsMetadata = true;

        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
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

// 3. Kafka Producer
builder.Services.AddSingleton<IProducer<string, string>>(sp =>
{
    var configuration =
        sp.GetRequiredService<IConfiguration>();

    var bootstrapServers =
        configuration["Kafka:BootstrapServers"]
        ?? throw new InvalidOperationException(
            "Kafka:BootstrapServers is missing.");

    var config = new ProducerConfig
    {
        BootstrapServers = bootstrapServers,
        EnableIdempotence = true
    };

    return new ProducerBuilder<string, string>(config).Build();
});

// 4. Dead Letter Queue Publisher
builder.Services.AddSingleton<KafkaDeadLetterPublisher>();

// Other registrations
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Middleware order matters
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
