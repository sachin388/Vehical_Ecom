using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Payment.Application.Interfaces;
using Payment.Infrastructure.Gateway.PayU;
using Payment.Infrastructure.Persistence;

namespace Payment.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddPaymentInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("PaymentDb")
                ?? "Server=(localdb)\\mssqllocaldb;Database=PaymentDb;Trusted_Connection=True;MultipleActiveResultSets=true";

            services.AddDbContext<PaymentDbContext>(options =>
                options.UseSqlServer(connectionString));

            services.AddScoped<IPaymentRepository, EfPaymentRepository>();
            services.AddScoped<IUnitOfWork, EfUnitOfWork>();
            services.AddScoped<IOutboxWriter, EfOutboxWriter>();

            services.Configure<PayUOptions>(configuration.GetSection(PayUOptions.SectionName));

            services.AddHttpClient<IPayUGatewayService, PayUGatewayService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            return services;
        }
    }
}

