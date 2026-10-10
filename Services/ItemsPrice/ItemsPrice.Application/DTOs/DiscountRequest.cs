using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItemsPrice.Application.DTOs
{
    public class DiscountRequest
    {
        public string Name { get; set; } = string.Empty;

        public DiscountType DiscountType { get; set; }

        public decimal DiscountValue { get; set; }

        public decimal? MaximumDiscountAmount { get; set; }

        public DateTime EffectiveFromUtc { get; set; }

        public DateTime? EffectiveToUtc { get; set; }
    }

}
