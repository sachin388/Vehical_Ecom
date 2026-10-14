using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesOrder.Domain.Entities
{
    public sealed class SalesOrderLineAccessory
    {
        public long Id { get; set; }
        public long SalesOrderLineId { get; set; }
        public int AccessoryId { get; set; }
        public string AccessoryNameSnapshot { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPriceSnapshot { get; set; }
        public decimal LineTotalSnapshot { get; set; }
    }

}
