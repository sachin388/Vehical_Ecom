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
    public class RtoChargeRule : IChargeRule
    {
        public ChargeType SupportedChargeType => ChargeType.RTO;

        public PriceBreakupLine Calculate(
            PriceCalculationContext context)
        {
            return new PriceBreakupLine
            {
                ChargeType = ChargeType.RTO,
                Code = "RTO",
                Name = "RTO / Road Tax",
                Amount = context.RtoAmount,
                CalculationBasis = "State/category rule"
            };
        }
    }

}
