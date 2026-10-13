using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Accessory.Application.Commands
{
    public class UpdateAccessoryPriceCommand
    {
        public long AccessoryId { get; set; }

        public decimal Price { get; set; }
    }

}
