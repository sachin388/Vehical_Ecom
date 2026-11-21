using ItemsPrice.Application.Commands;
using ItemsPrice.Application.DTOs;
using ItemsPrice.Application.Services.NewFolder;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VehicleEcom.Security.Authorization;
using VehicleEcom.Security.Constants;

namespace ItemsPrice.API.Controllers
{
    [ApiController]
    [Route("api/item-prices")]
    public class ItemPricesController : ControllerBase
    {
        private readonly ItemPriceApplicationService _service;

        public ItemPricesController(ItemPriceApplicationService service)
        {
            _service = service;
        }

        [HttpGet("current/{itemId:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCurrentPrice(
            int itemId,
            CancellationToken cancellationToken)
        {
            var result = await _service.GetCurrentPriceAsync(itemId, cancellationToken);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpGet("history/{itemId:int}")]
        [Authorize]
        [HasPermission(VehiclePermissions.PricingRead)]
        public async Task<IActionResult> GetHistory(
            int itemId,
            CancellationToken cancellationToken)
        {
            var result = await _service.GetHistoryAsync(itemId, cancellationToken);
            return Ok(result);
        }

        [HttpPost]
        [HasPermission(VehiclePermissions.PricingManage)]
        public async Task<IActionResult> Create(
            CreatePriceRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _service.CreateAsync(
                new CreatePriceCommand
                {
                    Request = request
                },
                cancellationToken);

            return Created($"/api/item-prices/{result.PriceId}", result);
        }

        [HttpPut("{priceId:long}")]
        [HasPermission(VehiclePermissions.PricingManage)]
        public async Task<IActionResult> Update(
            long priceId,
            UpdatePriceRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _service.UpdateAsync(
                new UpdatePriceCommand
                {
                    PriceId = priceId,
                    Request = request
                },
                cancellationToken);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpPost("{priceId:long}/discounts")]
        [HasPermission(VehiclePermissions.PricingManage)]
        public async Task<IActionResult> AddDiscount(
            long priceId,
            DiscountRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _service.AddDiscountAsync(
                new AddDiscountCommand
                {
                    PriceId = priceId,
                    Request = request
                },
                cancellationToken);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpPatch("{priceId:long}/deactivate")]
        [HasPermission(VehiclePermissions.PricingManage)]
        public async Task<IActionResult> Deactivate(
            long priceId,
            CancellationToken cancellationToken)
        {
            var result = await _service.DeactivateAsync(
                new DeactivatePriceCommand
                {
                    PriceId = priceId
                },
                cancellationToken);

            if (!result)
                return NotFound();

            return NoContent();
        }
    }
}
