using AuthService.Application.DTOs;
using AuthService.Application.Interfaces;
using AuthService.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;
using VehicleEcom.Security.Audit;
using VehicleEcom.Security.Constants;

namespace AuthService.Api.Controllers
{
    [ApiController]
    public class OidcController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IKeyMaterialService _keyMaterialService;
        private readonly IConfiguration _configuration;
        private readonly ISecurityAuditLogger _auditLogger;

        public OidcController(
            IAuthService authService,
            IKeyMaterialService keyMaterialService,
            IConfiguration configuration,
            ISecurityAuditLogger auditLogger)
        {
            _authService = authService;
            _keyMaterialService = keyMaterialService;
            _configuration = configuration;
            _auditLogger = auditLogger;
        }

        private string? GetClientIp()
        {
            if (HttpContext == null) return null;
            return HttpContext.Connection?.RemoteIpAddress?.ToString()
                   ?? (Request?.Headers != null && Request.Headers.TryGetValue("X-Forwarded-For", out var fwd) ? fwd.FirstOrDefault() : null);
        }

        [HttpGet("/.well-known/openid-configuration")]
        [HttpGet("api/auth/.well-known/openid-configuration")]
        [HttpGet("connect/.well-known/openid-configuration")]
        public IActionResult GetOpenIdConfiguration()
        {
            var baseUrl = Request?.Host.HasValue == true
                ? $"{Request.Scheme}://{Request.Host}"
                : "http://localhost:5149";
            var issuer = _configuration["Jwt:Issuer"] ?? baseUrl;

            var config = new OpenIdConfigurationResponse
            {
                Issuer = issuer,
                AuthorizationEndpoint = $"{baseUrl}/connect/authorize",
                TokenEndpoint = $"{baseUrl}/connect/token",
                UserInfoEndpoint = $"{baseUrl}/connect/userinfo",
                JwksUri = $"{baseUrl}/.well-known/jwks.json",
                RevocationEndpoint = $"{baseUrl}/connect/revocation",
                ResponseTypesSupported = new[] { "code", "token", "id_token", "code id_token" },
                SubjectTypesSupported = new[] { "public" },
                IdTokenSigningAlgValuesSupported = new[] { "RS256", "HS256" },
                ScopesSupported = VehicleScopes.All,
                TokenEndpointAuthMethodsSupported = new[] { "client_secret_basic", "client_secret_post" },
                ClaimsSupported = new[]
                {
                    "sub", "name", "given_name", "family_name", "email",
                    "email_verified", "role", "permission", "client_id", "scope", "token_use"
                },
                GrantTypesSupported = new[]
                {
                    "authorization_code", "client_credentials", "password", "refresh_token"
                }
            };

            return Ok(config);
        }

        [HttpGet("/.well-known/jwks.json")]
        [HttpGet("api/auth/.well-known/jwks.json")]
        public IActionResult GetJwks()
        {
            var jwks = _keyMaterialService.GetJsonWebKeySet();
            return Ok(jwks);
        }

        [HttpPost("/connect/token")]
        [HttpPost("api/auth/oauth/token")]
        [Consumes("application/x-www-form-urlencoded", "application/json")]
        public async Task<IActionResult> Token([FromForm] OAuthTokenRequest? formRequest, [FromBody] OAuthTokenRequest? bodyRequest)
        {
            var request = formRequest ?? bodyRequest ?? new OAuthTokenRequest();

            // Extract HTTP Basic Auth if present
            if (string.IsNullOrWhiteSpace(request.ClientId) && Request?.Headers != null && Request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                var authStr = authHeader.ToString();
                if (authStr.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var encoded = authStr.Substring("Basic ".Length).Trim();
                        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
                        var parts = decoded.Split(':', 2);
                        if (parts.Length == 2)
                        {
                            request.ClientId = parts[0];
                            request.ClientSecret = parts[1];
                        }
                    }
                    catch
                    {
                        // Ignore malformed basic auth header
                    }
                }
            }

            var clientIp = GetClientIp() ?? "unknown";

            try
            {
                var tokenResult = await _authService.HandleOAuthTokenRequestAsync(request, clientIp);
                return Ok(tokenResult);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new
                {
                    error = "invalid_grant",
                    error_description = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    error = "unsupported_grant_type",
                    error_description = ex.Message
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    error = "invalid_request",
                    error_description = ex.Message
                });
            }
        }

        [Authorize]
        [HttpGet("/connect/userinfo")]
        [HttpPost("/connect/userinfo")]
        public async Task<IActionResult> UserInfo()
        {
            var user = HttpContext?.User ?? User;
            var sub = user?.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? user?.FindFirstValue(VehicleClaimTypes.Subject);

            if (string.IsNullOrEmpty(sub) || !Guid.TryParse(sub, out var userId))
            {
                return Unauthorized(new { error = "invalid_token", error_description = "Invalid subject in access token" });
            }

            try
            {
                var userInfo = await _authService.GetUserInfoAsync(userId);
                return Ok(userInfo);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { error = "user_not_found" });
            }
        }

        [HttpPost("/connect/revocation")]
        [Consumes("application/x-www-form-urlencoded", "application/json")]
        public async Task<IActionResult> Revoke([FromForm(Name = "token")] string? formToken, [FromBody] RefreshTokenRequest? jsonRequest)
        {
            var token = formToken ?? jsonRequest?.RefreshToken;
            var clientIp = GetClientIp();

            if (!string.IsNullOrWhiteSpace(token))
            {
                await _authService.LogoutAsync(token, clientIp);
            }

            return Ok(new { message = "Token successfully revoked." });
        }
    }
}
