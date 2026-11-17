using System;

namespace Payment.Domain.Entities
{
    public sealed class OutboxMessage
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid EventId { get; set; } = Guid.NewGuid();
        public string AggregateId { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public string Topic { get; set; } = "payment.events";
        public string PayloadJson { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedAtUtc { get; set; }
    }
}

