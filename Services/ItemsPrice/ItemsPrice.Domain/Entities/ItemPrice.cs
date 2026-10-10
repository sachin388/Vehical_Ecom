using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItemsPrice.Domain.Entities
{
    public class ItemPrice
    {
        public long Id { get; set; }

        public int ItemId { get; set; }

        public string SKU { get; set; } = string.Empty;

        public decimal BasePrice { get; set; }

        public string Currency { get; set; } = "INR";

        public DateTime EffectiveFromUtc { get; set; }

        public DateTime? EffectiveToUtc { get; set; }

        public int Version { get; set; }

        public PriceStatus Status { get; set; } = PriceStatus.Active;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAtUtc { get; set; }

        public ICollection<PriceDiscount> Discounts { get; set; }
            = new List<PriceDiscount>();
    }

}
