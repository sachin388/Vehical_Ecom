using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesOrder.Infrastructure.Interfaces
{
    public interface IPricingClient
    {
        Task<IReadOnlyList<PricedItemSnapshot>> GetPriceSnapshotsAsync(
            IReadOnlyList<CreateSalesOrderLineRequest> lines, string currency, CancellationToken ct);
    }

}
