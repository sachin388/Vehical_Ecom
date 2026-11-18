using System.Security.Claims;

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
                         ?? context.User.FindFirst("sub")?.Value
                         ?? context.User.FindFirst("id")?.Value;

            var email = context.User.FindFirst(ClaimTypes.Email)?.Value
                        ?? context.User.FindFirst("email")?.Value;

            var roles = context.User.FindAll(ClaimTypes.Role)
                .Concat(context.User.FindAll("role"))
                .Select(c => c.Value)
                .Distinct();

            if (!string.IsNullOrEmpty(userId))
            {
                context.Request.Headers["X-User-Id"] = userId;
            }

            if (!string.IsNullOrEmpty(email))
            {
                context.Request.Headers["X-User-Email"] = email;
            }

            var rolesString = string.Join(",", roles);
            if (!string.IsNullOrEmpty(rolesString))
            {
                context.Request.Headers["X-User-Roles"] = rolesString;
            }
        }

        await _next(context);
    }
}
