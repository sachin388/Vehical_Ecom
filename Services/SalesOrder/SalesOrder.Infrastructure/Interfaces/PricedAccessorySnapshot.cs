using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesOrder.Infrastructure.Interfaces
{
    public sealed record PricedAccessorySnapshot(
    int AccessoryId, string Name, decimal UnitPrice, string Currency);

}
