using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AuthService.Application.DTOs;
using AuthService.Application.Interfaces;
using AuthService.Application.Services;
using AuthService.Domain.Entities;
using Moq;
using Xunit;

namespace AuthService.Tests.Services
{
    public class AuthServiceApplicationTests
    {
        private readonly Mock<IUserRepository> _userRepoMock;
        private readonly Mock<IRoleRepository> _roleRepoMock;
        private readonly Mock<IRefreshTokenRepository> _refreshTokenRepoMock;
        private readonly Mock<IPasswordService> _passwordServiceMock;
        private readonly Mock<ITokenService> _tokenServiceMock;
        private readonly AuthService.Application.Services.AuthService _authService;

        public AuthServiceApplicationTests()
        {
            _userRepoMock = new Mock<IUserRepository>();
            _roleRepoMock = new Mock<IRoleRepository>();
            _refreshTokenRepoMock = new Mock<IRefreshTokenRepository>();
            _passwordServiceMock = new Mock<IPasswordService>();
            _tokenServiceMock = new Mock<ITokenService>();

            _authService = new AuthService.Application.Services.AuthService(
                _userRepoMock.Object,
                _roleRepoMock.Object,
                _refreshTokenRepoMock.Object,
                _passwordServiceMock.Object,
                _tokenServiceMock.Object);
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
            _tokenServiceMock.Setup(t => t.GenerateTokens(user))
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
        public async Task LoginAsync_InactiveUser_ThrowsUnauthorizedAccessException()
        {
            var user = new User
            {
                Email = "test@example.com",
                PasswordHash = "hashed_secret",
                IsActive = false
            };

            var request = new LoginRequest
            {
                Email = "test@example.com",
                Password = "Password123!"
            };

            _userRepoMock.Setup(r => r.GetByEmailAsync("test@example.com"))
                .ReturnsAsync(user);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _authService.LoginAsync(request));
        }

        [Fact]
        public async Task RefreshTokenAsync_ValidToken_ReturnsNewTokens()
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
        public async Task LogoutAsync_ValidRefreshToken_RevokesToken()
        {
            var existingToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                TokenHash = "hashed_refresh_token",
                ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
                RevokedAtUtc = null
            };

            _tokenServiceMock.Setup(t => t.HashRefreshToken("raw_refresh_token"))
                .Returns("hashed_refresh_token");
            _refreshTokenRepoMock.Setup(r => r.GetByHashAsync("hashed_refresh_token"))
                .ReturnsAsync(existingToken);

            await _authService.LogoutAsync("raw_refresh_token");

            Assert.NotNull(existingToken.RevokedAtUtc);
            _refreshTokenRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }
    }
}
