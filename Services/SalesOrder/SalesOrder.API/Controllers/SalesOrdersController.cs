using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalesOrder.Application.DTOs;
using SalesOrder.Application.Services;
using SalesOrder.Infrastructure.Persistence;

namespace SalesOrder.API.Controllers
{
    [ApiController]
    [Route("api/sales-orders")]
    public sealed class SalesOrdersController : ControllerBase
    {
        private readonly ISalesOrderApplicationService _service;
        private readonly SalesOrderDbContext _dbContext;

        public SalesOrdersController(
            ISalesOrderApplicationService service,
            SalesOrderDbContext dbContext)
        {
            _service = service;
            _dbContext = dbContext;
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateSalesOrderRequest request,
            CancellationToken ct)
        {
            var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")
                ?? "USR-DEMO-001";

            var result = await _service.CreateAsync(customerId, request, ct);
            return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken ct)
        {
            var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");

            var query = _dbContext.SalesOrders
                .Include(x => x.Lines)
                .AsQueryable();

            if (!string.IsNullOrEmpty(customerId))
            {
                query = query.Where(x => x.CustomerId == customerId);
            }

            var list = await query
                .OrderByDescending(x => x.CreatedAtUtc)
                .Take(50)
                .Select(x => new
                {
                    id = x.Id,
                    publicId = x.PublicId,
                    orderNumber = x.OrderNumber,
                    customerId = x.CustomerId,
                    dealerId = x.DealerId,
                    status = x.Status.ToString(),
                    grandTotal = x.GrandTotalSnapshot,
                    finalAmount = x.GrandTotalSnapshot,
                    currency = x.Currency,
                    createdAtUtc = x.CreatedAtUtc,
                    lines = x.Lines.Select(l => new
                    {
                        id = l.Id,
                        itemId = l.ItemId,
                        itemName = "Vehicle Model #" + l.ItemId,
                        quantity = l.Quantity,
                        unitPrice = l.UnitPriceSnapshot,
                        lineTotal = l.LineTotalSnapshot,
                        discountAmount = l.DiscountSnapshot,
                        taxAmount = l.TaxSnapshot
                    }).ToList()
                })
                .ToListAsync(ct);

            return Ok(list);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(
            string id,
            CancellationToken ct)
        {
            Domain.Entities.SalesOrder? order = null;

            if (Guid.TryParse(id, out var guid))
            {
                order = await _dbContext.SalesOrders
                    .Include(x => x.Lines)
                    .FirstOrDefaultAsync(x => x.PublicId == guid, ct);
            }
            else if (long.TryParse(id, out var longId))
            {
                order = await _dbContext.SalesOrders
                    .Include(x => x.Lines)
                    .FirstOrDefaultAsync(x => x.Id == longId, ct);
            }

            if (order == null)
            {
                order = await _dbContext.SalesOrders
                    .Include(x => x.Lines)
                    .FirstOrDefaultAsync(x => x.OrderNumber == id, ct);
            }

            if (order == null)
                return NotFound();

            var response = new
            {
                id = order.Id,
                publicId = order.PublicId,
                orderNumber = order.OrderNumber,
                customerId = order.CustomerId,
                dealerId = order.DealerId,
                status = order.Status.ToString(),
                grandTotal = order.GrandTotalSnapshot,
                finalAmount = order.GrandTotalSnapshot,
                currency = order.Currency,
                createdAtUtc = order.CreatedAtUtc,
                lines = order.Lines.Select(l => new
                {
                    id = l.Id,
                    itemId = l.ItemId,
                    itemName = "Vehicle Model #" + l.ItemId,
                    quantity = l.Quantity,
                    unitPrice = l.UnitPriceSnapshot,
                    lineTotal = l.LineTotalSnapshot,
                    discountAmount = l.DiscountSnapshot,
                    taxAmount = l.TaxSnapshot
                }).ToList()
            };

            return Ok(response);
        }

        [HttpGet("{id}/history")]
        public async Task<IActionResult> GetHistory(
            string id,
            CancellationToken ct)
        {
            Guid targetGuid = Guid.Empty;
            if (Guid.TryParse(id, out var g))
            {
                targetGuid = g;
            }
            else if (long.TryParse(id, out var lId))
            {
                var o = await _dbContext.SalesOrders.FirstOrDefaultAsync(x => x.Id == lId, ct);
                if (o != null) targetGuid = o.PublicId;
            }

            var history = await _dbContext.SalesOrderHistory
                .Where(x => targetGuid == Guid.Empty || x.SalesOrderPublicId == targetGuid)
                .OrderByDescending(x => x.ChangedAtUtc)
                .Select(x => new
                {
                    id = x.Id,
                    status = x.ToStatus,
                    message = !string.IsNullOrEmpty(x.Reason) ? x.Reason : $"Status changed to {x.ToStatus}",
                    occurredAtUtc = x.ChangedAtUtc
                })
                .ToListAsync(ct);

            return Ok(history);
        }

        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> Cancel(
            string id,
            [FromBody] CancelSalesOrderRequest request,
            CancellationToken ct)
        {
            var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")
                ?? "USR-DEMO-001";

            Guid guid = Guid.Empty;
            if (Guid.TryParse(id, out var g))
            {
                guid = g;
            }
            else if (long.TryParse(id, out var lId))
            {
                var o = await _dbContext.SalesOrders.FirstOrDefaultAsync(x => x.Id == lId, ct);
                if (o != null) guid = o.PublicId;
            }

            if (guid != Guid.Empty)
            {
                await _service.CancelAsync(guid, customerId, request.Reason, ct);
            }
            return NoContent();
        }
    }
}
