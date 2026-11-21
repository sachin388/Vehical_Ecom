using AuthService.Application.DTOs;
using System;
using System.Threading.Tasks;

namespace AuthService.Application.Interfaces
{
    public interface IAuthService
    {
        Task RegisterAsync(RegisterRequest request);

        Task<TokenResponse> LoginAsync(LoginRequest request);

        Task<TokenResponse> LoginAsync(LoginRequest request, string? clientIp, string? clientId = null, string? clientType = null);

        Task<TokenResponse> RefreshTokenAsync(RefreshTokenRequest request);

        Task<TokenResponse> RefreshTokenAsync(RefreshTokenRequest request, string? clientIp);

        Task LogoutAsync(string refreshToken);

        Task LogoutAsync(string refreshToken, string? clientIp);

        Task<TokenResponse> HandleOAuthTokenRequestAsync(OAuthTokenRequest request, string? clientIp);

        Task<UserInfoResponse> GetUserInfoAsync(Guid userId);
    }
}
