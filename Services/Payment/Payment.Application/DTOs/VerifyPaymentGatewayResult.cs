namespace Payment.Application.DTOs
{
    public sealed class VerifyPaymentGatewayResult
    {
        public bool IsSuccess { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? MihpayId { get; set; }
        public string? BankRefNum { get; set; }
        public string? PaymentMode { get; set; }
        public string? ErrorMessage { get; set; }
        public decimal? Amount { get; set; }
        public string? RawResponse { get; set; }
    }
}

