using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dealer.Application.DTOs
{
    public class AssignDealerRequest : DealerAvailabilityRequest
    {
        public Guid SalesOrderId { get; set; }
    }

}
