using System;

namespace Payment.Application.DTOs
{
    public sealed class RefundPaymentResponse
    {
        public long RefundId { get; set; }
        public long PaymentId { get; set; }
        public Guid SalesOrderId { get; set; }
        public string RefundReference { get; set; } = string.Empty;
        public string? GatewayRefundId { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Error { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
    }
}
