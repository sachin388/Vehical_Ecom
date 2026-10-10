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
    public class OtherChargeRule : IChargeRule
    {
        public ChargeType SupportedChargeType =>
            ChargeType.Other;

        public PriceBreakupLine Calculate(
            PriceCalculationContext context)
        {
            return new PriceBreakupLine
            {
                ChargeType = ChargeType.Other,
                Code = "OTHER",
                Name = "Other Charges",
                Amount = context.OtherCharges,
                CalculationBasis = "Other charges rule"
            };
        }
    }

}
