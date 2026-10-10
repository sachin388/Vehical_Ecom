using ItemsPriceBreakup.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItemsPriceBreakup.Infrastruture.Pricing
{
    public class ChargeRuleFactory : IChargeRuleFactory
    {
        private readonly Dictionary<ChargeType, IChargeRule> _rules;

        public ChargeRuleFactory(IEnumerable<IChargeRule> rules)
        {
            _rules = rules.ToDictionary(x => x.SupportedChargeType);
        }

        public IChargeRule GetRule(ChargeType chargeType)
        {
            if (!_rules.TryGetValue(chargeType, out var rule))
                throw new InvalidOperationException(
                    $"No rule configured for {chargeType}.");

            return rule;
        }
    }

}
