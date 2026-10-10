using Dealer.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dealer.Application.Interfaces
    public interface IDealerSelectionStrategy
    {
        decimal Score(DealerAvailabilityCandidate candidate, DealerAvailabilityRequest request);
    }

}
