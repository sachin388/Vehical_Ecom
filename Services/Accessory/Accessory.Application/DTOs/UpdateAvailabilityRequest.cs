using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Accessory.Application.DTOs
{
    public class UpdateAvailabilityRequest
    {
        public int AvailableQuantity { get; set; }

        public AccessoryStatus Status { get; set; }
    }

}
