using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesOrder.Application.DTOs
{
    public sealed class CreateSalesOrderRequest
    {
        [Required, MinLength(1)]
        public List<CreateSalesOrderLineRequest> Lines { get; set; } = [];
        [Required, StringLength(3, MinimumLength = 3)]
        public string Currency { get; set; } = "INR";
        public string? DeliveryPostalCode { get; set; }
    }

}
