using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Payment.Domain.Entities;

namespace Payment.Application.Interfaces
{
    using PaymentEntity = Payment.Domain.Entities.Payment;

    public interface IPaymentRepository
    {
        Task<PaymentEntity?> GetByIdAsync(long id, CancellationToken ct = default);
        Task<PaymentEntity?> GetByPaymentReferenceAsync(string paymentReference, CancellationToken ct = default);
        Task<IReadOnlyList<PaymentEntity>> GetBySalesOrderIdAsync(Guid salesOrderId, CancellationToken ct = default);
        Task AddAsync(PaymentEntity payment, CancellationToken ct = default);
        Task AddRefundAsync(PaymentRefund refund, CancellationToken ct = default);
        Task SaveChangesAsync(CancellationToken ct = default);
    }
}
