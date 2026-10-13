using Accessory.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Accessory.Application.Commands
{
    public class UpdateAccessoryCommand
    {
        public long AccessoryId { get; set; }

        public UpdateAccessoryRequest Request { get; set; } = new();
    }

}
