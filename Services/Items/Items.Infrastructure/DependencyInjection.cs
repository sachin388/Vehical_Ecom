using Items.Application.Interfaces;
using Items.Infrastructure.Messaging;
using Items.Infrastructure.Persistence;
using Items.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Items.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddItemInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddDbContext<ItemDbContext>(options =>
            {
                options.UseSqlServer(
                    configuration.GetConnectionString("ItemDb"));
            });

            services.AddScoped<IItemRepository, ItemRepository>();

            services.AddSingleton<IEventPublisher, KafkaEventPublisher>();

            return services;
        }
    }

}
