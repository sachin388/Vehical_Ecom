using AuthService.Application.DTOs;
using AuthService.Domain.Entities;
using System.Collections.Generic;

namespace AuthService.Application.Interfaces
{
    public interface ITokenService
    {
        TokenResponse GenerateTokens(User user);

        TokenResponse GenerateTokens(
            User user,
            string? clientId,
            string? clientType,
            IEnumerable<string>? scopes,
            string? nonce = null);

        TokenResponse GenerateClientCredentialsToken(
            Client client,
            IEnumerable<string> scopes);

        string GenerateIdToken(
            User user,
            string clientId,
            string? nonce);

        string HashRefreshToken(string refreshToken);
    }
}
