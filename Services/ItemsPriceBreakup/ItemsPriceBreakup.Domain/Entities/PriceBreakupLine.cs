using ItemsPriceBreakup.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItemsPriceBreakup.Domain.Entities
{
    public class PriceBreakupLine
    {
        public long Id { get; set; }

        public long PriceBreakupId { get; set; }

        public ChargeType ChargeType { get; set; }

        public string Code { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public decimal TaxAmount { get; set; }

        public bool IsTaxIncluded { get; set; }

        public string CalculationBasis { get; set; } = string.Empty;

        public string RuleVersion { get; set; } = "v1";

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public PriceBreakup? PriceBreakup { get; set; }
    }

}
