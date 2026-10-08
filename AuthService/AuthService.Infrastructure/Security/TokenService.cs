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
using System.Threading.Tasks;

namespace AuthService.Infrastructure.Security
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;

        public TokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public TokenResponse GenerateTokens(User user)
        {
            var issuer = _configuration["Jwt:Issuer"]!;
            var audience = _configuration["Jwt:Audience"]!;
            var secretKey = _configuration["Jwt:SecretKey"]!;

            var accessMinutes =
                int.Parse(
                    _configuration["Jwt:AccessTokenMinutes"]!);

            var refreshDays =
                int.Parse(
                    _configuration["Jwt:RefreshTokenDays"]!);

            var accessExpires =
                DateTime.UtcNow.AddMinutes(accessMinutes);

            var refreshExpires =
                DateTime.UtcNow.AddDays(refreshDays);

            var claims = new List<Claim>
        {
            new(
                ClaimTypes.NameIdentifier,
                user.Id.ToString()),

            new(
                ClaimTypes.Email,
                user.Email),

            new(
                JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString())
        };

            foreach (var userRole in user.UserRoles)
            {
                claims.Add(
                    new Claim(
                        ClaimTypes.Role,
                        userRole.Role.Name));
            }

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(secretKey));

            var credentials =
                new SigningCredentials(
                    key,
                    SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: accessExpires,
                signingCredentials: credentials);

            var accessToken =
                new JwtSecurityTokenHandler()
                    .WriteToken(token);

            var refreshToken =
                GenerateRefreshToken();

            return new TokenResponse
            {
                AccessToken = accessToken,

                RefreshToken = refreshToken,

                AccessTokenExpiresAtUtc =
                    accessExpires,

                RefreshTokenExpiresAtUtc =
                    refreshExpires
            };
        }

        public string HashRefreshToken(
            string refreshToken)
        {
            using var sha256 =
                SHA256.Create();

            var bytes =
                Encoding.UTF8.GetBytes(
                    refreshToken);

            var hash =
                sha256.ComputeHash(bytes);

            return Convert.ToHexString(hash);
        }

        private static string GenerateRefreshToken()
        {
            var randomBytes =
                RandomNumberGenerator.GetBytes(64);

            return Convert.ToBase64String(
                randomBytes);
        }
    }

}
