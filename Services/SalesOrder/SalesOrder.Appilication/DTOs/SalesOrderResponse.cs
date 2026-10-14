using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesOrder.Application.DTOs
{
    public sealed class SalesOrderResponse
    {
        public Guid Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public long? DealerId { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal GrandTotal { get; set; }
        public string Currency { get; set; } = "INR";
        public DateTime CreatedAtUtc { get; set; }
        public List<SalesOrderLineResponse> Lines { get; set; } = [];
    }

}
