using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SalesOrder.Infrastructure.Persistence;
using SalesOrder.Infrastructure.Messaging;
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


