using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace VehicleEcom.Security.Extensions;

public static class SwaggerSecurityExtensions
{
    public static void AddVehicleSwaggerSecurity(
        this SwaggerGenOptions options,
        string title,
        string version = "v1",
        string? description = null)
    {
        options.SwaggerDoc(version, new OpenApiInfo
        {
            Title = title,
            Version = version,
            Description = description ?? $"{title} - Protected by Vehicle Platform Centralized OIDC / OAuth 2.0 Security."
        });

        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter JWT Bearer access token issued by Vehicle E-Commerce AuthService.\nExample: 'Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...'"
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
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
    }
}

