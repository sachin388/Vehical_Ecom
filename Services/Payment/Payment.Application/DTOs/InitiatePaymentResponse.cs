using System;
using System.Collections.Generic;

namespace Payment.Application.DTOs
{
    public sealed class InitiatePaymentResponse
    {
        public long PaymentId { get; set; }
        public Guid SalesOrderId { get; set; }
        public string PaymentReference { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
        public string PaymentUrl { get; set; } = string.Empty;
        public Dictionary<string, string> FormParameters { get; set; } = new();
        public string Status { get; set; } = string.Empty;
    }
}
