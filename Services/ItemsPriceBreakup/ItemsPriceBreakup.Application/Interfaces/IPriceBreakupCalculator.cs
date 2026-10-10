using ItemsPriceBreakup.Application.Pricing;
using ItemsPriceBreakup.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItemsPriceBreakup.Application.Interfaces
{
    public interface IPriceBreakupCalculator
    {
        List<PriceBreakupLine> Calculate(
            PriceCalculationContext context);
    }

}
