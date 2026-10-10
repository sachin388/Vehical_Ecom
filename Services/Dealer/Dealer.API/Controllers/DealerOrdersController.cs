using Dealer.Application.DTOs;
using Dealer.Application.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Dealer.API.Controllers
{
    [ApiController]
    [Route("api/dealers/orders")]
    public sealed class DealerOrdersController : ControllerBase
    {
        private readonly DealerApplicationService _service;

        public DealerOrdersController(DealerApplicationService service)
            => _service = service;

        // POST /api/dealers/orders/assign
        [HttpPost("assign")]
        [ProducesResponseType(typeof(DealerAssignmentResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<DealerAssignmentResponse>> Assign(
            [FromBody] AssignDealerRequest request, CancellationToken ct)
        {
            try
            {
                var assignment = await _service.AssignOrderAsync(request, ct);
                if (assignment is null)
                    return Conflict(new ProblemDetails
                    {
                        Title = "No dealer could be allocated",
                        Detail = "No eligible dealer has sufficient available inventory.",
                        Status = StatusCodes.Status409Conflict
                    });

                return Ok(assignment);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid dealer assignment request",
                    Detail = ex.Message,
                    Status = StatusCodes.Status400BadRequest
                });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new ProblemDetails
                {
                    Title = "Dealer assignment conflict",
                    Detail = ex.Message,
                    Status = StatusCodes.Status409Conflict
                });
            }
        }

        // GET /api/dealers/orders/{salesOrderId}
        [HttpGet("{salesOrderId:guid}")]
        [ProducesResponseType(typeof(DealerAssignmentResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DealerAssignmentResponse>> GetBySalesOrder(
            [FromRoute] Guid salesOrderId, CancellationToken ct)
        {
            if (salesOrderId == Guid.Empty)
                return BadRequest("salesOrderId must not be empty.");

            var assignment = await _service.GetAssignmentByOrderIdAsync(salesOrderId, ct);
            return assignment is null ? NotFound() : Ok(assignment);
        }

        // PATCH /api/dealers/orders/{salesOrderId}/status
        [HttpPatch("{salesOrderId:guid}/status")]
        [ProducesResponseType(typeof(DealerAssignmentResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<DealerAssignmentResponse>> UpdateStatus(
            [FromRoute] Guid salesOrderId,
            [FromBody] UpdateDealerAssignmentStatusRequest request,
            CancellationToken ct)
        {
            if (salesOrderId == Guid.Empty)
                return BadRequest("salesOrderId must not be empty.");

            if (string.IsNullOrWhiteSpace(request.Status))
                return BadRequest("Status is required.");

            try
            {
                var result = await _service.UpdateAssignmentStatusAsync(
                    salesOrderId, request.Status, ct);
                return result is null ? NotFound() : Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid assignment status",
                    Detail = ex.Message,
                    Status = StatusCodes.Status400BadRequest
                });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new ProblemDetails
                {
                    Title = "Assignment status conflict",
                    Detail = ex.Message,
                    Status = StatusCodes.Status409Conflict
                });
            }
        }
    }

}
