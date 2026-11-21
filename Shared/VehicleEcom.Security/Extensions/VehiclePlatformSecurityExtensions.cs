using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using VehicleEcom.Security.Audit;
using VehicleEcom.Security.Authorization;
using VehicleEcom.Security.Constants;
using VehicleEcom.Security.ServiceAuth;
using VehicleEcom.Security.UserContext;

namespace VehicleEcom.Security.Extensions;

public static class VehiclePlatformSecurityExtensions
{
    public const string DefaultSecretKey = "6XNealH/7HrldBMPZ4T/cNiQnxsTz/N1eOPrfOKN+05uTquaBKTBqgcAeq/N5QJPveDAqe1QuckyAP1jef2gKg==";
    public const string LegacySecretKey = "VehicleEcommerce_SuperSecretKey_ForJwtTokenGeneration_2026!";
    public const string DefaultIssuer = "VehicleEcommerce.Auth";
    public const string DefaultAudience = "VehicleEcommerce.Api";

    public static IServiceCollection AddVehiclePlatformAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<JwtBearerOptions>? configureJwt = null)
    {
        services.AddHttpContextAccessor();
        services.AddMemoryCache();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<ISecurityAuditLogger, SecurityAuditLogger>();

        var jwtSection = configuration.GetSection("Jwt");
        var issuer = jwtSection["Issuer"] ?? configuration["Authentication:Issuer"] ?? DefaultIssuer;
        var audience = jwtSection["Audience"] ?? configuration["Authentication:Audience"] ?? DefaultAudience;
        var secretKey = jwtSection["SecretKey"] ?? configuration["Authentication:SecretKey"] ?? DefaultSecretKey;
        var authority = jwtSection["Authority"] ?? configuration["Authentication:Authority"];

        var validIssuers = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            issuer,
            DefaultIssuer,
            "VehicleEcommerce",
            "http://localhost:5149",
            "https://localhost:5149"
        };

        var validAudiences = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            audience,
            DefaultAudience,
            "VehicleEcommerce"
        };

        // Prepare symmetric keys (both current and legacy keys accepted for zero-downtime compatibility)
        var signingKeys = new List<SecurityKey>
        {
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        };

        if (!string.Equals(secretKey, LegacySecretKey, StringComparison.Ordinal))
        {
            signingKeys.Add(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(LegacySecretKey)));
        }

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;

                if (!string.IsNullOrEmpty(authority))
                {
                    options.Authority = authority;
                }

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuers = validIssuers,
                    ValidAudiences = validAudiences,
                    IssuerSigningKeys = signingKeys,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = VehicleClaimTypes.Name,
                    RoleClaimType = ClaimTypes.Role
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetService<ILogger<JwtBearerEvents>>();
                        logger?.LogWarning("Authentication failed: {ErrorMessage}", context.Exception.Message);
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetService<ILogger<JwtBearerEvents>>();
                        var sub = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                  ?? context.Principal?.FindFirst(VehicleClaimTypes.Subject)?.Value;
                        logger?.LogDebug("JWT validated successfully for Subject={Subject}", sub);
                        return Task.CompletedTask;
                    }
                };

                configureJwt?.Invoke(options);
            });

        return services;
    }

    public static IServiceCollection AddVehiclePlatformAuthorization(
        this IServiceCollection services,
        Action<AuthorizationOptions>? configure = null)
    {
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        services.AddAuthorization(options =>
        {
            // 1. Authenticated
            options.AddPolicy("Authenticated", policy => policy.RequireAuthenticatedUser());

            // 2. Roles
            options.AddPolicy("AdminOnly", policy =>
                policy.RequireRole(VehicleRoles.Admin, VehicleRoles.SuperAdmin));

            options.AddPolicy("RequireAdmin", policy =>
                policy.RequireRole(VehicleRoles.Admin, VehicleRoles.SuperAdmin));

            options.AddPolicy("DealerOnly", policy =>
                policy.RequireRole(VehicleRoles.Dealer, VehicleRoles.DealerPrincipal, VehicleRoles.DealershipSalesManager, VehicleRoles.Admin, VehicleRoles.SuperAdmin));

            options.AddPolicy("RequireDealer", policy =>
                policy.RequireRole(VehicleRoles.Dealer, VehicleRoles.DealerPrincipal, VehicleRoles.DealershipSalesManager, VehicleRoles.Admin, VehicleRoles.SuperAdmin));

            options.AddPolicy("CustomerOnly", policy =>
                policy.RequireRole(VehicleRoles.Customer, VehicleRoles.Admin, VehicleRoles.SuperAdmin));

            options.AddPolicy("RequireCustomer", policy =>
                policy.RequireRole(VehicleRoles.Customer, VehicleRoles.Admin, VehicleRoles.SuperAdmin));

            // 3. Service-to-service internal calls
            options.AddPolicy("InternalService", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireAssertion(ctx =>
                {
                    if (ctx.User.IsInRole(VehicleRoles.ServiceAccount) ||
                        ctx.User.IsInRole(VehicleRoles.Admin) ||
                        ctx.User.IsInRole(VehicleRoles.SuperAdmin))
                    {
                        return true;
                    }

                    var scopes = ctx.User.FindAll("scope")
                        .Concat(ctx.User.FindAll(VehicleClaimTypes.Scope))
                        .SelectMany(c => c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries));

                    return scopes.Any(s => s.EndsWith(".internal", StringComparison.OrdinalIgnoreCase) ||
                                           string.Equals(s, VehicleScopes.InternalAll, StringComparison.OrdinalIgnoreCase));
                });
            });

            configure?.Invoke(options);
        });

        return services;
    }

    public static IServiceCollection AddServiceTokenClient(
        this IServiceCollection services,
        IConfiguration configuration,
        string clientId,
        string clientSecret,
        string? defaultScope = null,
        string? tokenEndpoint = null)
    {
        services.Configure<ServiceTokenOptions>(options =>
        {
            options.ClientId = clientId;
            options.ClientSecret = clientSecret;
            options.TokenEndpoint = tokenEndpoint
                ?? configuration["Authentication:TokenEndpoint"]
                ?? configuration["Jwt:TokenEndpoint"]
                ?? "http://localhost:5149/connect/token";
            options.DefaultScope = defaultScope ?? VehicleScopes.InternalAll;
        });

        services.AddHttpClient<IServiceTokenClient, ServiceTokenClient>();
        services.AddTransient<ServiceTokenDelegatingHandler>();

        return services;
    }

    public static IApplicationBuilder UseVehicleSecurityAudit(this IApplicationBuilder app)
    {
        return app.UseMiddleware<SecurityAuditMiddleware>();
    }
}

