using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesOrder.Domain.Entities
{
    public sealed class SalesOrderLine
    {
        public long Id { get; set; }
        public long SalesOrderId { get; set; }
        public int ItemId { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemNameSnapshot { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPriceSnapshot { get; set; }
        public decimal DiscountSnapshot { get; set; }
        public decimal TaxSnapshot { get; set; }
        public decimal LineTotalSnapshot { get; set; }
        public string Currency { get; set; } = "INR";
        public List<SalesOrderLineAccessory> Accessories { get; set; } = [];
    }

}
