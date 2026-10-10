using ItemsPriceBreakup.Application.Interfaces;
using ItemsPriceBreakup.Application.Services;
using ItemsPriceBreakup.Infrastruture.Messaging;
using ItemsPriceBreakup.Infrastruture.Persistence;
using ItemsPriceBreakup.Infrastruture.Pricing;
using ItemsPriceBreakup.Infrastruture.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ItemsPriceBreakup.Infrastruture
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddDbContext<PriceBreakupDbContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("PriceBreakupDb")));

            services.AddScoped<IChargeRule, ExShowroomChargeRule>();
            services.AddScoped<IChargeRule, GstChargeRule>();
            services.AddScoped<IChargeRule, RtoChargeRule>();
            services.AddScoped<IChargeRule, InsuranceChargeRule>();
            services.AddScoped<IChargeRule, HandlingChargeRule>();
            services.AddScoped<IChargeRule, RegistrationChargeRule>();
            services.AddScoped<IChargeRule, OtherChargeRule>();

            services.AddScoped<IChargeRuleFactory, ChargeRuleFactory>();
            services.AddScoped<IPriceBreakupCalculator, PriceBreakupCalculator>();

            services.AddScoped<IPriceBreakupRepository,
                PriceBreakupRepository>();

            services.AddSingleton<IEventPublisher, KafkaEventPublisher>();

            services.AddScoped<PriceBreakupApplicationService>();

            return services;
        }
    }

}
