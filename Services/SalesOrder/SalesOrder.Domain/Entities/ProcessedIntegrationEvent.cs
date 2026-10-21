using System;

namespace SalesOrder.Domain.Entities
{
    public sealed class ProcessedIntegrationEvent
    {
        public long Id { get; set; }
        public string ConsumerName { get; set; } = string.Empty;
        public Guid EventId { get; set; }
        public DateTime ProcessedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
