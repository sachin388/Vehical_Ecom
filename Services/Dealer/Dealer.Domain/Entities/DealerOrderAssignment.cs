using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dealer.Domain.Entities
{
    public class DealerOrderAssignment
    {
        public long Id { get; set; }
        public Guid SalesOrderId { get; set; }
        public long DealerId { get; set; }
        public long? DealerBranchId { get; set; }
        public int ItemId { get; set; }
        public string SKU { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string Status { get; set; } = "Assigned";
        public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }
    }

}
