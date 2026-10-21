using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SalesOrder.Application.DTOs;

namespace SalesOrder.Application.Interfaces
{
    public interface IPricingClient
    {
        Task<IReadOnlyList<PricedItemSnapshot>> GetPriceSnapshotsAsync(
            IReadOnlyList<CreateSalesOrderLineRequest> lines, string currency, CancellationToken ct);
    }
}
