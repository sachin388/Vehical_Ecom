using Microsoft.AspNetCore.Authorization;

namespace VehicleEcom.Security.Authorization;

public sealed class PermissionAuthorizationRequirement : IAuthorizationRequirement
{
    public PermissionAuthorizationRequirement(string permission)
    {
        Permission = permission ?? throw new ArgumentNullException(nameof(permission));
    }

    public string Permission { get; }
}

