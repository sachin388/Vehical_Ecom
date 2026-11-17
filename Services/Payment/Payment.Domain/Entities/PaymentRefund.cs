using System;

namespace Payment.Domain.Entities
{
    public sealed class PaymentRefund
    {
        public long Id { get; set; }
        public long PaymentId { get; set; }
        public Guid SalesOrderPublicId { get; set; }
        public string RefundReference { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Status { get; set; } = "Requested"; // Requested, Completed, Rejected
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAtUtc { get; set; }

        public void MarkSuccess(string? gatewayRefundId = null)
        {
            Status = "Completed";
            CompletedAtUtc = DateTime.UtcNow;
        }

        public void MarkFailed(string error)
        {
            Status = "Rejected";
            CompletedAtUtc = DateTime.UtcNow;
        }
    }
}
