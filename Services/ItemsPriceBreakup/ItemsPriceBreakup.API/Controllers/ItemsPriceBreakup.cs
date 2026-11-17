using ItemsPriceBreakup.Application.DTO;
using ItemsPriceBreakup.Application.Services;
using ItemsPriceBreakup.Infrastruture.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ItemsPriceBreakup.API.Controllers
{
    [ApiController]
    [Route("api/price-breakups")]
    public class PriceBreakupsController : ControllerBase
    {
        private readonly PriceBreakupApplicationService _service;
        private readonly PriceBreakupDbContext _dbContext;

        public PriceBreakupsController(
            PriceBreakupApplicationService service,
            PriceBreakupDbContext dbContext)
        {
            _service = service;
            _dbContext = dbContext;
        }

        [HttpPost("calculate")]
        [Authorize]
        public async Task<IActionResult> Calculate(
            CalculatePriceBreakupRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _service.CalculateAsync(
                request, cancellationToken);

            return Ok(result);
        }

        [HttpGet("current/{itemId:int}")]
        [HttpGet("{itemId:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCurrentByItemId(
            int itemId,
            CancellationToken cancellationToken)
        {
            var breakup = await _dbContext.PriceBreakups
                .Include(x => x.Lines)
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync(x => x.ItemId == itemId, cancellationToken);

            if (breakup == null)
                return NotFound(new { message = $"No price breakup found for item {itemId}" });

            var response = new PriceBreakupResponse
            {
                Id = breakup.Id,
                ItemId = breakup.ItemId,
                SKU = breakup.SKU,
                StateCode = breakup.StateCode,
                VehicleCategory = breakup.VehicleCategory,
                PriceVersion = breakup.PriceVersion,
                Currency = breakup.Currency,
                SubTotal = breakup.SubTotal,
                TotalTax = breakup.TotalTax,
                TotalCharges = breakup.TotalCharges,
                TotalDiscount = breakup.TotalDiscount,
                FinalOnRoadPrice = breakup.FinalOnRoadPrice,
                Status = breakup.Status,
                CreatedAtUtc = breakup.CreatedAtUtc,
                Lines = breakup.Lines.Select(l => new PriceBreakupLineResponse
                {
                    Id = l.Id,
                    ChargeType = l.ChargeType,
                    Code = l.Code,
                    Name = l.Name,
                    Amount = l.Amount,
                    TaxAmount = l.TaxAmount,
                    IsTaxIncluded = l.IsTaxIncluded,
                    CalculationBasis = l.CalculationBasis,
                    RuleVersion = l.RuleVersion
                }).ToList()
            };

            return Ok(response);
        }
    }
}
