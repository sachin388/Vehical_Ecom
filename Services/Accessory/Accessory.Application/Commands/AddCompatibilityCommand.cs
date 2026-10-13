using Accessory.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Accessory.Application.Commands
{
    public class AddCompatibilityCommand
    {
        public long AccessoryId { get; set; }

        public AddCompatibilityRequest Request { get; set; } = new();
    }

}
