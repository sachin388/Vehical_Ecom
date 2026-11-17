using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Payment.Application.DTOs;

namespace Payment.Application.Interfaces
{
    public interface IPaymentApplicationService
    {
        Task<InitiatePaymentResponse> InitiatePaymentAsync(InitiatePaymentRequest request, CancellationToken ct = default);
        Task<PaymentResponse> ProcessCallbackAsync(PayUCallbackPayload payload, CancellationToken ct = default);
        Task<PaymentResponse?> GetByIdAsync(long id, CancellationToken ct = default);
        Task<PaymentResponse?> GetByPaymentReferenceAsync(string paymentReference, CancellationToken ct = default);
        Task<IReadOnlyList<PaymentResponse>> GetBySalesOrderIdAsync(Guid salesOrderId, CancellationToken ct = default);
        Task<PaymentResponse> VerifyPaymentStatusAsync(string paymentReference, CancellationToken ct = default);
        Task<RefundPaymentResponse> RefundPaymentAsync(RefundPaymentRequest request, CancellationToken ct = default);
    }
}
