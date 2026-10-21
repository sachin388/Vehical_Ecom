using System.Collections.Generic;

namespace SalesOrder.Application.Interfaces
{
    public sealed record PricedItemSnapshot(
        int ItemId, string ItemCode, string ItemName, decimal UnitPrice,
        decimal Discount, decimal TaxRate, string Currency,
        IReadOnlyList<PricedAccessorySnapshot> Accessories);
}
