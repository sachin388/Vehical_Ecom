using Dealer.Application.DTOs;
using Dealer.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Dealer.Application.Services
{
    public class DealerInventoryApplicationService : IDealerInventoryApplicationService
    {
        public Task<DealerInventoryResponse> SetInventoryAsync(long dealerId, SetDealerInventoryRequest request, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<DealerInventoryResponse>> GetInventoryAsync(long dealerId, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }
    }
}
