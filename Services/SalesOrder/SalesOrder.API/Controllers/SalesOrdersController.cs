using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesOrder.Application.DTOs;
using SalesOrder.Application.Services;

namespace SalesOrder.API.Controllers
{
    [ApiController]
    [Route("api/sales-orders")]
    [Authorize]
    public sealed class SalesOrdersController : ControllerBase
    {
        private readonly ISalesOrderApplicationService _service;

        public SalesOrdersController(ISalesOrderApplicationService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateSalesOrderRequest request,
            CancellationToken ct)
        {
            var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")
                ?? "anonymous";

            var result = await _service.CreateAsync(customerId, request, ct);
            return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> Get(
            Guid id,
            CancellationToken ct)
        {
            var result = await _service.GetAsync(id, ct);
            if (result is null)
                return NotFound();

            return Ok(result);
        }

        [HttpPost("{id:guid}/cancel")]
        public async Task<IActionResult> Cancel(
            Guid id,
            [FromBody] CancelSalesOrderRequest request,
            CancellationToken ct)
        {
            var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")
                ?? "anonymous";

            await _service.CancelAsync(id, customerId, request.Reason, ct);
            return NoContent();
        }
    }
}
