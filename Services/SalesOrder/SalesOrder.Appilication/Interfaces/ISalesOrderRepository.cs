using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SalesOrder.Domain.Entities;

namespace SalesOrder.Application.Interfaces
{
    using SalesOrder = SalesOrder.Domain.Entities.SalesOrder;

    public interface ISalesOrderRepository
    {
        Task<SalesOrder?> GetByPublicIdAsync(Guid publicId, CancellationToken ct);
        Task AddAsync(SalesOrder order, CancellationToken ct);
        Task SaveChangesAsync(CancellationToken ct);
        Task<IReadOnlyList<SalesOrderHistory>> GetHistoryAsync(Guid publicId, CancellationToken ct);
    }
}
