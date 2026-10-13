using Accessory.Infrastructure.Persistence;
using Accessory.Infrastructure.Repositories;

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
