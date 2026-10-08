using AuthService.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthService.Application.Interfaces
{
    public interface IAuthService
    {
        Task RegisterAsync(RegisterRequest request);

        Task<TokenResponse> LoginAsync(LoginRequest request);

        Task<TokenResponse> RefreshTokenAsync(
            RefreshTokenRequest request);

        Task LogoutAsync(string refreshToken);
    }

}
