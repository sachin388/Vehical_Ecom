using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Accessory.Application.DTOs
{
    public class AccessoryCompatibilityResponse
    {
        public long Id { get; set; }

        public long AccessoryId { get; set; }

        public int ItemId { get; set; }

        public string SKU { get; set; } = string.Empty;

        public bool IsActive { get; set; }
    }

}
