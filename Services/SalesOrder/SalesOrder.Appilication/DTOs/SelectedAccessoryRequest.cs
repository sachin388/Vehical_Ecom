using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesOrder.Application.DTOs
{
    public sealed class SelectedAccessoryRequest
    {
        [Range(1, int.MaxValue)]
        public int AccessoryId { get; set; }
        [Range(1, 100)]
        public int Quantity { get; set; } = 1;
    }

}
