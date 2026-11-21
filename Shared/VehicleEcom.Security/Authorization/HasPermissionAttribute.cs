using Microsoft.AspNetCore.Authorization;

namespace VehicleEcom.Security.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    public const string PolicyPrefix = "permission:";

    public HasPermissionAttribute(string permission)
        : base(policy: $"{PolicyPrefix}{permission}")
    {
        Permission = permission;
    }

    public string Permission { get; }
}

