using Accessory.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Accessory.Application.Commands
{
    public class UpdateAvailabilityCommand
    {
        public long AccessoryId { get; set; }

        public UpdateAvailabilityRequest Request { get; set; } = new();
    }

}
