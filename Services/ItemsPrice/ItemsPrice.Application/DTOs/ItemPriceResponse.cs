using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItemsPrice.Application.DTOs
{
    public class ItemPriceResponse
    {
        public long PriceId { get; set; }

        public int ItemId { get; set; }

        public string SKU { get; set; } = string.Empty;

        public decimal BasePrice { get; set; }

        public string Currency { get; set; } = "INR";

        public decimal TotalDiscount { get; set; }

        public decimal FinalPrice { get; set; }

        public DateTime EffectiveFromUtc { get; set; }

        public DateTime? EffectiveToUtc { get; set; }

        public int Version { get; set; }

        public PriceStatus Status { get; set; }
    }

}
