using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Dealer.API.Controllers
{
    [ApiController]
    [Route("api/dealers/{dealerId:long}/inventory")]
    public sealed class DealerInventoryController : ControllerBase
    {
        private readonly IDealerInventoryApplicationService _service;

        public DealerInventoryController(IDealerInventoryApplicationService service)
            => _service = service;

        // POST /api/dealers/{dealerId}/inventory
        // Sets the physical quantity; the application service must reject a quantity
        // lower than ReservedQuantity and must verify that the dealer is active.
        [HttpPost]
        [ProducesResponseType(typeof(DealerInventoryResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<DealerInventoryResponse>> SetInventory(
            [FromRoute] long dealerId,
            [FromBody] SetDealerInventoryRequest request,
            CancellationToken ct)
        {
            if (dealerId <= 0)
                return BadRequest("dealerId must be positive.");

            try
            {
                var result = await _service.SetInventoryAsync(dealerId, request, ct);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid inventory request",
                    Detail = ex.Message,
                    Status = StatusCodes.Status400BadRequest
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Dealer or inventory not found",
                    Detail = ex.Message,
                    Status = StatusCodes.Status404NotFound
                });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new ProblemDetails
                {
                    Title = "Inventory update conflict",
                    Detail = ex.Message,
                    Status = StatusCodes.Status409Conflict
                });
            }
        }

        // GET /api/dealers/{dealerId}/inventory
        [HttpGet]
        [ProducesResponseType(typeof(IReadOnlyList<DealerInventoryResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IReadOnlyList<DealerInventoryResponse>>> GetInventory(
            [FromRoute] long dealerId, CancellationToken ct)
        {
            if (dealerId <= 0)
                return BadRequest("dealerId must be positive.");

            try
            {
                var inventory = await _service.GetInventoryAsync(dealerId, ct);
                return Ok(inventory);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Dealer not found",
                    Detail = ex.Message,
                    Status = StatusCodes.Status404NotFound
                });
            }
        }
    }

}
