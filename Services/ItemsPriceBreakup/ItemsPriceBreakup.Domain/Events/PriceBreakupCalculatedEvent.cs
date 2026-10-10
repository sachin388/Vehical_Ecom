using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItemsPriceBreakup.Domain.Events
{
    public class PriceBreakupCalculatedEvent
    {
        public Guid EventId { get; set; } = Guid.NewGuid();

        public long PriceBreakupId { get; set; }

        public int ItemId { get; set; }

        public string SKU { get; set; } = string.Empty;

        public string StateCode { get; set; } = string.Empty;

        public int PriceVersion { get; set; }

        public decimal FinalOnRoadPrice { get; set; }

        public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    }

}
