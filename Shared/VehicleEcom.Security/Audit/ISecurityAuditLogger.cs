namespace VehicleEcom.Security.Audit;

public interface ISecurityAuditLogger
{
    void LogAuthenticationSuccess(string userId, string email, string? clientId, string? clientIp);
    void LogAuthenticationFailure(string emailOrClientId, string reason, string? clientIp);
    void LogTokenRefresh(string userId, string? clientId, string? clientIp);
    void LogTokenRevocation(string userIdOrToken, string reason, string? clientIp);
    void LogServiceTokenIssued(string clientId, string scopes, string? clientIp);
    void LogAuthorizationDenied(string? userId, string path, string method, string requirement, string? clientIp);
}

