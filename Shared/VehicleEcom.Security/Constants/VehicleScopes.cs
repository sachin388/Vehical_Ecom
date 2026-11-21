namespace VehicleEcom.Security.Constants;

public static class VehicleScopes
{
    // Standard OIDC scopes
    public const string OpenId = "openid";
    public const string Profile = "profile";
    public const string Email = "email";
    public const string OfflineAccess = "offline_access";
    public const string VehicleApi = "vehicle_api";

    // Service-to-Service M2M Scopes
    public const string SalesOrderInternal = "salesorder.internal";
    public const string ItemsPriceInternal = "itemsprice.internal";
    public const string PaymentInternal = "payment.internal";
    public const string DealerInternal = "dealer.internal";
    public const string InternalAll = "internal.all";

    public static readonly IReadOnlyList<string> All = new[]
    {
        OpenId, Profile, Email, OfflineAccess, VehicleApi,
        SalesOrderInternal, ItemsPriceInternal, PaymentInternal, DealerInternal, InternalAll
    };
}

