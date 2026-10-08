using Items.Application.Commands;
using Items.Application.DTOs;
using Items.Application.Queries;
using Items.Application.Services;
using ItemService.Application.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Imtes.API.Controllers
{
    [ApiController]
    [Route("api/items")]
    public class ItemsController : ControllerBase
    {
        private readonly ItemApplicationService _service;

        public ItemsController(ItemApplicationService service)
        {
            _service = service;
        }

        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(
            int id,
            CancellationToken cancellationToken)
        {
            var result = await _service.GetByIdAsync(
                new GetItemByIdQuery(id),
                cancellationToken);

            if (result is null)
                return NotFound(new
                {
                    message = "Vehicle not found."
                });

            return Ok(result);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Search(
            [FromQuery] ItemSearchRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _service.SearchAsync(
                new SearchItemsQuery(request),
                cancellationToken);

            return Ok(result);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(
            [FromBody] CreateItemRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                var result = await _service.CreateAsync(
                    new CreateItemCommand(request),
                    cancellationToken);

                return CreatedAtAction(
                    nameof(GetById),
                    new { id = result.Id },
                    result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new
                {
                    message = ex.Message
                });
            }
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] UpdateItemRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _service.UpdateAsync(
                new UpdateItemCommand(id, request),
                cancellationToken);

            if (result is null)
                return NotFound(new
                {
                    message = "Vehicle not found."
                });

            return Ok(result);
        }

        [HttpPatch("{id:int}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ChangeStatus(
            int id,
            [FromBody] ChangeItemStatusRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _service.ChangeStatusAsync(
                new ChangeItemStatusCommand(
                    id,
                    request.Status),
                cancellationToken);

            if (result is null)
                return NotFound(new
                {
                    message = "Vehicle not found."
                });

            return Ok(result);
        }
    }

}
