using AuthService.Application.Interfaces;
using AuthService.Infrastructure.Persistence;
using AuthService.Infrastructure.Repositories;
using AuthService.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AuthService.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("AuthDb");

            services.AddDbContext<AuthDbContext>(options =>
            {
                if (!string.IsNullOrEmpty(connectionString))
                {
                    options.UseSqlServer(connectionString);
                }
            });

            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IRoleRepository, RoleRepository>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
            services.AddSingleton<IClientRepository, ClientRepository>();
            services.AddSingleton<IKeyMaterialService, KeyMaterialService>();
            services.AddScoped<IPasswordService, PasswordService>();
            services.AddScoped<ITokenService, TokenService>();

            return services;
        }
    }
}
