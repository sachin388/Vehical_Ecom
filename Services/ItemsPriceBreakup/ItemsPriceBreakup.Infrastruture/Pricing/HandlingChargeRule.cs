using ItemsPriceBreakup.Application.Pricing;
using ItemsPriceBreakup.Domain.Entities;
using ItemsPriceBreakup.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItemsPriceBreakup.Infrastruture.Pricing
{
    public class HandlingChargeRule : IChargeRule
    {
        public ChargeType SupportedChargeType =>
            ChargeType.Handling;

        public PriceBreakupLine Calculate(
            PriceCalculationContext context)
        {
            return new PriceBreakupLine
            {
                ChargeType = ChargeType.Handling,
                Code = "HANDLING",
                Name = "Handling",
                Amount = context.HandlingAmount,
                CalculationBasis = "Handling rule"
            };
        }
    }

}
