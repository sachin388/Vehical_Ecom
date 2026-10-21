using System;
using System.Collections.Generic;
using Accessory.Domain.Enums;

namespace Accessory.Domain.Entities
{
    public class Accessory
    {
        public long Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Currency { get; set; } = "INR";
        public int AvailableQuantity { get; set; }
        public AccessoryStatus Status { get; set; } = AccessoryStatus.Active;
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }
        public ICollection<AccessoryCompatibility> Compatibilities { get; set; } = new List<AccessoryCompatibility>();
    }
}
