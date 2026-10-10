using Dealer.Application.DTOs;
using Dealer.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Dealer.API.Controllers
{
    [ApiController]
    [Route("api/dealers")]
    public sealed class DealersController : ControllerBase
    {
        private readonly DealerApplicationService _service;

        public DealersController(DealerApplicationService service)
            => _service = service;

        // POST /api/dealers
        [HttpPost]
        [ProducesResponseType(typeof(DealerResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<DealerResponse>> Create(
            [FromBody] CreateDealerRequest request, CancellationToken ct)
        {
            try
            {
                var result = await _service.CreateDealerAsync(request, ct);
                return CreatedAtAction(nameof(GetById),
                    new { dealerId = result.Id }, result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid dealer request",
                    Detail = ex.Message,
                    Status = StatusCodes.Status400BadRequest
                });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new ProblemDetails
                {
                    Title = "Dealer already exists",
                    Detail = ex.Message,
                    Status = StatusCodes.Status409Conflict
                });
            }
        }

        // GET /api/dealers/{dealerId}
        [HttpGet("{dealerId:long}")]
        [ProducesResponseType(typeof(DealerResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DealerResponse>> GetById(
            [FromRoute] long dealerId, CancellationToken ct)
        {
            if (dealerId <= 0)
                return BadRequest("dealerId must be positive.");

            var dealer = await _service.GetDealerByIdAsync(dealerId, ct);
            return dealer is null ? NotFound() : Ok(dealer);
        }

        // GET /api/dealers/available?itemId=101&sku=BIKE-101&quantity=1&city=Pune
        [HttpGet("available")]
        [ProducesResponseType(typeof(List<DealerAvailabilityResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<List<DealerAvailabilityResponse>>> GetAvailable(
            [FromQuery] DealerAvailabilityRequest request, CancellationToken ct)
        {
            try
            {
                var result = await _service.GetAvailableDealersAsync(request, ct);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid availability request",
                    Detail = ex.Message,
                    Status = StatusCodes.Status400BadRequest
                });
            }
        }
    }

}
