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
    public class RegistrationChargeRule : IChargeRule
    {
        public ChargeType SupportedChargeType =>
            ChargeType.Registration;

        public PriceBreakupLine Calculate(
            PriceCalculationContext context)
        {
            return new PriceBreakupLine
            {
                ChargeType = ChargeType.Registration,
                Code = "REGISTRATION",
                Name = "Registration",
                Amount = context.RegistrationAmount,
                CalculationBasis = "Registration rule"
            };
        }
    }

}
