using System;

namespace Payment.Domain.Entities
{
    public sealed class PaymentTransaction
    {
        public long Id { get; set; }
        public long PaymentId { get; set; }
        public string TransactionType { get; set; } = "Capture"; // Authorization, Capture, Refund, Reversal
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "INR";
        public string Status { get; set; } = "Pending"; // Success, Failed, Pending, Tampered
        public string? GatewayResponseCode { get; set; }
        public string? GatewayMessage { get; set; }
        public DateTime ProcessedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
