using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dealer.Application.DTOs
{
    public class DealerAssignmentResponse
    {
        public long AssignmentId { get; set; }
        public Guid SalesOrderId { get; set; }
        public long DealerId { get; set; }
        public string DealerName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime AssignedAtUtc { get; set; }
    }

}
