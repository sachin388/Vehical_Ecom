namespace VehicleEcom.Security.UserContext;

public interface ICurrentUserService
{
    string? UserId { get; }
    string? Email { get; }
    string? Name { get; }
    string? ClientId { get; }
    string? ClientType { get; }
    IReadOnlyList<string> Roles { get; }
    IReadOnlyList<string> Permissions { get; }
    IReadOnlyList<string> Scopes { get; }
    bool IsAuthenticated { get; }
    bool IsServiceAccount { get; }

    bool IsInRole(string role);
    bool HasPermission(string permission);
    bool HasScope(string scope);
}

