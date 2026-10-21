using System;

namespace SalesOrder.Application.IntegrationEvents
{
    public sealed record PaymentCompensationCompletedPayload(
        Guid SagaId,
        Guid SalesOrderId,
        string ProviderReference);
}
