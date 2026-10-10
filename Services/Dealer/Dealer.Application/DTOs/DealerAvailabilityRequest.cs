using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dealer.Application.DTOs
{
    public class DealerAvailabilityRequest
    {
        public int ItemId { get; set; }
        public string SKU { get; set; } = string.Empty;
        public int Quantity { get; set; } = 1;
        public string? City { get; set; }
        public string? StateCode { get; set; }
        public long? PreferredDealerId { get; set; }
    }

}
