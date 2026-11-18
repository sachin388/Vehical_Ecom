using System;

namespace Payment.Application.DTOs
{
    public sealed class RefundPaymentRequest
    {
        public long? PaymentId { get; set; }
        public Guid? SalesOrderId { get; set; }
        public decimal Amount { get; set; }
        public string Reason { get; set; } = string.Empty;
        public Guid? SagaId { get; set; }
        public string? IdempotencyKey { get; set; }
    }
}
