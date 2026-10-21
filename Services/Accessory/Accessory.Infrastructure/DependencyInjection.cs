using Accessory.Application.Interfaces;
using Accessory.Application.Services;
using Accessory.Infrastructure.Messaging;
using Accessory.Infrastructure.Persistence;
using Accessory.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Accessory.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddDbContext<AccessoryDbContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("AccessoryDb")));

            services.AddScoped<IAccessoryRepository, AccessoryRepository>();
            services.AddSingleton<IEventPublisher, KafkaEventPublisher>();
            services.AddScoped<AccessoryApplicationService>();

            return services;
        }
    }

}
