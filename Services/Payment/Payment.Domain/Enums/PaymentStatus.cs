namespace Payment.Domain.Enums
{
    public enum PaymentStatus
    {
        Initiated = 1,
        Pending = 2,
        Success = 3,
        Failed = 4,
        Tampered = 5,
        Refunded = 6,
        PartiallyRefunded = 7
    }
}

