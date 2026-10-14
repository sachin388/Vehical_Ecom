using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesOrder.Application.IntegrationEvents
{
    public sealed class PaymentSucceededHandler
    {
        private const string Consumer = "SalesOrderService";
        private readonly SalesOrderDbContext _db;

        public PaymentSucceededHandler(SalesOrderDbContext db) => _db = db;

        public async Task HandleAsync(
            Guid eventId, PaymentSucceededPayload message, CancellationToken ct)
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            if (await _db.ProcessedIntegrationEvents.AnyAsync(
                x => x.ConsumerName == Consumer && x.EventId == eventId, ct))
            {
                await tx.CommitAsync(ct);
                return;
            }

            var order = await _db.SalesOrders.SingleOrDefaultAsync(
                x => x.PublicId == message.SalesOrderId, ct)
                ?? throw new InvalidOperationException("Order was not found.");

            if (message.Amount != order.GrandTotalSnapshot ||
                !string.Equals(message.Currency, order.Currency,
                    StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    "Payment amount/currency does not match the order snapshot.");

            if (order.Status == SalesOrderStatus.PendingPayment)
            {
                var previous = order.Status;
                order.ChangeStatus(SalesOrderStatus.Confirmed);
                _db.SalesOrderHistory.Add(new SalesOrderHistory
                {
                    SalesOrderPublicId = order.PublicId,
                    FromStatus = previous.ToString(),
                    ToStatus = order.Status.ToString(),
                    ActorId = "payment-service",
                    ActorType = "Service",
                    Reason = "Verified PaymentSucceeded event",
                    ChangedAtUtc = DateTime.UtcNow,
                    CorrelationId = message.SalesOrderId.ToString()
                });
                _db.OutboxMessages.Add(new OutboxMessage
                {
                    EventId = Guid.NewGuid(),
                    AggregateId = order.PublicId.ToString(),
                    EventType = "SalesOrderConfirmed",
                    Topic = "salesorder.events",
                    PayloadJson = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        order.PublicId,
                        order.OrderNumber,
                        order.CustomerId,
                        order.GrandTotalSnapshot,
                        order.Currency
                    }),
                    CreatedAtUtc = DateTime.UtcNow
                });
            }
            else if (order.Status != SalesOrderStatus.Confirmed)
            {
                throw new InvalidOperationException(
                    $"PaymentSucceeded cannot be applied when order status is {order.Status}.");
            }

            _db.ProcessedIntegrationEvents.Add(new ProcessedIntegrationEvent
            {
                ConsumerName = Consumer,
                EventId = eventId,
                ProcessedAtUtc = DateTime.UtcNow
            });

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
    }

}
