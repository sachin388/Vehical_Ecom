using ItemsPriceBreakup.Application.Interfaces;
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
    public class PriceBreakupCalculator : IPriceBreakupCalculator
    {
        private readonly IChargeRuleFactory _factory;

        public PriceBreakupCalculator(
            IChargeRuleFactory factory)
        {
            _factory = factory;
        }

        public List<PriceBreakupLine> Calculate(
            PriceCalculationContext context)
        {
            var lines = new List<PriceBreakupLine>();

            var types = new[]
            {
            ChargeType.ExShowroom,
            ChargeType.GST,
            ChargeType.RTO,
            ChargeType.Registration,
            ChargeType.Insurance,
            ChargeType.Handling,
            ChargeType.Other
        };

            foreach (var type in types)
            {
                var line = _factory.GetRule(type)
                    .Calculate(context);

                if (line.Amount > 0)
                    lines.Add(line);
            }

            return lines;
        }
    }

}
