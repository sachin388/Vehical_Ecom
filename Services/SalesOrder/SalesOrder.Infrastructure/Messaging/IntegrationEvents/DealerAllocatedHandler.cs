using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SalesOrder.Application.IntegrationEvents;
using SalesOrder.Domain.Entities;
using SalesOrder.Domain.Enums;
using SalesOrder.Infrastructure.Persistence;

namespace SalesOrder.Infrastructure.Messaging.IntegrationEvents
{
    using SalesOrder = SalesOrder.Domain.Entities.SalesOrder;

    public sealed class DealerAllocatedHandler
    {
        private const string Consumer = "SalesOrderService";
        private readonly SalesOrderDbContext _db;

        public DealerAllocatedHandler(SalesOrderDbContext db) => _db = db;

        public async Task HandleAsync(
            Guid eventId, DealerAllocatedPayload message, CancellationToken ct)
        {
            if (message.DealerId <= 0 ||
                string.IsNullOrWhiteSpace(message.AllocationReference))
                throw new ArgumentException("Dealer allocation details are invalid.");

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

            if (order.Status == SalesOrderStatus.Confirmed)
            {
                var previous = order.Status;
                order.DealerId = message.DealerId;
                order.ChangeStatus(SalesOrderStatus.Allocated);
                _db.SalesOrderHistory.Add(new SalesOrderHistory
                {
                    SalesOrderPublicId = order.PublicId,
                    FromStatus = previous.ToString(),
                    ToStatus = order.Status.ToString(),
                    ActorId = "dealer-service",
                    ActorType = "Service",
                    Reason = $"Allocated to dealer {message.DealerId} (ref {message.AllocationReference})",
                    ChangedAtUtc = DateTime.UtcNow,
                    CorrelationId = message.SalesOrderId.ToString()
                });
                _db.OutboxMessages.Add(new OutboxMessage
                {
                    EventId = Guid.NewGuid(),
                    AggregateId = order.PublicId.ToString(),
                    EventType = "SalesOrderAllocated",
                    Topic = "salesorder.events",
                    PayloadJson = JsonSerializer.Serialize(new
                    {
                        order.PublicId,
                        order.OrderNumber,
                        order.DealerId,
                        message.AllocationReference
                    }),
                    CreatedAtUtc = DateTime.UtcNow
                });
            }
            else if (order.Status != SalesOrderStatus.Allocated)
            {
                throw new InvalidOperationException(
                    $"DealerAllocated cannot be applied when order status is {order.Status}.");
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
