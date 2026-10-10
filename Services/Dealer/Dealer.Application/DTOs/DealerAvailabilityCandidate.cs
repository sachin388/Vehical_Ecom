using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dealer.Application.DTOs
{
    public class DealerAvailabilityCandidate
    {
        public long DealerId { get; set; }
        public long? DealerBranchId { get; set; }
        public string DealerName { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public int AvailableQuantity { get; set; }
        public decimal DistanceKm { get; set; }

    }
