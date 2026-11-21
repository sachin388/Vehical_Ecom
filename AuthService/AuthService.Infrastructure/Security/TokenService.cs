using AuthService.Application.DTOs;
using AuthService.Application.Interfaces;
using AuthService.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using VehicleEcom.Security.Constants;

namespace AuthService.Infrastructure.Security
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;
        private readonly IKeyMaterialService _keyMaterialService;

        public TokenService(
            IConfiguration configuration,
            IKeyMaterialService keyMaterialService)
        {
            _configuration = configuration;
            _keyMaterialService = keyMaterialService;
        }

        public TokenResponse GenerateTokens(User user)
        {
            return GenerateTokens(user, "vehicle-web-app", "web", new[] { "openid", "profile", "email", "vehicle_api", "offline_access" });
        }

        public TokenResponse GenerateTokens(
            User user,
            string? clientId,
            string? clientType,
            IEnumerable<string>? scopes,
            string? nonce = null)
        {
            var issuer = _configuration["Jwt:Issuer"] ?? "VehicleEcommerce.Auth";
            var audience = _configuration["Jwt:Audience"] ?? "VehicleEcommerce.Api";
            var secretKey = _configuration["Jwt:SecretKey"] ?? "6XNealH/7HrldBMPZ4T/cNiQnxsTz/N1eOPrfOKN+05uTquaBKTBqgcAeq/N5QJPveDAqe1QuckyAP1jef2gKg==";

            var accessMinutes = int.TryParse(_configuration["Jwt:AccessTokenMinutes"], out var m) ? m : 15;
            var refreshDays = int.TryParse(_configuration["Jwt:RefreshTokenDays"], out var d) ? d : 7;

            // Mobile clients can have extended validity
            if (string.Equals(clientType, "mobile", StringComparison.OrdinalIgnoreCase))
            {
                accessMinutes = Math.Max(accessMinutes, 30);
                refreshDays = Math.Max(refreshDays, 30);
            }

            var now = DateTime.UtcNow;
            var accessExpires = now.AddMinutes(accessMinutes);
            var refreshExpires = now.AddDays(refreshDays);

            var resolvedScopes = scopes?.ToList() ?? new List<string> { "openid", "profile", "email", "vehicle_api" };
            var scopeString = string.Join(" ", resolvedScopes);

            var roles = user.UserRoles?.Select(r => r.Role?.Name ?? "Customer").ToList() ?? new List<string> { "Customer" };
            if (!roles.Any()) roles.Add("Customer");

            var permissions = RolePermissionDefaults.GetPermissionsForRoles(roles);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(JwtRegisteredClaimNames.Email, user.Email),
                new(ClaimTypes.Email, user.Email),
                new(JwtRegisteredClaimNames.Name, $"{user.FirstName} {user.LastName}".Trim()),
                new(JwtRegisteredClaimNames.GivenName, user.FirstName),
                new(JwtRegisteredClaimNames.FamilyName, user.LastName),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                new(VehicleClaimTypes.ClientId, clientId ?? "vehicle-web-app"),
                new(VehicleClaimTypes.ClientType, clientType ?? "web"),
                new(VehicleClaimTypes.TokenType, "access_token"),
                new(VehicleClaimTypes.Scope, scopeString)
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
                claims.Add(new Claim(VehicleClaimTypes.Role, role));
            }

            foreach (var permission in permissions)
            {
                claims.Add(new Claim(VehicleClaimTypes.Permission, permission));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                notBefore: now,
                expires: accessExpires,
                signingCredentials: credentials);

            var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
            var refreshToken = GenerateRefreshToken();

            string? idToken = null;
            if (resolvedScopes.Contains("openid", StringComparer.OrdinalIgnoreCase))
            {
                idToken = GenerateIdToken(user, clientId ?? "vehicle-web-app", nonce);
            }

            return new TokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                TokenType = "Bearer",
                ExpiresIn = (int)(accessExpires - now).TotalSeconds,
                Scope = scopeString,
                IdToken = idToken,
                AccessTokenExpiresAtUtc = accessExpires,
                RefreshTokenExpiresAtUtc = refreshExpires
            };
        }

        public TokenResponse GenerateClientCredentialsToken(Client client, IEnumerable<string> scopes)
        {
            var issuer = _configuration["Jwt:Issuer"] ?? "VehicleEcommerce.Auth";
            var audience = _configuration["Jwt:Audience"] ?? "VehicleEcommerce.Api";
            var secretKey = _configuration["Jwt:SecretKey"] ?? "6XNealH/7HrldBMPZ4T/cNiQnxsTz/N1eOPrfOKN+05uTquaBKTBqgcAeq/N5QJPveDAqe1QuckyAP1jef2gKg==";

            var accessSeconds = client.AccessTokenLifetimeSeconds > 0 ? client.AccessTokenLifetimeSeconds : 3600;
            var now = DateTime.UtcNow;
            var accessExpires = now.AddSeconds(accessSeconds);

            var scopeList = scopes?.ToList() ?? new List<string> { VehicleScopes.InternalAll };
            var scopeString = string.Join(" ", scopeList);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, client.ClientId),
                new(ClaimTypes.NameIdentifier, client.ClientId),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                new(VehicleClaimTypes.ClientId, client.ClientId),
                new(VehicleClaimTypes.ClientType, "service"),
                new(VehicleClaimTypes.TokenType, "access_token"),
                new(VehicleClaimTypes.Scope, scopeString),
                new(ClaimTypes.Role, VehicleRoles.ServiceAccount),
                new(VehicleClaimTypes.Role, VehicleRoles.ServiceAccount)
            };

            // Grant internal service permissions based on service account defaults
            foreach (var perm in RolePermissionDefaults.GetPermissionsForRole(VehicleRoles.ServiceAccount))
            {
                claims.Add(new Claim(VehicleClaimTypes.Permission, perm));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                notBefore: now,
                expires: accessExpires,
                signingCredentials: credentials);

            var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

            return new TokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = string.Empty,
                TokenType = "Bearer",
                ExpiresIn = accessSeconds,
                Scope = scopeString,
                AccessTokenExpiresAtUtc = accessExpires,
                RefreshTokenExpiresAtUtc = accessExpires
            };
        }

        public string GenerateIdToken(User user, string clientId, string? nonce)
        {
            var issuer = _configuration["Jwt:Issuer"] ?? "VehicleEcommerce.Auth";
            var secretKey = _configuration["Jwt:SecretKey"] ?? "6XNealH/7HrldBMPZ4T/cNiQnxsTz/N1eOPrfOKN+05uTquaBKTBqgcAeq/N5QJPveDAqe1QuckyAP1jef2gKg==";

            var now = DateTime.UtcNow;
            var expires = now.AddMinutes(60);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Iss, issuer),
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new(JwtRegisteredClaimNames.Aud, clientId),
                new(JwtRegisteredClaimNames.Email, user.Email),
                new(JwtRegisteredClaimNames.Name, $"{user.FirstName} {user.LastName}".Trim()),
                new(JwtRegisteredClaimNames.GivenName, user.FirstName),
                new(JwtRegisteredClaimNames.FamilyName, user.LastName),
                new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                new(VehicleClaimTypes.TokenType, "id_token")
            };

            if (!string.IsNullOrEmpty(nonce))
            {
                claims.Add(new Claim(JwtRegisteredClaimNames.Nonce, nonce));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: clientId,
                claims: claims,
                notBefore: now,
                expires: expires,
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string HashRefreshToken(string refreshToken)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(refreshToken);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToHexString(hash);
        }

        private static string GenerateRefreshToken()
        {
            var randomBytes = RandomNumberGenerator.GetBytes(64);
            return Convert.ToBase64String(randomBytes);
        }
    }
}
