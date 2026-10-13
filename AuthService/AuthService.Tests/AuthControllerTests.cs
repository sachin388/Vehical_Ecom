using System.Security.Claims;
using AuthService.Api.Controllers;
using AuthService.Application.DTOs;
using AuthService.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace AuthService.Tests.Controllers
{
    public class AuthControllerTests
    {
        private readonly Mock<IAuthService> _authServiceMock;
        private readonly AuthController _controller;

        public AuthControllerTests()
        {
            _authServiceMock = new Mock<IAuthService>();
            _controller = new AuthController(_authServiceMock.Object);
        }

        [Fact]
        public async Task Register_ValidRequest_ReturnsOkResult()
        {
            // Arrange
            var request = new RegisterRequest
            {
                Email = "user@example.com",
                Password = "Password123!",
                FirstName = "John",
                LastName = "Doe"
            };
            _authServiceMock
                .Setup(s => s.RegisterAsync(request))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Register(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            _authServiceMock.Verify(s => s.RegisterAsync(request), Times.Once);
        }

        [Fact]
        public async Task Register_UserAlreadyExists_ReturnsConflictResult()
        {
            // Arrange
            var request = new RegisterRequest
            {
                Email = "existing@example.com",
                Password = "Password123!"
            };
            _authServiceMock
                .Setup(s => s.RegisterAsync(request))
                .ThrowsAsync(new InvalidOperationException("User already exists"));

            // Act
            var result = await _controller.Register(request);

            // Assert
            var conflictResult = Assert.IsType<ConflictObjectResult>(result);
            Assert.Equal(StatusCodes.Status409Conflict, conflictResult.StatusCode);
        }

        [Fact]
        public async Task Login_ValidCredentials_ReturnsOkWithTokenResponse()
        {
            // Arrange
            var request = new LoginRequest
            {
                Email = "user@example.com",
                Password = "Password123!"
            };
            var tokenResponse = new TokenResponse
            {
                AccessToken = "valid_access_token",
                RefreshToken = "valid_refresh_token",
                AccessTokenExpiresAtUtc = DateTime.UtcNow.AddHours(1),
                RefreshTokenExpiresAtUtc = DateTime.UtcNow.AddDays(7)
            };
            _authServiceMock
                .Setup(s => s.LoginAsync(request))
                .ReturnsAsync(tokenResponse);

            // Act
            var result = await _controller.Login(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            Assert.Equal(tokenResponse, okResult.Value);
        }

        [Fact]
        public async Task Login_InvalidCredentials_ReturnsUnauthorizedResult()
        {
            // Arrange
            var request = new LoginRequest
            {
                Email = "user@example.com",
                Password = "WrongPassword"
            };
            _authServiceMock
                .Setup(s => s.LoginAsync(request))
                .ThrowsAsync(new UnauthorizedAccessException("Invalid credentials"));

            // Act
            var result = await _controller.Login(request);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, unauthorizedResult.StatusCode);
        }

        [Fact]
        public async Task Refresh_ValidRefreshToken_ReturnsOkWithTokenResponse()
        {
            // Arrange
            var request = new RefreshTokenRequest
            {
                RefreshToken = "valid_refresh_token"
            };
            var tokenResponse = new TokenResponse
            {
                AccessToken = "new_access_token",
                RefreshToken = "new_refresh_token"
            };
            _authServiceMock
                .Setup(s => s.RefreshTokenAsync(request))
                .ReturnsAsync(tokenResponse);

            // Act
            var result = await _controller.Refresh(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            Assert.Equal(tokenResponse, okResult.Value);
        }

        [Fact]
        public async Task Refresh_InvalidRefreshToken_ReturnsUnauthorizedResult()
        {
            // Arrange
            var request = new RefreshTokenRequest
            {
                RefreshToken = "expired_token"
            };
            _authServiceMock
                .Setup(s => s.RefreshTokenAsync(request))
                .ThrowsAsync(new UnauthorizedAccessException("Token expired"));

            // Act
            var result = await _controller.Refresh(request);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, unauthorizedResult.StatusCode);
        }

        [Fact]
        public async Task Logout_ValidToken_ReturnsOkResult()
        {
            // Arrange
            var request = new RefreshTokenRequest
            {
                RefreshToken = "valid_refresh_token"
            };
            _authServiceMock
                .Setup(s => s.LogoutAsync(request.RefreshToken))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Logout(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            _authServiceMock.Verify(s => s.LogoutAsync(request.RefreshToken), Times.Once);
        }

        [Fact]
        public void Me_AuthenticatedUser_ReturnsOkWithUserClaims()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "user-123"),
                new Claim(ClaimTypes.Email, "test@example.com"),
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim(ClaimTypes.Role, "User")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            // Act
            var result = _controller.Me();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            Assert.NotNull(okResult.Value);
        }
    }
}
