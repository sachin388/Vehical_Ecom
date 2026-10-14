using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesOrder.Domain.Enums
{
    public enum SalesOrderStatus
    {
        Draft = 1,
        PendingPayment = 2,
        Confirmed = 3,
        Allocated = 4,
        Processing = 5,
        Completed = 6,
        Cancelled = 7
    }

}
