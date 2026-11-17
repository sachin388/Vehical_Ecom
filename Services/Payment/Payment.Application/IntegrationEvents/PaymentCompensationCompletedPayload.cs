using System;

namespace Payment.Application.IntegrationEvents
{
    public sealed record PaymentCompensationCompletedPayload(
        Guid SagaId,
        Guid SalesOrderId,
        string ProviderReference);
}

