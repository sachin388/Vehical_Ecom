using ItemsPrice.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItemsPrice.Application.Commands
{
    public class CreatePriceCommand
    {
        public CreatePriceRequest Request { get; set; } = new();
    }

}
