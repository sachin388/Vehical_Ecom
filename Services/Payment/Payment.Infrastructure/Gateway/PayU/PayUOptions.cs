namespace Payment.Infrastructure.Gateway.PayU
{
    public sealed class PayUOptions
    {
        public const string SectionName = "PayU";

        public string MerchantKey { get; set; } = "payu_test_key";
        public string MerchantSalt { get; set; } = "payu_test_salt";
        public string BaseUrl { get; set; } = "https://test.payu.in";
        public string PaymentUrl { get; set; } = "https://test.payu.in/_payment";
        public string VerifyPaymentUrl { get; set; } = "https://test.payu.in/merchant/postservice?form=2";
        public string RefundUrl { get; set; } = "https://test.payu.in/merchant/postservice?form=2";
        public string DefaultSuccessUrl { get; set; } = "https://localhost:7001/api/payments/callback";
        public string DefaultFailureUrl { get; set; } = "https://localhost:7001/api/payments/callback";
    }
}

