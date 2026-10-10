using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dealer.Domain.Entities
{
    public class DealerInventory
    {
        public long Id { get; set; }
        public long DealerId { get; set; }
        public int ItemId { get; set; }
        public string SKU { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int ReservedQuantity { get; set; }
        public int AvailableQuantity => Quantity - ReservedQuantity;
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    }

}
