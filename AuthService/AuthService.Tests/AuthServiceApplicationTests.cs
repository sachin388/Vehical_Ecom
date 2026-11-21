using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AuthService.Application.DTOs;
using AuthService.Application.Interfaces;
using AuthService.Application.Services;
using AuthService.Domain.Entities;
using Moq;
using VehicleEcom.Security.Audit;
using VehicleEcom.Security.Constants;
using Xunit;

namespace AuthService.Tests.Services
{
    public class AuthServiceApplicationTests
    {
        private readonly Mock<IUserRepository> _userRepoMock;
        private readonly Mock<IRoleRepository> _roleRepoMock;
        private readonly Mock<IRefreshTokenRepository> _refreshTokenRepoMock;
        private readonly Mock<IClientRepository> _clientRepoMock;
        private readonly Mock<IPasswordService> _passwordServiceMock;
        private readonly Mock<ITokenService> _tokenServiceMock;
        private readonly Mock<ISecurityAuditLogger> _auditLoggerMock;
        private readonly AuthService.Application.Services.AuthService _authService;

        public AuthServiceApplicationTests()
        {
            _userRepoMock = new Mock<IUserRepository>();
            _roleRepoMock = new Mock<IRoleRepository>();
            _refreshTokenRepoMock = new Mock<IRefreshTokenRepository>();
            _clientRepoMock = new Mock<IClientRepository>();
            _passwordServiceMock = new Mock<IPasswordService>();
            _tokenServiceMock = new Mock<ITokenService>();
            _auditLoggerMock = new Mock<ISecurityAuditLogger>();

            _authService = new AuthService.Application.Services.AuthService(
                _userRepoMock.Object,
                _roleRepoMock.Object,
                _refreshTokenRepoMock.Object,
                _clientRepoMock.Object,
                _passwordServiceMock.Object,
                _tokenServiceMock.Object,
                _auditLoggerMock.Object);
        }

        [Fact]
        public async Task RegisterAsync_ValidRequest_CreatesUserWithRole()
        {
            var request = new RegisterRequest
            {
                Email = "test@example.com",
                Password = "Password123!",
                FirstName = "Alice",
                LastName = "Smith"
            };

            _userRepoMock.Setup(r => r.GetByEmailAsync("test@example.com"))
                .ReturnsAsync((User?)null);
            _passwordServiceMock.Setup(p => p.HashPassword(It.IsAny<User>(), "Password123!"))
                .Returns("hashed_secret");
            _roleRepoMock.Setup(r => r.GetByNameAsync("Customer"))
                .ReturnsAsync(new Role { Id = Guid.NewGuid(), Name = "Customer" });

            await _authService.RegisterAsync(request);

            _userRepoMock.Verify(r => r.AddAsync(It.Is<User>(u =>
                u.Email == "test@example.com" &&
                u.FirstName == "Alice" &&
                u.LastName == "Smith" &&
                u.PasswordHash == "hashed_secret")), Times.Once);
            _userRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_ExistingEmail_ThrowsInvalidOperationException()
        {
            var request = new RegisterRequest
            {
                Email = "test@example.com",
                Password = "Password123!",
                FirstName = "Alice",
                LastName = "Smith"
            };

            _userRepoMock.Setup(r => r.GetByEmailAsync("test@example.com"))
                .ReturnsAsync(new User { Email = "test@example.com" });

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _authService.RegisterAsync(request));
        }

        [Fact]
        public async Task LoginAsync_ValidCredentials_ReturnsAuthResponse()
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@example.com",
                PasswordHash = "hashed_secret",
                IsActive = true,
                FirstName = "Alice",
                LastName = "Smith"
            };

            var request = new LoginRequest
            {
                Email = "test@example.com",
                Password = "Password123!"
            };

