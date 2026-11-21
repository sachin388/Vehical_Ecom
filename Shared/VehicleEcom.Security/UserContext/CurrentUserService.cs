using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using VehicleEcom.Security.Constants;

namespace VehicleEcom.Security.UserContext;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

    public string? UserId =>
        User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? User?.FindFirst(VehicleClaimTypes.Subject)?.Value
        ?? User?.FindFirst("id")?.Value
        ?? _httpContextAccessor.HttpContext?.Request.Headers["X-User-Id"].FirstOrDefault();

    public string? Email =>
        User?.FindFirst(ClaimTypes.Email)?.Value
        ?? User?.FindFirst(VehicleClaimTypes.Email)?.Value
        ?? _httpContextAccessor.HttpContext?.Request.Headers["X-User-Email"].FirstOrDefault();

    public string? Name =>
        User?.FindFirst(ClaimTypes.Name)?.Value
        ?? User?.FindFirst(VehicleClaimTypes.Name)?.Value
        ?? User?.FindFirst("name")?.Value;

    public string? ClientId =>
        User?.FindFirst(VehicleClaimTypes.ClientId)?.Value
        ?? User?.FindFirst("client_id")?.Value
        ?? _httpContextAccessor.HttpContext?.Request.Headers["X-Client-Id"].FirstOrDefault();

    public string? ClientType =>
        User?.FindFirst(VehicleClaimTypes.ClientType)?.Value
        ?? User?.FindFirst("client_type")?.Value;

    public IReadOnlyList<string> Roles
    {
        get
        {
            if (User == null)
            {
                var headerRoles = _httpContextAccessor.HttpContext?.Request.Headers["X-User-Roles"].FirstOrDefault();
                if (!string.IsNullOrEmpty(headerRoles))
                {
                    return headerRoles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                }
                return Array.Empty<string>();
            }

            return User.FindAll(ClaimTypes.Role)
                .Concat(User.FindAll(VehicleClaimTypes.Role))
                .Concat(User.FindAll("role"))
                .Concat(User.FindAll("roles"))
                .Select(c => c.Value)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }

    public IReadOnlyList<string> Permissions
    {
        get
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (User != null)
            {
                foreach (var claim in User.FindAll(VehicleClaimTypes.Permission).Concat(User.FindAll("permission")))
                {
                    result.Add(claim.Value);
                }
            }

            var headerPerms = _httpContextAccessor.HttpContext?.Request.Headers["X-User-Permissions"].FirstOrDefault();
            if (!string.IsNullOrEmpty(headerPerms))
            {
                foreach (var p in headerPerms.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    result.Add(p);
                }
            }

            // Also include permissions mapped by roles
            foreach (var p in RolePermissionDefaults.GetPermissionsForRoles(Roles))
            {
                result.Add(p);
            }

            return result.ToList();
        }
    }

    public IReadOnlyList<string> Scopes
    {
        get
        {
            if (User == null) return Array.Empty<string>();

            return User.FindAll("scope")
                .Concat(User.FindAll(VehicleClaimTypes.Scope))
                .SelectMany(c => c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }

    public bool IsServiceAccount =>
        IsInRole(VehicleRoles.ServiceAccount) ||
        string.Equals(ClientType, "service", StringComparison.OrdinalIgnoreCase) ||
        Scopes.Any(s => s.EndsWith(".internal", StringComparison.OrdinalIgnoreCase) || s == VehicleScopes.InternalAll);

    public bool IsInRole(string role)
    {
        return Roles.Any(r => string.Equals(r, role, StringComparison.OrdinalIgnoreCase));
    }

    public bool HasPermission(string permission)
    {
        if (IsInRole(VehicleRoles.Admin) || IsInRole(VehicleRoles.SuperAdmin))
            return true;

        return Permissions.Any(p => string.Equals(p, permission, StringComparison.OrdinalIgnoreCase));
    }

    public bool HasScope(string scope)
    {
        return Scopes.Any(s => string.Equals(s, scope, StringComparison.OrdinalIgnoreCase));
    }
}

