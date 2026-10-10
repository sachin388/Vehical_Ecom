using Dealer.Application.DTOs;
using Dealer.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dealer.Infrastructure.Selection
{
    public class LocationPriorityDealerSelectionStrategy : IDealerSelectionStrategy
    {
        public decimal Score(DealerAvailabilityCandidate c, DealerAvailabilityRequest r)
        {
            decimal score = 0;
            if (!string.IsNullOrWhiteSpace(r.City) &&
                string.Equals(c.City, r.City, StringComparison.OrdinalIgnoreCase))
                score += 100m;

            // DistanceKm must come from a trusted distance calculation.
            score += Math.Max(0m, 50m - c.DistanceKm);
            score += Math.Min(c.AvailableQuantity, 10) * 2m;
            return score;
        }
    }

}
