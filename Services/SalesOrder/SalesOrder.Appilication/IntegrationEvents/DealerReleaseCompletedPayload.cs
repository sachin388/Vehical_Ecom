using System;

namespace SalesOrder.Application.IntegrationEvents
{
    public sealed record DealerReleaseCompletedPayload(
        Guid SagaId,
        Guid SalesOrderId,
        string ReleaseReference);
}
