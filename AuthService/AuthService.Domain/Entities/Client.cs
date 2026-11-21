using System;

namespace AuthService.Domain.Entities
{
    public class Client
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public string ClientType { get; set; } = "web"; // web, mobile, service
        public string AllowedGrantTypes { get; set; } = "password,refresh_token";
        public string AllowedScopes { get; set; } = "openid,profile,email,vehicle_api,offline_access";
        public bool IsActive { get; set; } = true;
        public int AccessTokenLifetimeSeconds { get; set; } = 900; // 15 mins
        public int RefreshTokenLifetimeDays { get; set; } = 7;
    }
}

