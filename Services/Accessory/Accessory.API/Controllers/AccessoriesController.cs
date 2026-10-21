using Accessory.Application.DTOs;
using Accessory.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Accessory.API.Controllers
{
    [ApiController]
    [Route("api/accessories")]
    public class AccessoriesController : ControllerBase
    {
        private readonly AccessoryApplicationService _service;

        public AccessoriesController(
            AccessoryApplicationService service)
        {
            _service = service;
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(
            CreateAccessoryRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _service.CreateAsync(
                request, cancellationToken);

            return Created(
                $"/api/accessories/{result.Id}",
                result);
        }

        [HttpGet("{accessoryId:long}")]
        [Authorize]
        public async Task<IActionResult> Get(
            long accessoryId,
            CancellationToken cancellationToken)
        {
            var result = await _service.GetAsync(
                accessoryId, cancellationToken);

            return result == null
                ? NotFound()
                : Ok(result);
        }

        [HttpGet("compatible/{itemId:int}")]
        [Authorize]
        public async Task<IActionResult> GetCompatible(
            int itemId,
            CancellationToken cancellationToken)
        {
            return Ok(await _service.GetCompatibleAsync(
                itemId, cancellationToken));
        }

        [HttpPost("{accessoryId:long}/compatibility")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddCompatibility(
            long accessoryId,
            AddCompatibilityRequest request,
            CancellationToken cancellationToken)
        {
            var ok = await _service.AddCompatibilityAsync(
                accessoryId, request, cancellationToken);

            return ok ? Ok() : NotFound();
        }

        [HttpPatch("{accessoryId:long}/availability")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateAvailability(
            long accessoryId,
            UpdateAvailabilityRequest request,
            CancellationToken cancellationToken)
        {
            var ok = await _service.UpdateAvailabilityAsync(
                accessoryId, request, cancellationToken);

            return ok ? NoContent() : NotFound();
        }

        [HttpPatch("{accessoryId:long}/price")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdatePrice(
            long accessoryId,
            UpdateAccessoryPriceRequest request,
            CancellationToken cancellationToken)
        {
            var ok = await _service.UpdatePriceAsync(
                accessoryId, request, cancellationToken);

            return ok ? NoContent() : NotFound();
        }
    }

}
