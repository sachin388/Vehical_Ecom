using ItemsPrice.Infrastructure.Interfaces;
using ItemsPrice.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ItemsPrice.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddItemPriceInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddDbContext<ItemPriceDbContext>(options =>
            {
                options.UseSqlServer(
                    configuration.GetConnectionString("ItemPriceDb"));
            });

            services.AddScoped<IItemPriceRepository,
                ItemPriceRepository>();

            services.AddSingleton<IEventPublisher,
                KafkaEventPublisher>();

            return services;
        }
    }

}
