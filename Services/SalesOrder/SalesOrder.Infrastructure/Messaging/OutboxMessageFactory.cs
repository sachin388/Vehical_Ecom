using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using SalesOrder.Domain.Entities;

namespace SalesOrder.Infrastructure.Messaging
{
    public static class OutboxMessageFactory
    {
        public static OutboxMessage CreateOutbox<T>(
            string eventType,
            string topic,
            T payload,
            string aggregateId)
        {
            return new OutboxMessage
            {
                EventId = Guid.NewGuid(),
                AggregateId = aggregateId,
                EventType = eventType,
                Topic = topic,
                PayloadJson = JsonSerializer.Serialize(payload),
                CreatedAtUtc = DateTime.UtcNow
            };
        }
    }
}
