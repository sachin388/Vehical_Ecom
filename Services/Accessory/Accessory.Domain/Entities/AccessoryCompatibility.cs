using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Accessory.Domain.Entities
{
    public class AccessoryCompatibility
    {
        public long Id { get; set; }

        public long AccessoryId { get; set; }

        public int ItemId { get; set; }

        public string SKU { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAtUtc { get; set; } =
            DateTime.UtcNow;

        public DateTime? UpdatedAtUtc { get; set; }

        public Accessory? Accessory { get; set; }
    }

}
