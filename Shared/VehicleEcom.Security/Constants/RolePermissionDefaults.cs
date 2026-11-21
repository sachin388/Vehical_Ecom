namespace VehicleEcom.Security.Constants;

public static class RolePermissionDefaults
{
    private static readonly Dictionary<string, HashSet<string>> RolePermissions = new(StringComparer.OrdinalIgnoreCase)
    {
        [VehicleRoles.Customer] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            VehiclePermissions.OrdersRead,
            VehiclePermissions.OrdersCreate,
            VehiclePermissions.OrdersCancel,
            VehiclePermissions.DealersRead,
            VehiclePermissions.ItemsRead,
            VehiclePermissions.PricingRead,
            VehiclePermissions.PriceBreakupRead,
            VehiclePermissions.AccessoriesRead,
            VehiclePermissions.PaymentsProcess,
            VehiclePermissions.PaymentsRead,
            VehiclePermissions.NotificationsRead
        },

        [VehicleRoles.Dealer] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            VehiclePermissions.OrdersRead,
            VehiclePermissions.DealersRead,
            VehiclePermissions.InventoryRead,
            VehiclePermissions.InventoryManage,
            VehiclePermissions.ItemsRead,
            VehiclePermissions.AccessoriesRead,
            VehiclePermissions.PricingRead,
            VehiclePermissions.PriceBreakupRead,
            VehiclePermissions.NotificationsRead
        },

        [VehicleRoles.DealershipSalesManager] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            VehiclePermissions.OrdersRead,
            VehiclePermissions.OrdersManage,
            VehiclePermissions.DealersRead,
            VehiclePermissions.InventoryRead,
            VehiclePermissions.InventoryManage,
            VehiclePermissions.ItemsRead,
            VehiclePermissions.AccessoriesRead,
            VehiclePermissions.PricingRead,
            VehiclePermissions.PriceBreakupRead,
            VehiclePermissions.NotificationsRead
        },

        [VehicleRoles.DealerPrincipal] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            VehiclePermissions.OrdersRead,
            VehiclePermissions.OrdersManage,
            VehiclePermissions.DealersRead,
            VehiclePermissions.DealersManage,
            VehiclePermissions.InventoryRead,
            VehiclePermissions.InventoryManage,
            VehiclePermissions.ItemsRead,
            VehiclePermissions.AccessoriesRead,
            VehiclePermissions.PricingRead,
            VehiclePermissions.PriceBreakupRead,
            VehiclePermissions.NotificationsRead
        },

        [VehicleRoles.ServiceAccount] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            VehiclePermissions.OrdersRead,
            VehiclePermissions.OrdersManage,
            VehiclePermissions.DealersRead,
            VehiclePermissions.InventoryRead,
            VehiclePermissions.InventoryManage,
            VehiclePermissions.ItemsRead,
            VehiclePermissions.ItemsManage,
            VehiclePermissions.PricingRead,
            VehiclePermissions.PricingManage,
            VehiclePermissions.PriceBreakupRead,
            VehiclePermissions.PriceBreakupManage,
            VehiclePermissions.AccessoriesRead,
            VehiclePermissions.AccessoriesManage,
            VehiclePermissions.PaymentsRead,
            VehiclePermissions.PaymentsProcess,
            VehiclePermissions.PaymentsRefund,
            VehiclePermissions.NotificationsSend
        },

        [VehicleRoles.Admin] = new HashSet<string>(VehiclePermissions.All, StringComparer.OrdinalIgnoreCase),
        [VehicleRoles.SuperAdmin] = new HashSet<string>(VehiclePermissions.All, StringComparer.OrdinalIgnoreCase)
    };

    public static IReadOnlySet<string> GetPermissionsForRole(string role)
    {
        if (RolePermissions.TryGetValue(role, out var permissions))
        {
            return permissions;
        }
        return new HashSet<string>();
    }

    public static IReadOnlySet<string> GetPermissionsForRoles(IEnumerable<string> roles)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var role in roles)
        {
            if (RolePermissions.TryGetValue(role, out var perms))
            {
                foreach (var p in perms)
                {
                    result.Add(p);
                }
            }
        }
        return result;
    }
}

