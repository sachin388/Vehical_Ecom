using AuthService.Application.DTOs;
using AuthService.Application.Interfaces;
using AuthService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthService.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IPasswordService _passwordService;
        private readonly ITokenService _tokenService;

        public AuthService(
            IUserRepository userRepository,
            IRoleRepository roleRepository,
            IRefreshTokenRepository refreshTokenRepository,
            IPasswordService passwordService,
            ITokenService tokenService)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _passwordService = passwordService;
            _tokenService = tokenService;
        }

        public async Task RegisterAsync(
            RegisterRequest request)
        {
            var email =
                request.Email.Trim().ToLowerInvariant();

            var existingUser =
                await _userRepository
                    .GetByEmailAsync(email);

            if (existingUser != null)
            {
                throw new InvalidOperationException(
                    "User already exists.");
            }

            var user = new User
            {
                Id = Guid.NewGuid(),

                FirstName = request.FirstName.Trim(),

                LastName = request.LastName.Trim(),

                Email = email,

                IsActive = true,

                CreatedAtUtc = DateTime.UtcNow
            };

            user.PasswordHash =
                _passwordService.HashPassword(
                    user,
                    request.Password);

            var customerRole =
                await _roleRepository
                    .GetByNameAsync("Customer");

            if (customerRole == null)
            {
                customerRole = new Role
                {
                    Id = Guid.NewGuid(),
                    Name = "Customer"
                };

                await _roleRepository
                    .AddAsync(customerRole);
            }

            user.UserRoles.Add(
                new UserRole
                {
                    UserId = user.Id,
                    RoleId = customerRole.Id,
                    User = user,
                    Role = customerRole
                });

            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();
        }

        public async Task<TokenResponse> LoginAsync(
            LoginRequest request)
        {
            var email =
                request.Email.Trim().ToLowerInvariant();

            var user =
                await _userRepository
                    .GetByEmailAsync(email);

            if (user == null)
            {
                throw new UnauthorizedAccessException(
                    "Invalid email or password.");
            }

            if (!user.IsActive)
            {
                throw new UnauthorizedAccessException(
                    "User account is inactive.");
            }

            var passwordValid =
                _passwordService.VerifyPassword(
                    user,
                    user.PasswordHash,
                    request.Password);

            if (!passwordValid)
            {
                throw new UnauthorizedAccessException(
                    "Invalid email or password.");
            }

            var tokens =
                _tokenService.GenerateTokens(user);

            var refreshToken = new RefreshToken
            {
                Id = Guid.NewGuid(),

                UserId = user.Id,

                TokenHash =
                    _tokenService.HashRefreshToken(
                        tokens.RefreshToken),

                CreatedAtUtc = DateTime.UtcNow,

                ExpiresAtUtc =
                    tokens.RefreshTokenExpiresAtUtc
            };

            await _refreshTokenRepository
                .AddAsync(refreshToken);

            await _refreshTokenRepository
                .SaveChangesAsync();

            return tokens;
        }

        public async Task<TokenResponse> RefreshTokenAsync(
            RefreshTokenRequest request)
        {
            var tokenHash =
                _tokenService.HashRefreshToken(
                    request.RefreshToken);

            var storedToken =
                await _refreshTokenRepository
                    .GetByHashAsync(tokenHash);

            if (storedToken == null)
            {
                throw new UnauthorizedAccessException(
                    "Invalid refresh token.");
            }

            if (!storedToken.IsActive)
            {
                throw new UnauthorizedAccessException(
                    "Refresh token expired or revoked.");
            }

            if (!storedToken.User.IsActive)
            {
                throw new UnauthorizedAccessException(
                    "User account is inactive.");
            }

            var newTokens =
                _tokenService.GenerateTokens(
                    storedToken.User);

            storedToken.RevokedAtUtc =
                DateTime.UtcNow;

            var newRefreshToken =
                new RefreshToken
                {
                    Id = Guid.NewGuid(),

                    UserId = storedToken.UserId,

                    TokenHash =
                        _tokenService.HashRefreshToken(
                            newTokens.RefreshToken),

                    CreatedAtUtc =
                        DateTime.UtcNow,

                    ExpiresAtUtc =
                        newTokens.RefreshTokenExpiresAtUtc
                };

            storedToken.ReplacedByTokenId =
                newRefreshToken.Id;

            await _refreshTokenRepository
                .AddAsync(newRefreshToken);

            await _refreshTokenRepository
                .SaveChangesAsync();

            return newTokens;
        }

        public async Task LogoutAsync(
            string refreshToken)
        {
            var tokenHash =
                _tokenService.HashRefreshToken(
                    refreshToken);

            var storedToken =
                await _refreshTokenRepository
                    .GetByHashAsync(tokenHash);

            if (storedToken == null)
            {
                return;
            }

            if (!storedToken.IsRevoked)
            {
                storedToken.RevokedAtUtc =
                    DateTime.UtcNow;

                await _refreshTokenRepository
                    .SaveChangesAsync();
            }
        }
    }

}
