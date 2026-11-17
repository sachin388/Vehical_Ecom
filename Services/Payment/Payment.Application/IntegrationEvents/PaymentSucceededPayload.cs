using System;

namespace Payment.Application.IntegrationEvents
{
    public sealed record PaymentSucceededPayload(
        Guid SalesOrderId,
        decimal Amount,
        string Currency);
}

