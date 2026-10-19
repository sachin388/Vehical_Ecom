using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SalesOrder.Application.Services;

namespace SalesOrder.API.Controllers
{
    [ApiController]
    [Route("internal/sales-orders")]
    [Authorize(Policy = "InternalService")]
    public sealed class InternalSalesOrdersController : ControllerBase
    {
        private readonly ISalesOrderApplicationService _salesOrderService;

        public InternalSalesOrdersController(
            ISalesOrderApplicationService salesOrderService)
        {
            _salesOrderService = salesOrderService;
        }

        [HttpGet("{orderId:guid}")]
        public async Task<IActionResult> GetOrder(
            Guid orderId,
            CancellationToken cancellationToken)
        {
            var order = await _salesOrderService.GetByPublicIdAsync(
                orderId,
                cancellationToken);

            if (order is null)
                return NotFound();

            return Ok(order);
        }
    }
}
