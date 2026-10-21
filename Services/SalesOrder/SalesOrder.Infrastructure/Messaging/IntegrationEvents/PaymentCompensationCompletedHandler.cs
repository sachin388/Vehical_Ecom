using System;
using System.Threading;
using System.Threading.Tasks;
using SalesOrder.Application.IntegrationEvents;
using SalesOrder.Infrastructure.Persistence;

namespace SalesOrder.Infrastructure.Messaging.IntegrationEvents
{
    public sealed class PaymentCompensationCompletedHandler
        : CancellationSagaCompletionHandler
    {
        public PaymentCompensationCompletedHandler(
            SalesOrderDbContext db)
            : base(db)
        {
        }

        public Task HandleAsync(
            Guid eventId,
            PaymentCompensationCompletedPayload message,
            CancellationToken ct)
            => CompleteStepAsync(
                eventId,
                message.SagaId,
                message.SalesOrderId,
                paymentStep: true,
                message.ProviderReference,
                ct);
    }
}
