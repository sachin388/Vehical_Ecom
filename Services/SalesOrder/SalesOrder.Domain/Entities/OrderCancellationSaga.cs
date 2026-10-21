using System;
using SalesOrder.Domain.Enums;

namespace SalesOrder.Domain.Entities
{
    public sealed class OrderCancellationSaga
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid SalesOrderPublicId { get; set; }
        public CancellationSagaStatus Status { get; set; } = CancellationSagaStatus.Initiated;
        public bool PaymentCompensationRequired { get; set; }
        public bool PaymentCompensationCompleted { get; set; }
        public bool DealerReleaseRequired { get; set; }
        public bool DealerReleaseCompleted { get; set; }
        public string RequestedBy { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string? LastError { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }
    }
}
