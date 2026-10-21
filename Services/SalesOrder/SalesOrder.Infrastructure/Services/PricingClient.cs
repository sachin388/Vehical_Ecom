using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SalesOrder.Application.DTOs;
using SalesOrder.Application.Interfaces;

namespace SalesOrder.Infrastructure.Services
{
    public sealed class PricingClient : IPricingClient
    {
        public Task<IReadOnlyList<PricedItemSnapshot>> GetPriceSnapshotsAsync(
            IReadOnlyList<CreateSalesOrderLineRequest> lines, string currency, CancellationToken ct)
        {
            var results = lines.Select(l => new PricedItemSnapshot(
                l.ItemId,
                $"ITEM-{l.ItemId}",
                $"Vehicle Item {l.ItemId}",
                100000m,
                0m,
                0.18m,
                currency,
                l.Accessories.Select(a => new PricedAccessorySnapshot(
                    a.AccessoryId,
                    $"Accessory {a.AccessoryId}",
                    5000m,
                    currency
                )).ToList()
            )).ToList();

            return Task.FromResult<IReadOnlyList<PricedItemSnapshot>>(results);
        }
    }
}
