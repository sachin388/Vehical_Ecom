using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Accessory.Domain.Events
{
    public class AccessoryCompatibilityChangedEvent
    {
        public Guid EventId { get; set; } = Guid.NewGuid();

        public long AccessoryId { get; set; }

        public int ItemId { get; set; }

        public string SKU { get; set; } = string.Empty;

        public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    }

}
