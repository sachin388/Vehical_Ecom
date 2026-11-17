using System;

namespace Payment.Application.DTOs
{
    public sealed class InitiatePaymentRequest
    {
        public Guid SalesOrderId { get; set; }
        public string? CustomerId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "INR";
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string ProductInfo { get; set; } = string.Empty;
        public string? SuccessUrl { get; set; }
        public string? FailureUrl { get; set; }
        public string? Udf1 { get; set; }
        public string? Udf2 { get; set; }
        public string? Udf3 { get; set; }
        public string? Udf4 { get; set; }
        public string? Udf5 { get; set; }
    }
}
