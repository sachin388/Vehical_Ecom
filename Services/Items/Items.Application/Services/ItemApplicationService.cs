using Items.Application.Commands;
using Items.Application.DTOs;
using Items.Application.Interfaces;
using Items.Application.Queries;
using Items.Domain.Entities;
using Items.Domain.Enums;
using ItemService.Application.Commands;
using ItemService.Application.DTOs;
using ItemService.Domain.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Items.Application.Services
{
    public sealed class ItemApplicationService
    {
        private readonly IItemRepository _repository;
        private readonly IEventPublisher _eventPublisher;

        public ItemApplicationService(
            IItemRepository repository,
            IEventPublisher eventPublisher)
        {
            _repository = repository;
            _eventPublisher = eventPublisher;
        }

        public async Task<ItemResponse> CreateAsync(
            CreateItemCommand command,
            CancellationToken cancellationToken)
        {
            var request = command.Request;

            if (string.IsNullOrWhiteSpace(request.SKU))
                throw new ArgumentException("SKU is required.");

            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("Vehicle name is required.");

            var existing = await _repository.GetBySkuAsync(
                request.SKU,
                cancellationToken);

            if (existing is not null)
                throw new InvalidOperationException(
                    $"Vehicle with SKU '{request.SKU}' already exists.");

            var now = DateTime.UtcNow;

            var item = new Item
            {
                SKU = request.SKU.Trim(),
                Name = request.Name.Trim(),
                VehicleMakeId = request.VehicleMakeId,
                VehicleModelId = request.VehicleModelId,
                VehicleVariantId = request.VehicleVariantId,
                VehicleCategoryId = request.VehicleCategoryId,
                Description = request.Description,
                Status = ItemStatus.Active,
                CreatedAtUtc = now
            };

            await _repository.AddAsync(item, cancellationToken);

            await _repository.SaveChangesAsync(cancellationToken);

            var vehicleCreatedEvent = new VehicleCreatedEvent(
                Guid.NewGuid(),
                item.Id,
                item.SKU,
                item.Name,
                item.VehicleMakeId,
                item.VehicleModelId,
                item.VehicleVariantId,
                item.VehicleCategoryId,
                now);

            await _eventPublisher.PublishAsync(
                "vehicle.created",
                vehicleCreatedEvent,
                cancellationToken);

            var createdItem = await _repository.GetByIdAsync(
                item.Id,
                cancellationToken);

            return MapToResponse(createdItem!);
        }

        public async Task<ItemResponse?> GetByIdAsync(
            GetItemByIdQuery query,
            CancellationToken cancellationToken)
        {
            var item = await _repository.GetByIdAsync(
                query.ItemId,
                cancellationToken);

            return item is null
                ? null
                : MapToResponse(item);
        }

        public async Task<ItemResponse?> UpdateAsync(
            UpdateItemCommand command,
            CancellationToken cancellationToken)
        {
            var item = await _repository.GetByIdAsync(
                command.ItemId,
                cancellationToken);

            if (item is null)
                return null;

            item.Name = command.Request.Name.Trim();
            item.VehicleMakeId = command.Request.VehicleMakeId;
            item.VehicleModelId = command.Request.VehicleModelId;
            item.VehicleVariantId = command.Request.VehicleVariantId;
            item.VehicleCategoryId = command.Request.VehicleCategoryId;
            item.Description = command.Request.Description;
            item.UpdatedAtUtc = DateTime.UtcNow;

            _repository.Update(item);

            await _repository.SaveChangesAsync(cancellationToken);

            var vehicleUpdatedEvent = new VehicleUpdatedEvent(
                Guid.NewGuid(),
                item.Id,
                item.SKU,
                item.Name,
                item.UpdatedAtUtc.Value);

            await _eventPublisher.PublishAsync(
                "vehicle.updated",
                vehicleUpdatedEvent,
                cancellationToken);

            var updatedItem = await _repository.GetByIdAsync(
                item.Id,
                cancellationToken);

            return updatedItem is null
                ? null
                : MapToResponse(updatedItem);
        }

        public async Task<ItemResponse?> ChangeStatusAsync(
            ChangeItemStatusCommand command,
            CancellationToken cancellationToken)
        {
            var item = await _repository.GetByIdAsync(
                command.ItemId,
                cancellationToken);

            if (item is null)
                return null;

            if (item.Status == command.NewStatus)
                return MapToResponse(item);

            var oldStatus = item.Status;

            item.Status = command.NewStatus;
            item.UpdatedAtUtc = DateTime.UtcNow;

            _repository.Update(item);

            await _repository.SaveChangesAsync(cancellationToken);

            var statusEvent = new VehicleAvailabilityChangedEvent(
                Guid.NewGuid(),
                item.Id,
                oldStatus,
                item.Status,
                item.UpdatedAtUtc.Value);

            await _eventPublisher.PublishAsync(
                "vehicle.availability.changed",
                statusEvent,
                cancellationToken);

            var updatedItem = await _repository.GetByIdAsync(
                item.Id,
                cancellationToken);

            return updatedItem is null
                ? null
                : MapToResponse(updatedItem);
        }

        public async Task<PagedResponse<ItemResponse>> SearchAsync(
            SearchItemsQuery query,
            CancellationToken cancellationToken)
        {
            var request = query.Request;

            var page = request.Page < 1
                ? 1
                : request.Page;

            var pageSize = request.PageSize switch
            {
                < 1 => 20,
                > 100 => 100,
                _ => request.PageSize
            };

            request.Page = page;
            request.PageSize = pageSize;

            var result = await _repository.SearchAsync(
                request,
                cancellationToken);

            var responseItems = result.Items
                .Select(MapToResponse)
                .ToList();

            return new PagedResponse<ItemResponse>
            {
                Items = responseItems,
                Page = page,
                PageSize = pageSize,
                TotalCount = result.TotalCount
            };
        }

        private static ItemResponse MapToResponse(Item item)
        {
            return new ItemResponse
            {
                Id = item.Id,
                SKU = item.SKU,
                Name = item.Name,

                MakeId = item.VehicleMakeId,
                MakeName = item.VehicleMake?.Name ?? string.Empty,

                ModelId = item.VehicleModelId,
                ModelName = item.VehicleModel?.Name ?? string.Empty,

                VariantId = item.VehicleVariantId,
                VariantName = item.VehicleVariant?.Name ?? string.Empty,

                CategoryId = item.VehicleCategoryId,
                CategoryName = item.VehicleCategory?.Name ?? string.Empty,

                Status = item.Status,

                Description = item.Description,

                CreatedAtUtc = item.CreatedAtUtc,

                UpdatedAtUtc = item.UpdatedAtUtc
            };
        }
    }

}
