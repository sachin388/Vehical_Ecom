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

    public class InsuranceChargeRule : IChargeRule
    {
        public ChargeType SupportedChargeType =>
            ChargeType.Insurance;

        public PriceBreakupLine Calculate(
            PriceCalculationContext context)
        {
            return new PriceBreakupLine
            {
                ChargeType = ChargeType.Insurance,
                Code = "INSURANCE",
                Name = "Insurance",
                Amount = context.InsuranceAmount,
                CalculationBasis = "Insurance quote/rule"
            };
        }
    }

}
