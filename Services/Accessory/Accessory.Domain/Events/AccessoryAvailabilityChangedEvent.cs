using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Accessory.Domain.Events
{
    public class AccessoryAvailabilityChangedEvent
    {
        public Guid EventId { get; set; } = Guid.NewGuid();

        public long AccessoryId { get; set; }

        public string Code { get; set; } = string.Empty;

        public int AvailableQuantity { get; set; }

        public string Status { get; set; } = string.Empty;

        public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    }

}
