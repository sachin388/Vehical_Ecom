namespace Payment.Application.DTOs
{
    public sealed class PayUCallbackPayload
    {
        public string? TxnId { get; set; }
        public string? MihpayId { get; set; }
        public string? Status { get; set; }
        public string? Amount { get; set; }
        public string? Hash { get; set; }
        public string? ProductInfo { get; set; }
        public string? FirstName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Mode { get; set; }
        public string? BankRefNum { get; set; }
        public string? ErrorMessage { get; set; }
        public string? UnmappedStatus { get; set; }
        public string? AdditionalCharges { get; set; }
        public string? Key { get; set; }
        public string? Udf1 { get; set; }
        public string? Udf2 { get; set; }
        public string? Udf3 { get; set; }
        public string? Udf4 { get; set; }
        public string? Udf5 { get; set; }
    }
}

