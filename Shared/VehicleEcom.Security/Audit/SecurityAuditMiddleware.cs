using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using VehicleEcom.Security.Constants;

namespace VehicleEcom.Security.Audit;

public sealed class SecurityAuditMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ISecurityAuditLogger _auditLogger;
    private readonly ILogger<SecurityAuditMiddleware> _logger;

    public SecurityAuditMiddleware(
        RequestDelegate next,
        ISecurityAuditLogger auditLogger,
        ILogger<SecurityAuditMiddleware> logger)
    {
        _next = next;
        _auditLogger = auditLogger;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);

        var statusCode = context.Response.StatusCode;
        if (statusCode == StatusCodes.Status401Unauthorized || statusCode == StatusCodes.Status403Forbidden)
        {
            var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? context.User?.FindFirst(VehicleClaimTypes.Subject)?.Value
                         ?? context.Request.Headers["X-User-Id"].FirstOrDefault();

            var clientIp = context.Connection.RemoteIpAddress?.ToString()
                           ?? context.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                           ?? "unknown";

            var path = context.Request.Path.Value ?? "/";
            var method = context.Request.Method;
            var reason = statusCode == StatusCodes.Status401Unauthorized
                ? "Unauthorized (Missing or Invalid Token)"
                : "Forbidden (Insufficient Role or Permission)";

            _auditLogger.LogAuthorizationDenied(userId, path, method, reason, clientIp);
        }
    }
}

