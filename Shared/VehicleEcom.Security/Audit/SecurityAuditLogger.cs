using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace VehicleEcom.Security.Audit;

public sealed class SecurityAuditLogger : ISecurityAuditLogger
{
    private readonly ILogger<SecurityAuditLogger> _logger;

    public SecurityAuditLogger(ILogger<SecurityAuditLogger> logger)
    {
        _logger = logger;
    }

    public void LogAuthenticationSuccess(string userId, string email, string? clientId, string? clientIp)
    {
        var traceId = Activity.Current?.TraceId.ToString() ?? "none";
        _logger.LogInformation(
            "[SECURITY-AUDIT] [AUTH_SUCCESS] UserId={UserId} Email={Email} ClientId={ClientId} ClientIp={ClientIp} TraceId={TraceId} Timestamp={TimestampUtc}",
            userId, email, clientId ?? "unknown", clientIp ?? "unknown", traceId, DateTime.UtcNow);
    }

    public void LogAuthenticationFailure(string emailOrClientId, string reason, string? clientIp)
    {
        var traceId = Activity.Current?.TraceId.ToString() ?? "none";
        _logger.LogWarning(
            "[SECURITY-AUDIT] [AUTH_FAILURE] Target={Target} Reason={Reason} ClientIp={ClientIp} TraceId={TraceId} Timestamp={TimestampUtc}",
            emailOrClientId, reason, clientIp ?? "unknown", traceId, DateTime.UtcNow);
    }

    public void LogTokenRefresh(string userId, string? clientId, string? clientIp)
    {
        var traceId = Activity.Current?.TraceId.ToString() ?? "none";
        _logger.LogInformation(
            "[SECURITY-AUDIT] [TOKEN_REFRESH] UserId={UserId} ClientId={ClientId} ClientIp={ClientIp} TraceId={TraceId} Timestamp={TimestampUtc}",
            userId, clientId ?? "unknown", clientIp ?? "unknown", traceId, DateTime.UtcNow);
    }

    public void LogTokenRevocation(string userIdOrToken, string reason, string? clientIp)
    {
        var traceId = Activity.Current?.TraceId.ToString() ?? "none";
        _logger.LogInformation(
            "[SECURITY-AUDIT] [TOKEN_REVOCATION] Target={Target} Reason={Reason} ClientIp={ClientIp} TraceId={TraceId} Timestamp={TimestampUtc}",
            userIdOrToken, reason, clientIp ?? "unknown", traceId, DateTime.UtcNow);
    }

    public void LogServiceTokenIssued(string clientId, string scopes, string? clientIp)
    {
        var traceId = Activity.Current?.TraceId.ToString() ?? "none";
        _logger.LogInformation(
            "[SECURITY-AUDIT] [SERVICE_TOKEN_ISSUED] ClientId={ClientId} Scopes={Scopes} ClientIp={ClientIp} TraceId={TraceId} Timestamp={TimestampUtc}",
            clientId, scopes, clientIp ?? "unknown", traceId, DateTime.UtcNow);
    }

    public void LogAuthorizationDenied(string? userId, string path, string method, string requirement, string? clientIp)
    {
        var traceId = Activity.Current?.TraceId.ToString() ?? "none";
        _logger.LogWarning(
            "[SECURITY-AUDIT] [ACCESS_DENIED] UserId={UserId} Method={Method} Path={Path} Requirement={Requirement} ClientIp={ClientIp} TraceId={TraceId} Timestamp={TimestampUtc}",
            userId ?? "anonymous", method, path, requirement, clientIp ?? "unknown", traceId, DateTime.UtcNow);
    }
}

