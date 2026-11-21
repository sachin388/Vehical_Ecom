using System.Security.Claims;
using VehicleEcom.Security.Constants;

namespace ApiGateway.Middleware;

public class UserContextRelayMiddleware
{
    private readonly RequestDelegate _next;

    public UserContextRelayMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? context.User.FindFirst(VehicleClaimTypes.Subject)?.Value
                         ?? context.User.FindFirst("id")?.Value;

            var email = context.User.FindFirst(ClaimTypes.Email)?.Value
                        ?? context.User.FindFirst(VehicleClaimTypes.Email)?.Value;

            var roles = context.User.FindAll(ClaimTypes.Role)
                .Concat(context.User.FindAll(VehicleClaimTypes.Role))
                .Concat(context.User.FindAll("role"))
                .Concat(context.User.FindAll("roles"))
                .Select(c => c.Value)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var permissions = context.User.FindAll(VehicleClaimTypes.Permission)
                .Concat(context.User.FindAll("permission"))
                .Concat(context.User.FindAll("permissions"))
                .Select(c => c.Value)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (!permissions.Any() && roles.Any())
            {
                permissions = RolePermissionDefaults.GetPermissionsForRoles(roles).ToList();
            }

            var clientId = context.User.FindFirst(VehicleClaimTypes.ClientId)?.Value
                           ?? context.User.FindFirst("client_id")?.Value;

            var clientType = context.User.FindFirst(VehicleClaimTypes.ClientType)?.Value
                             ?? context.User.FindFirst("client_type")?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                context.Request.Headers["X-User-Id"] = userId;
            }

            if (!string.IsNullOrEmpty(email))
            {
                context.Request.Headers["X-User-Email"] = email;
            }

            if (roles.Any())
            {
                context.Request.Headers["X-User-Roles"] = string.Join(",", roles);
            }

            if (permissions.Any())
            {
                context.Request.Headers["X-User-Permissions"] = string.Join(",", permissions);
            }

            if (!string.IsNullOrEmpty(clientId))
            {
                context.Request.Headers["X-Client-Id"] = clientId;
            }

            if (!string.IsNullOrEmpty(clientType))
            {
                context.Request.Headers["X-Client-Type"] = clientType;
            }
        }

        await _next(context);
    }
}
