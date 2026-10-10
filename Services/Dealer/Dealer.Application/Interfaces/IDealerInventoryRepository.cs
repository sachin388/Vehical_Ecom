using Dealer.Application.DTOs;
using Dealer.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dealer.Application.Interfaces
{
    public interface IDealerInventoryRepository
    {
        Task<List<DealerAvailabilityCandidate>> GetCandidatesAsync(
            int itemId, string sku, int quantity, string? city, string? stateCode,
            CancellationToken ct = default);
        Task<DealerOrderAssignment?> GetAssignmentByOrderIdAsync(
            Guid salesOrderId, CancellationToken ct = default);
        Task<bool> TryReserveAsync(long dealerId, int itemId, int quantity,
            CancellationToken ct = default);
        Task AddAssignmentAsync(DealerOrderAssignment assignment, CancellationToken ct = default);
        Task SaveChangesAsync(CancellationToken ct = default);
    }

}
