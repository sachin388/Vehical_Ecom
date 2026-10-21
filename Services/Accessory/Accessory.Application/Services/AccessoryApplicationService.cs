using Accessory.Application.DTOs;
using Accessory.Application.Interfaces;
using Accessory.Domain.Entities;
using Accessory.Domain.Enums;
using Accessory.Domain.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Accessory.Application.Services
{
    using Accessory = Accessory.Domain.Entities.Accessory;

    public class AccessoryApplicationService
    {
        private readonly IAccessoryRepository _repository;
        private readonly IEventPublisher _eventPublisher;

        public AccessoryApplicationService(
            IAccessoryRepository repository,
            IEventPublisher eventPublisher)
        {
            _repository = repository;
            _eventPublisher = eventPublisher;
        }

        public async Task<AccessoryResponse> CreateAsync(
            CreateAccessoryRequest request,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Code))
                throw new ArgumentException("Code is required.");

            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("Name is required.");

            if (request.Price < 0)
                throw new ArgumentException("Price cannot be negative.");

            if (request.AvailableQuantity < 0)
                throw new ArgumentException(
                    "Available quantity cannot be negative.");

            var entity = new Accessory
            {
                Code = request.Code,
                Name = request.Name,
                Description = request.Description,
                Price = request.Price,
                Currency = request.Currency,
                AvailableQuantity = request.AvailableQuantity
            };

            await _repository.AddAsync(entity, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);

            return Map(entity);
        }

        public async Task<AccessoryResponse?> GetAsync(
            long accessoryId,
            CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetByIdAsync(
                accessoryId, cancellationToken);

            return entity == null ? null : Map(entity);
        }

        public async Task<List<AccessoryResponse>>
            GetCompatibleAsync(
                int itemId,
                CancellationToken cancellationToken = default)
        {
            var items = await _repository
                .GetCompatibleAccessoriesAsync(
                    itemId, cancellationToken);

            return items.Select(Map).ToList();
        }

        public async Task<bool> AddCompatibilityAsync(
            long accessoryId,
            AddCompatibilityRequest request,
            CancellationToken cancellationToken = default)
        {
            var accessory = await _repository.GetByIdAsync(
                accessoryId, cancellationToken);

            if (accessory == null)
                return false;

            if (request.ItemId <= 0)
                throw new ArgumentException("Invalid ItemId.");

            if (await _repository.CompatibilityExistsAsync(
                    accessoryId, request.ItemId,
                    cancellationToken))
                return true;

            var compatibility = new AccessoryCompatibility
            {
                AccessoryId = accessoryId,
                ItemId = request.ItemId,
                SKU = request.SKU
            };

            await _repository.AddCompatibilityAsync(
                compatibility, cancellationToken);

            await _repository.SaveChangesAsync(
                cancellationToken);

            await _eventPublisher.PublishAsync(
                "accessory.compatibility.changed",
                new AccessoryCompatibilityChangedEvent
                {
                    AccessoryId = accessoryId,
                    ItemId = request.ItemId,
                    SKU = request.SKU
                },
                cancellationToken);

            return true;
        }

        public async Task<bool> UpdateAvailabilityAsync(
            long accessoryId,
            UpdateAvailabilityRequest request,
            CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetByIdAsync(
                accessoryId, cancellationToken);

            if (entity == null)
                return false;

            if (request.AvailableQuantity < 0)
                throw new ArgumentException(
                    "Quantity cannot be negative.");

            entity.AvailableQuantity =
                request.AvailableQuantity;
            entity.Status = request.Status;
            entity.UpdatedAtUtc = DateTime.UtcNow;

            _repository.Update(entity);
            await _repository.SaveChangesAsync(cancellationToken);

            await _eventPublisher.PublishAsync(
                "accessory.availability.changed",
                new AccessoryAvailabilityChangedEvent
                {
                    AccessoryId = entity.Id,
                    Code = entity.Code,
                    AvailableQuantity =
                        entity.AvailableQuantity,
                    Status = entity.Status.ToString()
                },
                cancellationToken);

            return true;
        }

        public async Task<bool> UpdatePriceAsync(
            long accessoryId,
            UpdateAccessoryPriceRequest request,
            CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetByIdAsync(
                accessoryId, cancellationToken);

            if (entity == null)
                return false;

            if (request.Price < 0)
                throw new ArgumentException(
                    "Price cannot be negative.");

            entity.Price = request.Price;
            entity.UpdatedAtUtc = DateTime.UtcNow;

            _repository.Update(entity);
            await _repository.SaveChangesAsync(cancellationToken);

            await _eventPublisher.PublishAsync(
                "accessory.price.changed",
                new AccessoryPriceChangedEvent
                {
                    AccessoryId = entity.Id,
                    Code = entity.Code,
                    Price = entity.Price,
                    Currency = entity.Currency
                },
                cancellationToken);

            return true;
        }

        private static AccessoryResponse Map(
            Accessory entity)
        {
            return new AccessoryResponse
            {
                Id = entity.Id,
                Code = entity.Code,
                Name = entity.Name,
                Description = entity.Description,
                Price = entity.Price,
                Currency = entity.Currency,
                AvailableQuantity =
                    entity.AvailableQuantity,
                Status = entity.Status,
                CreatedAtUtc = entity.CreatedAtUtc,
                UpdatedAtUtc = entity.UpdatedAtUtc,
                Compatibilities =
                    entity.Compatibilities.Select(x =>
                        new AccessoryCompatibilityResponse
                        {
                            Id = x.Id,
                            AccessoryId = x.AccessoryId,
                            ItemId = x.ItemId,
                            SKU = x.SKU,
                            IsActive = x.IsActive
                        }).ToList()
            };
        }
    }

}
