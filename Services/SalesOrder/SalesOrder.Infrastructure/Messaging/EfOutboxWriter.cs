using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SalesOrder.Application.Interfaces;
using SalesOrder.Domain.Entities;
using SalesOrder.Infrastructure.Persistence;

namespace SalesOrder.Infrastructure.Messaging
{
    public sealed class EfOutboxWriter : IOutboxWriter
    {
        private readonly SalesOrderDbContext _db;

        public EfOutboxWriter(SalesOrderDbContext db) => _db = db;

        public Task AddAsync(
            string eventType, string aggregateId, object payload, CancellationToken ct)
        {
            var topic = eventType switch
            {
                "SalesOrderCreated" or "SalesOrderConfirmed" or
                "SalesOrderCancelled" or "SalesOrderStatusChanged"
                    => "salesorder.events",
                "OrderCancellationRequested" => "order.cancellation.commands",
                _ => throw new InvalidOperationException(
                    $"No topic mapping configured for event '{eventType}'.")
            };

            var message = new OutboxMessage
            {
                EventId = Guid.NewGuid(),
                AggregateId = aggregateId,
                EventType = eventType,
                Topic = topic,
                PayloadJson = JsonSerializer.Serialize(payload),
                CreatedAtUtc = DateTime.UtcNow
            };

            _db.OutboxMessages.Add(message);
            return Task.CompletedTask;
        }
    }

}
