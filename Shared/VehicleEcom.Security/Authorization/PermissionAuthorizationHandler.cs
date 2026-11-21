using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using VehicleEcom.Security.Constants;

namespace VehicleEcom.Security.Authorization;

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionAuthorizationRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionAuthorizationRequirement requirement)
    {
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            return Task.CompletedTask;
        }

        // 1. Direct permission claim check (both "permission" and custom claim types)
        var hasDirectPermission = context.User.Claims.Any(c =>
            (c.Type == VehicleClaimTypes.Permission || c.Type == "permission" || c.Type == "permissions") &&
            string.Equals(c.Value, requirement.Permission, StringComparison.OrdinalIgnoreCase));

        if (hasDirectPermission)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // 2. Roles evaluation - Admins always succeed, or roles mapped via RolePermissionDefaults
        var roles = context.User.FindAll(ClaimTypes.Role)
            .Concat(context.User.FindAll(VehicleClaimTypes.Role))
            .Concat(context.User.FindAll("role"))
            .Concat(context.User.FindAll("roles"))
            .Select(c => c.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var role in roles)
        {
            if (string.Equals(role, VehicleRoles.Admin, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(role, VehicleRoles.SuperAdmin, StringComparison.OrdinalIgnoreCase))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            var grantedPermissions = RolePermissionDefaults.GetPermissionsForRole(role);
            if (grantedPermissions.Contains(requirement.Permission))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }
        }

        // 3. Scope evaluation for service accounts
        var scopes = context.User.FindAll("scope")
            .Concat(context.User.FindAll(VehicleClaimTypes.Scope))
            .SelectMany(c => c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Distinct(StringComparer.OrdinalIgnoreCase);

        if (scopes.Contains(VehicleScopes.InternalAll) || scopes.Contains(requirement.Permission))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        return Task.CompletedTask;
    }
}

