using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesOrder.Infrastructure.Interfaces
{
    public interface ISalesOrderRepository
    {
        Task<SalesOrder?> GetByPublicIdAsync(Guid publicId, CancellationToken ct);
        Task AddAsync(SalesOrder order, CancellationToken ct);
        Task SaveChangesAsync(CancellationToken ct);
        Task<IReadOnlyList<SalesOrderHistory>> GetHistoryAsync(Guid publicId, CancellationToken ct);
    }

}
