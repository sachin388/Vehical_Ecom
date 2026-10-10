using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItemsPrice.Domain.Entities
{
    public class PriceDiscount
    {
        public long Id { get; set; }

        public long ItemPriceId { get; set; }

        public string Name { get; set; } = string.Empty;

        public DiscountType DiscountType { get; set; }

        public decimal DiscountValue { get; set; }

        public decimal? MaximumDiscountAmount { get; set; }

        public DateTime EffectiveFromUtc { get; set; }

        public DateTime? EffectiveToUtc { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAtUtc { get; set; }

        public ItemPrice? ItemPrice { get; set; }
    }

