using Dealer.Application.DTOs;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Dealer.Application.Interfaces
{
    public interface IDealerInventoryApplicationService
    {
        Task<DealerInventoryResponse> SetInventoryAsync(long dealerId, SetDealerInventoryRequest request, CancellationToken ct = default);
        Task<IReadOnlyList<DealerInventoryResponse>> GetInventoryAsync(long dealerId, CancellationToken ct = default);
    }
}
