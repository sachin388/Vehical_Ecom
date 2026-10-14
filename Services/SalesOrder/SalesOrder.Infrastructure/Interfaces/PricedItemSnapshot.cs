using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesOrder.Infrastructure.Interfaces
{
    public sealed record PricedItemSnapshot(
       int ItemId, string ItemCode, string ItemName, decimal UnitPrice,
       decimal Discount, decimal TaxRate, string Currency,
       IReadOnlyList<PricedAccessorySnapshot> Accessories);

}
