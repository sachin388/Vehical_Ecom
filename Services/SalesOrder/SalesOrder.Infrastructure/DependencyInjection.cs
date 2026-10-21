using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SalesOrder.Application.Interfaces;
using SalesOrder.Infrastructure.Messaging;
using SalesOrder.Infrastructure.Messaging.IntegrationEvents;
using SalesOrder.Infrastructure.Persistence;
using SalesOrder.Infrastructure.Repositories;
using SalesOrder.Infrastructure.Services;

namespace SalesOrder.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddSalesOrderInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("SalesOrderDb")
                ?? "Server=(localdb)\\mssqllocaldb;Database=SalesOrderDb;Trusted_Connection=True;MultipleActiveResultSets=true";

            services.AddDbContext<SalesOrderDbContext>(options =>
                options.UseSqlServer(connectionString));

            services.AddScoped<ISalesOrderRepository, SalesOrderRepository>();
            services.AddScoped<IUnitOfWork, EfUnitOfWork>();
            services.AddScoped<IOutboxWriter, EfOutboxWriter>();
            services.AddScoped<IPricingClient, PricingClient>();

            services.AddScoped<PaymentSucceededHandler>();
            services.AddScoped<DealerAllocatedHandler>();
            services.AddScoped<PaymentCompensationCompletedHandler>();
            services.AddScoped<DealerReleaseCompletedHandler>();
            services.AddScoped<SalesOrderIntegrationEventHandler>();

            return services;
        }
    }
}
