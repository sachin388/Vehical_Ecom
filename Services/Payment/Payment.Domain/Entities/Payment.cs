using System;
using System.Collections.Generic;

namespace Payment.Domain.Entities
{
    public sealed class Payment
    {
        public long Id { get; set; }
        public string PaymentReference { get; set; } = string.Empty;
        public Guid SalesOrderId { get; set; }
        public string CustomerId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "INR";
        public string Status { get; set; } = "Initiated"; // Captured, Authorized, Initiated, Refunded, Failed, Tampered
        public string PaymentMethod { get; set; } = "PayU";
        public string IdempotencyKey { get; set; } = string.Empty;
        public string? GatewayTransactionId { get; set; } // PayU mihpayid
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }

        public List<PaymentTransaction> Transactions { get; set; } = new();
        public List<PaymentRefund> Refunds { get; set; } = new();

        public void MarkSuccess(string gatewayTransactionId, string? paymentMode, string? bankReference)
        {
            Status = "Captured";
            GatewayTransactionId = gatewayTransactionId;
            if (!string.IsNullOrWhiteSpace(paymentMode))
                PaymentMethod = paymentMode;
            UpdatedAtUtc = DateTime.UtcNow;

            Transactions.Add(new PaymentTransaction
            {
                PaymentId = Id,
                TransactionType = "Capture",
                Amount = Amount,
                Currency = Currency,
                Status = "Success",
                GatewayResponseCode = paymentMode,
                GatewayMessage = bankReference,
                ProcessedAtUtc = DateTime.UtcNow
            });
        }

        public void MarkFailed(string? reason)
        {
            Status = "Failed";
            UpdatedAtUtc = DateTime.UtcNow;

            Transactions.Add(new PaymentTransaction
            {
                PaymentId = Id,
                TransactionType = "Capture",
                Amount = Amount,
                Currency = Currency,
                Status = "Failed",
                GatewayResponseCode = "FAILED",
                GatewayMessage = reason,
                ProcessedAtUtc = DateTime.UtcNow
            });
        }

        public void MarkTampered(string? details)
        {
            Status = "Tampered";
            UpdatedAtUtc = DateTime.UtcNow;

            Transactions.Add(new PaymentTransaction
            {
                PaymentId = Id,
                TransactionType = "Capture",
                Amount = Amount,
                Currency = Currency,
                Status = "Tampered",
                GatewayResponseCode = "TAMPERED",
                GatewayMessage = details ?? "PayU callback hash mismatch.",
                ProcessedAtUtc = DateTime.UtcNow
            });
        }

        public void MarkRefunded(decimal refundAmount)
        {
            Status = "Refunded";
            UpdatedAtUtc = DateTime.UtcNow;

            Transactions.Add(new PaymentTransaction
            {
                PaymentId = Id,
                TransactionType = "Refund",
                Amount = refundAmount,
                Currency = Currency,
                Status = "Success",
                GatewayResponseCode = "REFUND_SUCCESS",
                GatewayMessage = "Refund processed successfully.",
                ProcessedAtUtc = DateTime.UtcNow
            });
        }
    }
}

