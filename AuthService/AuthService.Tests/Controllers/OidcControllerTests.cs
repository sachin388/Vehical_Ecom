using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using AuthService.Api.Controllers;
using AuthService.Application.DTOs;
using AuthService.Application.Interfaces;
using AuthService.Infrastructure.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Moq;
using VehicleEcom.Security.Audit;
using VehicleEcom.Security.Constants;
using Xunit;

namespace AuthService.Tests.Controllers
{
    public class OidcControllerTests
    {
        private readonly Mock<IAuthService> _authServiceMock;
        private readonly Mock<IKeyMaterialService> _keyMaterialServiceMock;
        private readonly Mock<IConfiguration> _configMock;
        private readonly Mock<ISecurityAuditLogger> _auditLoggerMock;
        private readonly OidcController _controller;

        public OidcControllerTests()
        {
            _authServiceMock = new Mock<IAuthService>();
            _keyMaterialServiceMock = new Mock<IKeyMaterialService>();
            _configMock = new Mock<IConfiguration>();
            _auditLoggerMock = new Mock<ISecurityAuditLogger>();

            _configMock.Setup(c => c["Jwt:Issuer"]).Returns("http://localhost:5149");

            _controller = new OidcController(
                _authServiceMock.Object,
                _keyMaterialServiceMock.Object,
                _configMock.Object,
                _auditLoggerMock.Object);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            _controller.Request.Scheme = "http";
            _controller.Request.Host = new HostString("localhost:5149");
        }

        [Fact]
        public void GetOpenIdConfiguration_ReturnsValidOidcMetadata()
        {
            var result = _controller.GetOpenIdConfiguration();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var config = Assert.IsType<OpenIdConfigurationResponse>(okResult.Value);
            Assert.Equal("http://localhost:5149", config.Issuer);
            Assert.Equal("http://localhost:5149/connect/token", config.TokenEndpoint);
            Assert.Equal("http://localhost:5149/connect/userinfo", config.UserInfoEndpoint);
            Assert.Equal("http://localhost:5149/.well-known/jwks.json", config.JwksUri);
        }

        [Fact]
        public void GetJwks_ReturnsValidJsonWebKeySet()
        {
            var keySet = new JsonWebKeySet();
            keySet.Keys.Add(new JsonWebKey { Kid = "test-key-id", Kty = "RSA", Use = "sig" });
            _keyMaterialServiceMock.Setup(k => k.GetJsonWebKeySet()).Returns(keySet);

            var result = _controller.GetJwks();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedSet = Assert.IsType<JsonWebKeySet>(okResult.Value);
            Assert.Single(returnedSet.Keys);
            Assert.Equal("test-key-id", returnedSet.Keys[0].Kid);
        }

        [Fact]
        public async Task Token_ClientCredentials_ReturnsOkWithAccessToken()
        {
            var request = new OAuthTokenRequest
            {
                GrantType = "client_credentials",
                ClientId = "salesorder-service",
                ClientSecret = "SalesOrderServiceSecret2026!",
                Scope = "salesorder.internal"
            };

            var tokenResponse = new TokenResponse
            {
                AccessToken = "jwt-service-token",
                TokenType = "Bearer",
                ExpiresIn = 3600,
                Scope = "salesorder.internal"
            };

            _authServiceMock.Setup(s => s.HandleOAuthTokenRequestAsync(request, It.IsAny<string?>()))
                .ReturnsAsync(tokenResponse);

            var result = await _controller.Token(request, null);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<TokenResponse>(okResult.Value);
            Assert.Equal("jwt-service-token", response.AccessToken);
            Assert.Equal("Bearer", response.TokenType);
            Assert.Equal(3600, response.ExpiresIn);
        }

        [Fact]
        public async Task UserInfo_AuthenticatedUser_ReturnsOidcUserInfo()
        {
            var userId = Guid.NewGuid();
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Email, "jane@example.com")
            };
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));

            var expectedUserInfo = new UserInfoResponse
            {
                Sub = userId.ToString(),
                Email = "jane@example.com",
                Name = "Jane Doe",
                Roles = new[] { VehicleRoles.Customer },
                Permissions = new[] { VehiclePermissions.OrdersCreate, VehiclePermissions.OrdersRead }
            };

            _authServiceMock.Setup(s => s.GetUserInfoAsync(userId))
                .ReturnsAsync(expectedUserInfo);

            var result = await _controller.UserInfo();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var userInfo = Assert.IsType<UserInfoResponse>(okResult.Value);
            Assert.Equal(userId.ToString(), userInfo.Sub);
            Assert.Equal("Jane Doe", userInfo.Name);
            Assert.Contains(VehiclePermissions.OrdersCreate, userInfo.Permissions);
        }

        [Fact]
        public async Task Revoke_ValidToken_CallsLogoutAndReturnsOk()
        {
            _authServiceMock.Setup(s => s.LogoutAsync("my-refresh-token", It.IsAny<string?>()))
                .Returns(Task.CompletedTask);

            var result = await _controller.Revoke("my-refresh-token", null);

            var okResult = Assert.IsType<OkObjectResult>(result);
            _authServiceMock.Verify(s => s.LogoutAsync("my-refresh-token", It.IsAny<string?>()), Times.Once);
        }
    }
}
