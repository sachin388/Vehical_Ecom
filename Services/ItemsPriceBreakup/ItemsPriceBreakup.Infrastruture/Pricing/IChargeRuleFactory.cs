using ItemsPriceBreakup.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItemsPriceBreakup.Infrastruture.Pricing
{
    public interface IChargeRuleFactory
    {
        IChargeRule GetRule(ChargeType chargeType);
    }

}
