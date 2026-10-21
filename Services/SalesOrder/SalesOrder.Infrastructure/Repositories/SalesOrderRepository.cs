using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SalesOrder.Application.Interfaces;
using SalesOrder.Domain.Entities;
using SalesOrder.Infrastructure.Persistence;

namespace SalesOrder.Infrastructure.Repositories
{
    using SalesOrder = SalesOrder.Domain.Entities.SalesOrder;

    public sealed class SalesOrderRepository : ISalesOrderRepository
    {
        private readonly SalesOrderDbContext _db;

        public SalesOrderRepository(SalesOrderDbContext db)
        {
            _db = db;
        }

        public async Task<SalesOrder?> GetByPublicIdAsync(Guid publicId, CancellationToken ct)
        {
            return await _db.SalesOrders
                .Include(x => x.Lines)
                .SingleOrDefaultAsync(x => x.PublicId == publicId, ct);
        }

        public async Task AddAsync(SalesOrder order, CancellationToken ct)
        {
            await _db.SalesOrders.AddAsync(order, ct);
        }

        public Task SaveChangesAsync(CancellationToken ct)
        {
            return _db.SaveChangesAsync(ct);
        }

        public async Task<IReadOnlyList<SalesOrderHistory>> GetHistoryAsync(Guid publicId, CancellationToken ct)
        {
            return await _db.SalesOrderHistory
                .Where(x => x.SalesOrderPublicId == publicId)
                .OrderByDescending(x => x.ChangedAtUtc)
                .ToListAsync(ct);
        }
    }
}
