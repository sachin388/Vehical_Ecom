using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesOrder.Domain.Entities
{
    public sealed class SalesOrderHistory
    {
        public long Id { get; set; }
        public Guid SalesOrderPublicId { get; set; }
        public string? FromStatus { get; set; }
        public string ToStatus { get; set; } = string.Empty;
        public string ActorId { get; set; } = string.Empty;
        public string ActorType { get; set; } = "User";
        public string Reason { get; set; } = string.Empty;
        public DateTime ChangedAtUtc { get; set; } = DateTime.UtcNow;
        public string CorrelationId { get; set; } = string.Empty;
    }

}
