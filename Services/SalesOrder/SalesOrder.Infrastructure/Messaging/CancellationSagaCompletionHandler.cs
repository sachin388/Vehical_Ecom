using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SalesOrder.Domain.Entities;
using SalesOrder.Domain.Enums;
using SalesOrder.Infrastructure.Persistence;

namespace SalesOrder.Infrastructure.Messaging.IntegrationEvents
{
    using SalesOrder = SalesOrder.Domain.Entities.SalesOrder;

    public abstract class CancellationSagaCompletionHandler
    {
        protected const string ConsumerName = "SalesOrderService";
        protected readonly SalesOrderDbContext Db;

        protected CancellationSagaCompletionHandler(SalesOrderDbContext db) => Db = db;

        protected async Task CompleteStepAsync(
            Guid eventId,
            Guid sagaId,
            Guid salesOrderId,
            bool paymentStep,
            string completionReference,
            CancellationToken ct)
        {
            await using var tx = await Db.Database.BeginTransactionAsync(ct);

            if (await Db.ProcessedIntegrationEvents.AnyAsync(
                x => x.ConsumerName == ConsumerName && x.EventId == eventId, ct))
            {
                await tx.CommitAsync(ct);
                return;
            }

            var saga = await Db.OrderCancellationSagas
                .SingleOrDefaultAsync(x => x.Id == sagaId, ct)
                ?? throw new InvalidOperationException("Cancellation saga was not found.");

            if (saga.SalesOrderPublicId != salesOrderId)
                throw new InvalidOperationException("Saga/order ID mismatch.");

            if (saga.Status == CancellationSagaStatus.Failed)
                throw new InvalidOperationException("Cancellation saga has failed.");

            if (paymentStep)
            {
                if (!saga.PaymentCompensationRequired)
                    throw new InvalidOperationException("Payment compensation was not required.");
                saga.PaymentCompensationCompleted = true;
            }
            else
            {
                if (!saga.DealerReleaseRequired)
                    throw new InvalidOperationException("Dealer release was not required.");
                saga.DealerReleaseCompleted = true;
            }

            saga.UpdatedAtUtc = DateTime.UtcNow;
            saga.LastError = null;

            var paymentDone = !saga.PaymentCompensationRequired ||
                              saga.PaymentCompensationCompleted;
            var dealerDone = !saga.DealerReleaseRequired || saga.DealerReleaseCompleted;

            if (paymentDone && dealerDone)
            {
                var order = await Db.SalesOrders.SingleOrDefaultAsync(
                    x => x.PublicId == salesOrderId, ct)
                    ?? throw new InvalidOperationException("Sales order was not found.");

                if (order.Status is SalesOrderStatus.Completed or SalesOrderStatus.Processing)
                    throw new InvalidOperationException(
                        $"Order in {order.Status} cannot be cancelled by this saga.");

                if (order.Status != SalesOrderStatus.Cancelled)
                {
                    var previous = order.Status;
                    order.ChangeStatus(SalesOrderStatus.Cancelled);
                    Db.SalesOrderHistory.Add(new SalesOrderHistory
                    {
                        SalesOrderPublicId = order.PublicId,
                        FromStatus = previous.ToString(),
                        ToStatus = SalesOrderStatus.Cancelled.ToString(),
                        ActorId = saga.RequestedBy,
                        ActorType = "CancellationSaga",
                        Reason = saga.Reason,
                        ChangedAtUtc = DateTime.UtcNow,
                        CorrelationId = saga.Id.ToString()
                    });

                    Db.OutboxMessages.Add(new OutboxMessage
                    {
                        EventId = Guid.NewGuid(),
                        AggregateId = order.PublicId.ToString(),
                        EventType = "SalesOrderCancelled",
                        Topic = "salesorder.events",
                        PayloadJson = JsonSerializer.Serialize(new
                        {
                            order.PublicId,
                            order.OrderNumber,
                            PreviousStatus = previous.ToString(),
                            NewStatus = SalesOrderStatus.Cancelled.ToString(),
                            SagaId = saga.Id,
                            Reason = saga.Reason,
                            CompletionReference = completionReference
                        }),
                        CreatedAtUtc = DateTime.UtcNow
                    });
                }

                saga.Status = CancellationSagaStatus.Completed;
            }
            else
            {
                saga.Status = paymentDone
                    ? CancellationSagaStatus.WaitingForDealerRelease
                    : CancellationSagaStatus.WaitingForPaymentCompensation;
            }

            Db.ProcessedIntegrationEvents.Add(new ProcessedIntegrationEvent
            {
                ConsumerName = ConsumerName,
                EventId = eventId,
                ProcessedAtUtc = DateTime.UtcNow
            });

            await Db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
    }
}
