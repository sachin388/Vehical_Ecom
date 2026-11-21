using AuthService.Application.DTOs;
using AuthService.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Security.Claims;
using VehicleEcom.Security.Constants;

namespace AuthService.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        private string? GetClientIp()
        {
            if (HttpContext == null) return null;
            return HttpContext.Connection?.RemoteIpAddress?.ToString()
                   ?? (Request?.Headers != null && Request.Headers.TryGetValue("X-Forwarded-For", out var fwd) ? fwd.FirstOrDefault() : null);
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            try
            {
                await _authService.RegisterAsync(request);

                return Ok(new
                {
                    message = "User registered successfully."
                });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new
                {
                    message = ex.Message
                });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var clientIp = GetClientIp();

            try
            {
                var result = clientIp != null
                    ? await _authService.LoginAsync(request, clientIp)
                    : await _authService.LoginAsync(request);

                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new
                {
                    message = ex.Message
                });
            }
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(RefreshTokenRequest request)
        {
            var clientIp = GetClientIp();

            try
            {
                var result = clientIp != null
                    ? await _authService.RefreshTokenAsync(request, clientIp)
                    : await _authService.RefreshTokenAsync(request);

                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new
                {
                    message = ex.Message
                });
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout(RefreshTokenRequest request)
        {
            var clientIp = GetClientIp();

            if (clientIp != null)
            {
                await _authService.LogoutAsync(request.RefreshToken, clientIp);
            }
            else
            {
                await _authService.LogoutAsync(request.RefreshToken);
            }

            return Ok(new
            {
                message = "Logged out successfully."
            });
        }

        [Authorize]
        [HttpGet("me")]
        public IActionResult Me()
        {
            var user = HttpContext?.User ?? User;
            var roles = user?.FindAll(ClaimTypes.Role)
                .Concat(user?.FindAll(VehicleClaimTypes.Role) ?? Enumerable.Empty<Claim>())
                .Select(x => x.Value)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? new List<string>();

            var permissions = user?.FindAll(VehicleClaimTypes.Permission)
                .Concat(user?.FindAll("permission") ?? Enumerable.Empty<Claim>())
                .Select(x => x.Value)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? new List<string>();

            if (!permissions.Any())
            {
                permissions = RolePermissionDefaults.GetPermissionsForRoles(roles).ToList();
            }

            return Ok(new
            {
                UserId = user?.FindFirstValue(ClaimTypes.NameIdentifier) ?? user?.FindFirstValue(VehicleClaimTypes.Subject),
                Email = user?.FindFirstValue(ClaimTypes.Email) ?? user?.FindFirstValue(VehicleClaimTypes.Email),
                Roles = roles,
                Permissions = permissions,
                ClientId = user?.FindFirstValue(VehicleClaimTypes.ClientId)
            });
        }
    }
}
