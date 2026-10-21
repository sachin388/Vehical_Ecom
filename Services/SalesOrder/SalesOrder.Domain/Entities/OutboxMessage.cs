using System;

namespace SalesOrder.Domain.Entities
{
    public sealed class OutboxMessage
    {
        public long Id { get; set; }
        public Guid EventId { get; set; } = Guid.NewGuid();
        public string AggregateId { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public string Type { get => EventType; set => EventType = value; }
        public string Topic { get; set; } = string.Empty;
        public string PayloadJson { get; set; } = string.Empty;
        public string Payload { get => PayloadJson; set => PayloadJson = value; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime OccurredAtUtc { get => CreatedAtUtc; set => CreatedAtUtc = value; }
        public DateTime? PublishedAtUtc { get; set; }
        public int RetryCount { get; set; } = 0;
        public string Status { get; set; } = "Pending";
        public string? Error { get; set; }
    }
}
