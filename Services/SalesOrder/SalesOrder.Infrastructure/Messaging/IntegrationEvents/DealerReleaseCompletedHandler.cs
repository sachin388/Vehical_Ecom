using SalesOrder.Infrastructure.IntegrationEvents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SalesOrder.Application.IntegrationEvents;
using SalesOrder.Infrastructure.Persistence;
namespace SalesOrder.Infrastructure.Messaging.IntegrationEvents
{
    public sealed class DealerReleaseCompletedHandler
    : CancellationSagaCompletionHandler
    {
        public DealerReleaseCompletedHandler(
            SalesOrderDbContext db)
            : base(db)
        {
        }

        public Task HandleAsync(
            Guid eventId,
            DealerReleaseCompletedPayload message,
            CancellationToken ct)
            => CompleteStepAsync(
                eventId,
                message.SagaId,
                message.SalesOrderId,
                paymentStep: false,
                message.ReleaseReference,
                ct);
    }
}
