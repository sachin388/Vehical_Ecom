namespace VehicleEcom.Security.Constants;

public static class VehicleRoles
{
    public const string Admin = "Admin";
    public const string SuperAdmin = "SuperAdmin";
    public const string Dealer = "Dealer";
    public const string DealerPrincipal = "DealerPrincipal";
    public const string DealershipSalesManager = "DealershipSalesManager";
    public const string Customer = "Customer";
    public const string ServiceAccount = "ServiceAccount";

    public static readonly IReadOnlyList<string> All = new[]
    {
        Admin,
        SuperAdmin,
        Dealer,
        DealerPrincipal,
        DealershipSalesManager,
        Customer,
        ServiceAccount
    };
}

