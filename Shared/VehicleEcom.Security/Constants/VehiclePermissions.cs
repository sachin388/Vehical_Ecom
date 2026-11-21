namespace VehicleEcom.Security.Constants;

public static class VehiclePermissions
{
    // Orders
    public const string OrdersRead = "orders.read";
    public const string OrdersCreate = "orders.create";
    public const string OrdersCancel = "orders.cancel";
    public const string OrdersManage = "orders.manage";

    // Dealers & Inventory
    public const string DealersRead = "dealers.read";
    public const string DealersManage = "dealers.manage";
    public const string InventoryRead = "inventory.read";
    public const string InventoryManage = "inventory.manage";

    // Catalog & Items
    public const string ItemsRead = "items.read";
    public const string ItemsManage = "items.manage";

    // Pricing & Price Breakup
    public const string PricingRead = "pricing.read";
    public const string PricingManage = "pricing.manage";
    public const string PriceBreakupRead = "pricebreakup.read";
    public const string PriceBreakupManage = "pricebreakup.manage";

    // Accessories
    public const string AccessoriesRead = "accessories.read";
    public const string AccessoriesManage = "accessories.manage";

    // Payments
    public const string PaymentsRead = "payments.read";
    public const string PaymentsProcess = "payments.process";
    public const string PaymentsRefund = "payments.refund";

    // Notifications
    public const string NotificationsRead = "notifications.read";
    public const string NotificationsSend = "notifications.send";

    // Administration & Security
    public const string AdminAccess = "admin.access";
    public const string UsersManage = "users.manage";
    public const string AuditRead = "audit.read";

    public static readonly IReadOnlyList<string> All = new[]
    {
        OrdersRead, OrdersCreate, OrdersCancel, OrdersManage,
        DealersRead, DealersManage, InventoryRead, InventoryManage,
        ItemsRead, ItemsManage,
        PricingRead, PricingManage, PriceBreakupRead, PriceBreakupManage,
        AccessoriesRead, AccessoriesManage,
        PaymentsRead, PaymentsProcess, PaymentsRefund,
        NotificationsRead, NotificationsSend,
        AdminAccess, UsersManage, AuditRead
    };
}

