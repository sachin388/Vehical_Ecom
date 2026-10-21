using System;

namespace SalesOrder.Application.IntegrationEvents
{
    public sealed record PaymentSucceededPayload(
        Guid SalesOrderId,
        decimal Amount,
        string Currency);
}
