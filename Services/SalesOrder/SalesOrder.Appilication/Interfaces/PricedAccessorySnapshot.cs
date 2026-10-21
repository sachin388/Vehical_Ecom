using System;

namespace SalesOrder.Application.Interfaces
{
    public sealed record PricedAccessorySnapshot(
        int AccessoryId, string Name, decimal UnitPrice, string Currency);
}
