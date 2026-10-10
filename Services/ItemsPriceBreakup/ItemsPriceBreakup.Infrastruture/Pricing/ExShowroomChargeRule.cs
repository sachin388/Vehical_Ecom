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
    public class ExShowroomChargeRule : IChargeRule
    {
        public ChargeType SupportedChargeType =>
            ChargeType.ExShowroom;

        public PriceBreakupLine Calculate(
            PriceCalculationContext context)
        {
            return new PriceBreakupLine
            {
                ChargeType = ChargeType.ExShowroom,
                Code = "EX_SHOWROOM",
                Name = "Ex-Showroom Price",
                Amount = context.ExShowroomPrice,
                CalculationBasis = "Base vehicle price"
            };
        }
    }

}
