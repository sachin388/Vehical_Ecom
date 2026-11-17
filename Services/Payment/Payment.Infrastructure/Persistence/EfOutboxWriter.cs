using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Payment.Application.Interfaces;
using Payment.Domain.Entities;

namespace Payment.Infrastructure.Persistence
{
    public sealed class EfOutboxWriter : IOutboxWriter
    {
        private readonly PaymentDbContext _db;

        public EfOutboxWriter(PaymentDbContext db)
        {
            _db = db;
        }

        public async Task AddAsync(string eventType, string aggregateId, object payload, CancellationToken ct = default)
        {
            var outboxMessage = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                EventId = Guid.NewGuid(),
                AggregateId = aggregateId,
                EventType = eventType,
                Topic = "payment.events",
                PayloadJson = JsonSerializer.Serialize(payload),
                CreatedAtUtc = DateTime.UtcNow
            };

            await _db.OutboxMessages.AddAsync(outboxMessage, ct);
        }
    }
}

