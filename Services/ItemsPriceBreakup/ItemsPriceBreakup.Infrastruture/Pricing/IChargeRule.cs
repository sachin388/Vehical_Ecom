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
    public interface IChargeRule
    {
        ChargeType SupportedChargeType { get; }

        PriceBreakupLine Calculate(PriceCalculationContext context);
    }

}
