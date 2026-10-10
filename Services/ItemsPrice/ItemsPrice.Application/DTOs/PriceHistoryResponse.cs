using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItemsPrice.Application.DTOs
{
    public class PriceHistoryResponse
    {
        public long PriceId { get; set; }

        public decimal BasePrice { get; set; }

        public string Currency { get; set; } = "INR";

        public int Version { get; set; }

        public PriceStatus Status { get; set; }

        public DateTime EffectiveFromUtc { get; set; }

        public DateTime? EffectiveToUtc { get; set; }

        public DateTime CreatedAtUtc { get; set; }
    }

}