            _userRepoMock.Setup(r => r.GetByEmailAsync("test@example.com"))
                .ReturnsAsync(user);
            _passwordServiceMock.Setup(p => p.VerifyPassword(user, "hashed_secret", "Password123!"))
                .Returns(true);
            _tokenServiceMock.Setup(t => t.GenerateTokens(user, "vehicle-web-app", "web", It.IsAny<IEnumerable<string>>(), null))
                .Returns(new TokenResponse
                {
                    AccessToken = "jwt_access_token",
                    RefreshToken = "raw_refresh_token",
                    AccessTokenExpiresAtUtc = DateTime.UtcNow.AddMinutes(15),
                    RefreshTokenExpiresAtUtc = DateTime.UtcNow.AddDays(7)
                });
            _tokenServiceMock.Setup(t => t.HashRefreshToken("raw_refresh_token"))
                .Returns("hashed_refresh_token");

            var result = await _authService.LoginAsync(request);

            Assert.NotNull(result);
            Assert.Equal("jwt_access_token", result.AccessToken);
            Assert.Equal("raw_refresh_token", result.RefreshToken);
            _refreshTokenRepoMock.Verify(r => r.AddAsync(It.Is<RefreshToken>(rt => rt.TokenHash == "hashed_refresh_token")), Times.Once);
            _refreshTokenRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task LoginAsync_InvalidPassword_ThrowsUnauthorizedAccessException()
        {
            var user = new User
            {
                Email = "test@example.com",
                PasswordHash = "hashed_secret",
                IsActive = true
            };

            var request = new LoginRequest
            {
                Email = "test@example.com",
                Password = "WrongPassword"
            };

            _userRepoMock.Setup(r => r.GetByEmailAsync("test@example.com"))
                .ReturnsAsync(user);
            _passwordServiceMock.Setup(p => p.VerifyPassword(user, "hashed_secret", "WrongPassword"))
                .Returns(false);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _authService.LoginAsync(request));
        }

        [Fact]
        public async Task RefreshTokenAsync_ValidToken_ReturnsNewTokens_WithSingleUseRotation()
        {
            var user = new User { Id = Guid.NewGuid(), Email = "test@example.com", IsActive = true };
            var existingToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                User = user,
                TokenHash = "hashed_refresh_token",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-1),
                ExpiresAtUtc = DateTime.UtcNow.AddDays(6),
                RevokedAtUtc = null
            };

            _tokenServiceMock.Setup(t => t.HashRefreshToken("raw_refresh_token"))
                .Returns("hashed_refresh_token");
            _refreshTokenRepoMock.Setup(r => r.GetByHashAsync("hashed_refresh_token"))
                .ReturnsAsync(existingToken);

            var newTokens = new TokenResponse
            {
                AccessToken = "new_jwt_access_token",
                RefreshToken = "new_raw_refresh_token",
                AccessTokenExpiresAtUtc = DateTime.UtcNow.AddMinutes(15),
                RefreshTokenExpiresAtUtc = DateTime.UtcNow.AddDays(7)
            };
            _tokenServiceMock.Setup(t => t.GenerateTokens(user))
                .Returns(newTokens);
            _tokenServiceMock.Setup(t => t.HashRefreshToken("new_raw_refresh_token"))
                .Returns("new_hashed_refresh_token");

            var result = await _authService.RefreshTokenAsync(new RefreshTokenRequest { RefreshToken = "raw_refresh_token" });

            Assert.NotNull(result);
            Assert.Equal("new_jwt_access_token", result.AccessToken);
            Assert.Equal("new_raw_refresh_token", result.RefreshToken);
            Assert.NotNull(existingToken.RevokedAtUtc);
            _refreshTokenRepoMock.Verify(r => r.AddAsync(It.Is<RefreshToken>(rt => rt.TokenHash == "new_hashed_refresh_token")), Times.Once);
            _refreshTokenRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task RefreshTokenAsync_RevokedTokenReplay_ThrowsUnauthorized_DetectsReuse()
        {
            var user = new User { Id = Guid.NewGuid(), Email = "test@example.com", IsActive = true };
            var revokedToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                User = user,
                TokenHash = "hashed_replayed_token",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-2),
                ExpiresAtUtc = DateTime.UtcNow.AddDays(5),
                RevokedAtUtc = DateTime.UtcNow.AddDays(-1) // Already revoked!
            };

            _tokenServiceMock.Setup(t => t.HashRefreshToken("replayed_token"))
                .Returns("hashed_replayed_token");
            _refreshTokenRepoMock.Setup(r => r.GetByHashAsync("hashed_replayed_token"))
                .ReturnsAsync(revokedToken);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _authService.RefreshTokenAsync(new RefreshTokenRequest { RefreshToken = "replayed_token" }));

            _auditLoggerMock.Verify(a => a.LogAuthenticationFailure(
                user.Id.ToString(),
                It.Is<string>(s => s.Contains("reuse")),
                It.IsAny<string?>()), Times.Once);
        }

        [Fact]
        public async Task HandleOAuthTokenRequestAsync_ClientCredentials_ReturnsM2MToken()
        {
            var client = new Client
            {
                ClientId = "salesorder-service",
                ClientSecret = "secret123",
                ClientType = "service",
                IsActive = true,
                AllowedGrantTypes = "client_credentials",
                AllowedScopes = "salesorder.internal,internal.all"
            };

            var request = new OAuthTokenRequest
            {
                GrantType = "client_credentials",
                ClientId = "salesorder-service",
                ClientSecret = "secret123",
                Scope = "salesorder.internal"
            };

            _clientRepoMock.Setup(c => c.ValidateClientCredentialsAsync("salesorder-service", "secret123"))
                .ReturnsAsync(true);
            _clientRepoMock.Setup(c => c.GetByClientIdAsync("salesorder-service"))
                .ReturnsAsync(client);

            var expectedResponse = new TokenResponse
            {
                AccessToken = "service_m2m_jwt_token",
                TokenType = "Bearer",
                ExpiresIn = 3600,
                Scope = "salesorder.internal"
            };

            _tokenServiceMock.Setup(t => t.GenerateClientCredentialsToken(client, It.IsAny<IEnumerable<string>>()))
                .Returns(expectedResponse);

            var result = await _authService.HandleOAuthTokenRequestAsync(request, "127.0.0.1");

            Assert.NotNull(result);
            Assert.Equal("service_m2m_jwt_token", result.AccessToken);
            Assert.Equal("Bearer", result.TokenType);
            Assert.Equal("salesorder.internal", result.Scope);
            _auditLoggerMock.Verify(a => a.LogServiceTokenIssued("salesorder-service", "salesorder.internal", "127.0.0.1"), Times.Once);
        }

        [Fact]
        public async Task GetUserInfoAsync_ExistingUser_ReturnsStandardOidcClaims()
        {
            var userId = Guid.NewGuid();
            var role = new Role { Id = Guid.NewGuid(), Name = VehicleRoles.Customer };
            var user = new User
            {
                Id = userId,
                FirstName = "Jane",
                LastName = "Doe",
                Email = "jane.doe@example.com",
                IsActive = true,
                UserRoles = new List<UserRole>
                {
                    new UserRole { UserId = userId, RoleId = role.Id, Role = role }
                }
            };

            _userRepoMock.Setup(u => u.GetByIdAsync(userId))
                .ReturnsAsync(user);

            var result = await _authService.GetUserInfoAsync(userId);

            Assert.NotNull(result);
            Assert.Equal(userId.ToString(), result.Sub);
            Assert.Equal("Jane Doe", result.Name);
            Assert.Equal("jane.doe@example.com", result.Email);
            Assert.Contains(VehicleRoles.Customer, result.Roles);
            Assert.Contains(VehiclePermissions.OrdersCreate, result.Permissions);
        }
    }
}
