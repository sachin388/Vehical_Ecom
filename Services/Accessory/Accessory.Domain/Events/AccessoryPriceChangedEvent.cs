using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Accessory.Domain.Events
{
    public class AccessoryPriceChangedEvent
    {
        public Guid EventId { get; set; } = Guid.NewGuid();

        public long AccessoryId { get; set; }

        public string Code { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public string Currency { get; set; } = "INR";

        public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    }

}
