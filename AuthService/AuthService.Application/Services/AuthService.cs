using AuthService.Application.DTOs;
using AuthService.Application.Interfaces;
using AuthService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VehicleEcom.Security.Audit;
using VehicleEcom.Security.Constants;

namespace AuthService.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IClientRepository _clientRepository;
        private readonly IPasswordService _passwordService;
        private readonly ITokenService _tokenService;
        private readonly ISecurityAuditLogger _auditLogger;

        public AuthService(
            IUserRepository userRepository,
            IRoleRepository roleRepository,
            IRefreshTokenRepository refreshTokenRepository,
            IClientRepository clientRepository,
            IPasswordService passwordService,
            ITokenService tokenService,
            ISecurityAuditLogger auditLogger)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _clientRepository = clientRepository;
            _passwordService = passwordService;
            _tokenService = tokenService;
            _auditLogger = auditLogger;
        }

        public async Task RegisterAsync(RegisterRequest request)
        {
            var email = request.Email.Trim().ToLowerInvariant();

            var existingUser = await _userRepository.GetByEmailAsync(email);
            if (existingUser != null)
            {
                _auditLogger.LogAuthenticationFailure(email, "Registration failed: user already exists", null);
                throw new InvalidOperationException("User already exists.");
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

            user.PasswordHash = _passwordService.HashPassword(user, request.Password);

            var customerRole = await _roleRepository.GetByNameAsync(VehicleRoles.Customer);
            if (customerRole == null)
            {
                customerRole = new Role
                {
                    Id = Guid.NewGuid(),
                    Name = VehicleRoles.Customer
                };

                await _roleRepository.AddAsync(customerRole);
            }

            user.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = customerRole.Id,
                User = user,
                Role = customerRole
            });

            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();

            _auditLogger.LogAuthenticationSuccess(user.Id.ToString(), email, "registration", null);
        }

        public Task<TokenResponse> LoginAsync(LoginRequest request)
        {
            return LoginAsync(request, null, "vehicle-web-app", "web");
        }

        public async Task<TokenResponse> LoginAsync(LoginRequest request, string? clientIp, string? clientId = null, string? clientType = null)
        {
            var email = request.Email.Trim().ToLowerInvariant();

            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
            {
                _auditLogger.LogAuthenticationFailure(email, "Invalid credentials (user not found)", clientIp);
                throw new UnauthorizedAccessException("Invalid email or password.");
            }

            if (!user.IsActive)
            {
                _auditLogger.LogAuthenticationFailure(email, "User account is inactive", clientIp);
                throw new UnauthorizedAccessException("User account is inactive.");
            }

            var passwordValid = _passwordService.VerifyPassword(user, user.PasswordHash, request.Password);
            if (!passwordValid)
            {
                _auditLogger.LogAuthenticationFailure(email, "Invalid credentials (incorrect password)", clientIp);
                throw new UnauthorizedAccessException("Invalid email or password.");
            }

            var tokens = _tokenService.GenerateTokens(
                user,
                clientId ?? "vehicle-web-app",
                clientType ?? "web",
                new[] { "openid", "profile", "email", "vehicle_api", "offline_access" });

            var refreshToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenHash = _tokenService.HashRefreshToken(tokens.RefreshToken),
                CreatedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = tokens.RefreshTokenExpiresAtUtc
            };

            await _refreshTokenRepository.AddAsync(refreshToken);
            await _refreshTokenRepository.SaveChangesAsync();

            _auditLogger.LogAuthenticationSuccess(user.Id.ToString(), email, clientId ?? "vehicle-web-app", clientIp);
            return tokens;
        }

        public Task<TokenResponse> RefreshTokenAsync(RefreshTokenRequest request)
        {
            return RefreshTokenAsync(request, null);
        }

        public async Task<TokenResponse> RefreshTokenAsync(RefreshTokenRequest request, string? clientIp)
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                _auditLogger.LogAuthenticationFailure("unknown", "Empty refresh token presented", clientIp);
                throw new UnauthorizedAccessException("Refresh token must be provided.");
            }

            var tokenHash = _tokenService.HashRefreshToken(request.RefreshToken);
            var storedToken = await _refreshTokenRepository.GetByHashAsync(tokenHash);

            if (storedToken == null)
            {
                _auditLogger.LogAuthenticationFailure("unknown", "Refresh token not found", clientIp);
                throw new UnauthorizedAccessException("Invalid refresh token.");
            }

            // Reuse detection / compromised token family check
            if (storedToken.IsRevoked)
            {
                _auditLogger.LogAuthenticationFailure(
                    storedToken.UserId.ToString(),
                    "Attempted reuse of already revoked refresh token (potential compromise)",
                    clientIp);
                throw new UnauthorizedAccessException("Refresh token expired or revoked.");
            }

            if (storedToken.IsExpired)
            {
                _auditLogger.LogAuthenticationFailure(storedToken.UserId.ToString(), "Refresh token has expired", clientIp);
                throw new UnauthorizedAccessException("Refresh token expired or revoked.");
            }

            if (!storedToken.User.IsActive)
            {
                _auditLogger.LogAuthenticationFailure(storedToken.UserId.ToString(), "User account is inactive during refresh", clientIp);
                throw new UnauthorizedAccessException("User account is inactive.");
            }

            var newTokens = _tokenService.GenerateTokens(storedToken.User);

            // Single-use token rotation: revoke old token
            storedToken.RevokedAtUtc = DateTime.UtcNow;

            var newRefreshToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = storedToken.UserId,
                TokenHash = _tokenService.HashRefreshToken(newTokens.RefreshToken),
                CreatedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = newTokens.RefreshTokenExpiresAtUtc
            };

            storedToken.ReplacedByTokenId = newRefreshToken.Id;

            await _refreshTokenRepository.AddAsync(newRefreshToken);
            await _refreshTokenRepository.SaveChangesAsync();

            _auditLogger.LogTokenRefresh(storedToken.UserId.ToString(), "refresh-grant", clientIp);
            return newTokens;
        }

        public Task LogoutAsync(string refreshToken)
        {
            return LogoutAsync(refreshToken, null);
        }

        public async Task LogoutAsync(string refreshToken, string? clientIp)
        {
            if (string.IsNullOrWhiteSpace(refreshToken)) return;

            var tokenHash = _tokenService.HashRefreshToken(refreshToken);
            var storedToken = await _refreshTokenRepository.GetByHashAsync(tokenHash);

            if (storedToken == null) return;

            if (!storedToken.IsRevoked)
            {
                storedToken.RevokedAtUtc = DateTime.UtcNow;
                await _refreshTokenRepository.SaveChangesAsync();
                _auditLogger.LogTokenRevocation(storedToken.UserId.ToString(), "User logged out", clientIp);
            }
        }

        public async Task<TokenResponse> HandleOAuthTokenRequestAsync(OAuthTokenRequest request, string? clientIp)
        {
            var grantType = request.GrantType?.Trim().ToLowerInvariant();

            switch (grantType)
            {
                case "password":
                {
                    if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                    {
                        throw new UnauthorizedAccessException("Username and password are required.");
                    }

                    Client? client = null;
                    if (!string.IsNullOrWhiteSpace(request.ClientId))
                    {
                        client = await _clientRepository.GetByClientIdAsync(request.ClientId);
                    }

                    var email = request.Username.Trim().ToLowerInvariant();
                    var user = await _userRepository.GetByEmailAsync(email);
                    if (user == null || !user.IsActive || !_passwordService.VerifyPassword(user, user.PasswordHash, request.Password))
                    {
                        _auditLogger.LogAuthenticationFailure(email, "Invalid OAuth credentials", clientIp);
                        throw new UnauthorizedAccessException("Invalid username or password.");
                    }

                    var scopes = !string.IsNullOrWhiteSpace(request.Scope)
                        ? request.Scope.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                        : new[] { "openid", "profile", "email", "vehicle_api" };

                    var clientType = client?.ClientType ?? "web";
                    var tokens = _tokenService.GenerateTokens(user, request.ClientId ?? "vehicle-web-app", clientType, scopes, request.Nonce);

                    var refreshToken = new RefreshToken
                    {
                        Id = Guid.NewGuid(),
                        UserId = user.Id,
                        TokenHash = _tokenService.HashRefreshToken(tokens.RefreshToken),
                        CreatedAtUtc = DateTime.UtcNow,
                        ExpiresAtUtc = tokens.RefreshTokenExpiresAtUtc
                    };

                    await _refreshTokenRepository.AddAsync(refreshToken);
                    await _refreshTokenRepository.SaveChangesAsync();

                    _auditLogger.LogAuthenticationSuccess(user.Id.ToString(), email, request.ClientId, clientIp);
                    return tokens;
                }

                case "refresh_token":
                {
                    if (string.IsNullOrWhiteSpace(request.RefreshToken))
                    {
                        throw new UnauthorizedAccessException("Refresh token is required.");
                    }

                    return await RefreshTokenAsync(new RefreshTokenRequest { RefreshToken = request.RefreshToken }, clientIp);
                }

                case "client_credentials":
                {
                    if (string.IsNullOrWhiteSpace(request.ClientId) || string.IsNullOrWhiteSpace(request.ClientSecret))
                    {
                        _auditLogger.LogAuthenticationFailure(request.ClientId ?? "empty", "Missing client credentials", clientIp);
                        throw new UnauthorizedAccessException("client_id and client_secret are required for client_credentials grant.");
                    }

                    var isValid = await _clientRepository.ValidateClientCredentialsAsync(request.ClientId, request.ClientSecret);
                    if (!isValid)
                    {
                        _auditLogger.LogAuthenticationFailure(request.ClientId, "Invalid client credentials", clientIp);
                        throw new UnauthorizedAccessException("Invalid client credentials.");
                    }

                    var client = await _clientRepository.GetByClientIdAsync(request.ClientId);
                    if (client == null || !client.IsActive)
                    {
                        _auditLogger.LogAuthenticationFailure(request.ClientId, "Client account inactive or not found", clientIp);
                        throw new UnauthorizedAccessException("Client account is inactive.");
                    }

                    var requestedScopes = !string.IsNullOrWhiteSpace(request.Scope)
                        ? request.Scope.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList()
                        : new List<string> { VehicleScopes.InternalAll };

                    var tokens = _tokenService.GenerateClientCredentialsToken(client, requestedScopes);
                    _auditLogger.LogServiceTokenIssued(client.ClientId, string.Join(" ", requestedScopes), clientIp);
                    return tokens;
                }

                default:
                    throw new InvalidOperationException($"Unsupported grant_type: '{request.GrantType}'. Supported grants: 'password', 'refresh_token', 'client_credentials'.");
            }
        }

        public async Task<UserInfoResponse> GetUserInfoAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new KeyNotFoundException("User not found.");
            }

            var roles = user.UserRoles?.Select(r => r.Role?.Name ?? "Customer").ToList() ?? new List<string> { "Customer" };
            if (!roles.Any()) roles.Add("Customer");

            var permissions = RolePermissionDefaults.GetPermissionsForRoles(roles).ToList();

            return new UserInfoResponse
            {
                Sub = user.Id.ToString(),
                Email = user.Email,
                EmailVerified = true,
                Name = $"{user.FirstName} {user.LastName}".Trim(),
                GivenName = user.FirstName,
                FamilyName = user.LastName,
                Roles = roles,
                Permissions = permissions,
                UpdatedAtUtc = user.UpdatedAtUtc ?? user.CreatedAtUtc
            };
        }
    }
}
