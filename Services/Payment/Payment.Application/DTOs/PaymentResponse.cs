using System;
using System.Collections.Generic;

namespace Payment.Application.DTOs
{
    public sealed class PaymentTransactionDto
    {
        public long Id { get; set; }
        public string TransactionType { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? GatewayResponseCode { get; set; }
        public string? GatewayMessage { get; set; }
        public DateTime ProcessedAtUtc { get; set; }
    }

    public sealed class PaymentRefundDto
    {
        public long Id { get; set; }
        public string RefundReference { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
    }

    public sealed class PaymentResponse
    {
        public long Id { get; set; }
        public string PaymentReference { get; set; } = string.Empty;
        public Guid SalesOrderId { get; set; }
        public string CustomerId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "INR";
        public string Status { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string? GatewayTransactionId { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        public List<PaymentTransactionDto> Transactions { get; set; } = new();
        public List<PaymentRefundDto> Refunds { get; set; } = new();
    }
}
