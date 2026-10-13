using ItemsPriceBreakup.Application.DTO;
using ItemsPriceBreakup.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ItemsPriceBreakup.API.Controllers
{
   [ApiController]
[Route("api/price-breakups")]
public class PriceBreakupsController : ControllerBase
{
    private readonly PriceBreakupApplicationService _service;

    public PriceBreakupsController(
        PriceBreakupApplicationService service)
    {
        _service = service;
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
}

}
