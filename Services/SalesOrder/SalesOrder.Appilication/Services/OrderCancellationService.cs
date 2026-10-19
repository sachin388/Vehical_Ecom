using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesOrder.Application.Services
{
    

    public sealed class OrderCancellationService
    {
        private readonly SalesOrderDbContext _db;

        public OrderCancellationService(SalesOrderDbContext db)
        {
            _db = db;
        }

        public async Task RequestCancellationAsync(
            Guid orderId,
            string customerId,
            string reason,
            CancellationToken ct)
        {
            await using var tx =
                await _db.Database.BeginTransactionAsync(ct);

            // 1. Load the order and validate customer ownership.
            // 2. Check whether the order can be cancelled.
            // 3. Determine whether payment compensation is required.
            // 4. Determine whether dealer stock release is required.
            // 5. Create and persist the OrderCancellationSaga.
            // 6. Add PaymentCompensationRequested to the Outbox.
            // 7. Add DealerReleaseRequested to the Outbox.
            // 8. If no compensation is required, complete cancellation.
            // 9. Save all changes and commit the transaction.

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
    }
}
