using ItemsPrice.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItemsPrice.Application.Commands
{
    public class AddDiscountCommand
    {
        public long PriceId { get; set; }

        public DiscountRequest Request { get; set; } = new();
    }

}
