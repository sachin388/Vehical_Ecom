using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Accessory.Application.DTOs
{
    public class AddCompatibilityRequest
    {
        public int ItemId { get; set; }

        public string SKU { get; set; } = string.Empty;
    }

}
