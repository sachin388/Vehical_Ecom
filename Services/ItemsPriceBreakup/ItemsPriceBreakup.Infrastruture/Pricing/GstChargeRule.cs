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
    public class GstChargeRule : IChargeRule
    {
        public ChargeType SupportedChargeType =>
            ChargeType.GST;

        public PriceBreakupLine Calculate(
            PriceCalculationContext context)
        {
            var taxableBase = Math.Max(
                context.ExShowroomPrice - context.Discount, 0);

            var gst = Math.Round(
                taxableBase * context.GstRate / 100m, 2);

            return new PriceBreakupLine
            {
                ChargeType = ChargeType.GST,
                Code = "GST",
                Name = "GST",
                Amount = gst,
                TaxAmount = gst,
                CalculationBasis =
                    $"{taxableBase} x {context.GstRate}%"
            };
        }
    }

}
