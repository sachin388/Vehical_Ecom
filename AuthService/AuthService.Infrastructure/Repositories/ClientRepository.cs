using AuthService.Application.Interfaces;
using AuthService.Domain.Entities;
using System.Collections.Concurrent;

namespace AuthService.Infrastructure.Repositories
{
    public class ClientRepository : IClientRepository
    {
        private static readonly ConcurrentDictionary<string, Client> Clients = new(StringComparer.OrdinalIgnoreCase);

        static ClientRepository()
        {
            var seedClients = new List<Client>
            {
                new Client
                {
                    ClientId = "vehicle-web-app",
                    ClientSecret = "WebSecret2026!",
                    ClientName = "Vehicle E-Commerce Web Portal",
                    ClientType = "web",
                    AllowedGrantTypes = "password,refresh_token,authorization_code",
                    AllowedScopes = "openid,profile,email,vehicle_api,offline_access",
                    AccessTokenLifetimeSeconds = 900,
                    RefreshTokenLifetimeDays = 7,
                    IsActive = true
                },
                new Client
                {
                    ClientId = "vehicle-mobile-app",
                    ClientSecret = "MobileSecret2026!",
                    ClientName = "Vehicle E-Commerce Mobile App (iOS / Android)",
                    ClientType = "mobile",
                    AllowedGrantTypes = "password,refresh_token",
                    AllowedScopes = "openid,profile,email,vehicle_api,offline_access",
                    AccessTokenLifetimeSeconds = 1800, // 30 mins for mobile
                    RefreshTokenLifetimeDays = 30, // 30 days for mobile
                    IsActive = true
                },
                new Client
                {
                    ClientId = "salesorder-service",
                    ClientSecret = "SalesOrderServiceSecret2026!",
                    ClientName = "Sales Order Microservice M2M Client",
                    ClientType = "service",
                    AllowedGrantTypes = "client_credentials",
                    AllowedScopes = "salesorder.internal,itemsprice.internal,payment.internal,dealer.internal,internal.all",
                    AccessTokenLifetimeSeconds = 3600,
                    RefreshTokenLifetimeDays = 0,
                    IsActive = true
                },
                new Client
                {
                    ClientId = "payment-service",
                    ClientSecret = "PaymentServiceSecret2026!",
                    ClientName = "Payment Microservice M2M Client",
                    ClientType = "service",
                    AllowedGrantTypes = "client_credentials",
                    AllowedScopes = "salesorder.internal,payment.internal,internal.all",
                    AccessTokenLifetimeSeconds = 3600,
                    RefreshTokenLifetimeDays = 0,
                    IsActive = true
                },
                new Client
                {
                    ClientId = "dealer-service",
                    ClientSecret = "DealerServiceSecret2026!",
                    ClientName = "Dealer Microservice M2M Client",
                    ClientType = "service",
                    AllowedGrantTypes = "client_credentials",
                    AllowedScopes = "salesorder.internal,dealer.internal,internal.all",
                    AccessTokenLifetimeSeconds = 3600,
                    RefreshTokenLifetimeDays = 0,
                    IsActive = true
                },
                new Client
                {
                    ClientId = "pricing-service",
                    ClientSecret = "PricingServiceSecret2026!",
                    ClientName = "Pricing Microservice M2M Client",
                    ClientType = "service",
                    AllowedGrantTypes = "client_credentials",
                    AllowedScopes = "itemsprice.internal,internal.all",
                    AccessTokenLifetimeSeconds = 3600,
                    RefreshTokenLifetimeDays = 0,
                    IsActive = true
                },
                new Client
                {
                    ClientId = "gateway-service",
                    ClientSecret = "GatewayServiceSecret2026!",
                    ClientName = "API Gateway Edge Client",
                    ClientType = "service",
                    AllowedGrantTypes = "client_credentials",
                    AllowedScopes = "internal.all",
                    AccessTokenLifetimeSeconds = 3600,
                    RefreshTokenLifetimeDays = 0,
                    IsActive = true
                }
            };

            foreach (var client in seedClients)
            {
                Clients[client.ClientId] = client;
            }
        }

        public Task<Client?> GetByClientIdAsync(string clientId)
        {
            if (string.IsNullOrWhiteSpace(clientId))
                return Task.FromResult<Client?>(null);

            Clients.TryGetValue(clientId, out var client);
            return Task.FromResult(client);
        }

        public Task<bool> ValidateClientCredentialsAsync(string clientId, string clientSecret)
        {
            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
                return Task.FromResult(false);

            if (Clients.TryGetValue(clientId, out var client))
            {
                return Task.FromResult(client.IsActive && string.Equals(client.ClientSecret, clientSecret, StringComparison.Ordinal));
            }

            return Task.FromResult(false);
        }
    }
}

