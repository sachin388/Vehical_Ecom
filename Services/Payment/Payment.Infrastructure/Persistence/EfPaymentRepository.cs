using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Payment.Application.Interfaces;
using Payment.Domain.Entities;

namespace Payment.Infrastructure.Persistence
{
    using PaymentEntity = Payment.Domain.Entities.Payment;

    public sealed class EfPaymentRepository : IPaymentRepository
    {
        private readonly PaymentDbContext _db;

        public EfPaymentRepository(PaymentDbContext db)
        {
            _db = db;
        }

        public async Task<PaymentEntity?> GetByIdAsync(long id, CancellationToken ct = default)
        {
            return await _db.Payments
                .Include(x => x.Transactions)
                .Include(x => x.Refunds)
                .SingleOrDefaultAsync(x => x.Id == id, ct);
        }

        public async Task<PaymentEntity?> GetByPaymentReferenceAsync(string paymentReference, CancellationToken ct = default)
        {
            return await _db.Payments
                .Include(x => x.Transactions)
                .Include(x => x.Refunds)
                .SingleOrDefaultAsync(x => x.PaymentReference == paymentReference, ct);
        }

        public async Task<PaymentEntity?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken ct = default)
        {
            return await _db.Payments
                .Include(x => x.Transactions)
                .Include(x => x.Refunds)
                .SingleOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, ct);
        }

        public async Task<IReadOnlyList<PaymentEntity>> GetBySalesOrderIdAsync(Guid salesOrderId, CancellationToken ct = default)
        {
            return await _db.Payments
                .Include(x => x.Transactions)
                .Include(x => x.Refunds)
                .Where(x => x.SalesOrderId == salesOrderId)
                .OrderByDescending(x => x.CreatedAtUtc)
                .ToListAsync(ct);
        }

        public async Task<PaymentRefund?> GetRefundByReferenceAsync(string refundReference, CancellationToken ct = default)
        {
            return await _db.PaymentRefunds
                .SingleOrDefaultAsync(x => x.RefundReference == refundReference, ct);
        }

        public async Task<PaymentRefund?> GetRefundBySagaIdAsync(Guid sagaId, CancellationToken ct = default)
        {
            var sagaStr = sagaId.ToString();
            var sagaPrefix = sagaStr[..8];

            return await _db.PaymentRefunds
                .Where(x => x.Reason.Contains(sagaStr) || x.RefundReference.Contains(sagaPrefix))
                .OrderByDescending(x => x.CreatedAtUtc)
                .FirstOrDefaultAsync(ct);
        }

        public async Task AddAsync(PaymentEntity payment, CancellationToken ct = default)
        {
            await _db.Payments.AddAsync(payment, ct);
        }

        public async Task AddRefundAsync(PaymentRefund refund, CancellationToken ct = default)
        {
            await _db.PaymentRefunds.AddAsync(refund, ct);
        }

        public Task SaveChangesAsync(CancellationToken ct = default)
        {
            return _db.SaveChangesAsync(ct);
        }
    }
}
