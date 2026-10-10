using ItemsPrice.Application.Commands;
using ItemsPrice.Application.DTOs;
using ItemsPrice.Application.Queries;
using ItemsPrice.Domain.Entities;
using ItemsPrice.Domain.Enums;
using ItemsPrice.Infrastructure.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItemsPrice.Application.Services.NewFolder
{
    public class ItemPriceApplicationService
    {
        private readonly IItemPriceRepository _repository;
        private readonly IEventPublisher _eventPublisher;

        public ItemPriceApplicationService(
            IItemPriceRepository repository,
            IEventPublisher eventPublisher)
        {
            _repository = repository;
            _eventPublisher = eventPublisher;
        }

        public async Task<ItemPriceResponse> CreateAsync(
            CreatePriceCommand command,
            CancellationToken cancellationToken = default)
        {
            var request = command.Request;

            ValidateItemId(request.ItemId);
            if (string.IsNullOrWhiteSpace(request.SKU))
                throw new ArgumentException("SKU is required.");
            if (request.BasePrice < 0)
                throw new ArgumentException("BasePrice cannot be negative.");
            if (string.IsNullOrWhiteSpace(request.Currency))
                throw new ArgumentException("Currency is required.");
            ValidateDateRange(request.EffectiveFromUtc, request.EffectiveToUtc);

            var history = await _repository.GetPriceHistoryAsync(
                request.ItemId, cancellationToken);

            EnsureNoOverlappingPriceWindow(
                history, request.EffectiveFromUtc, request.EffectiveToUtc);

            var version = await _repository.GetNextVersionAsync(
                request.ItemId, cancellationToken);

            var price = new ItemPrice
            {
                ItemId = request.ItemId,
                SKU = request.SKU.Trim(),
                BasePrice = request.BasePrice,
                Currency = request.Currency.Trim().ToUpperInvariant(),
                EffectiveFromUtc = EnsureUtc(request.EffectiveFromUtc),
                EffectiveToUtc = request.EffectiveToUtc.HasValue
                    ? EnsureUtc(request.EffectiveToUtc.Value)
                    : null,
                Version = version,
                Status = PriceStatus.Active,
                CreatedAtUtc = DateTime.UtcNow
            };

            await _repository.AddAsync(price, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);

            await _eventPublisher.PublishAsync(
                "item.price.created",
                new
                {
                    price.Id,
                    price.ItemId,
                    price.SKU,
                    price.BasePrice,
                    price.Currency,
                    price.Version,
                    price.EffectiveFromUtc,
                    price.EffectiveToUtc
                },
                cancellationToken);

            return MapToResponse(price, 0m);
        }

        public async Task<ItemPriceResponse?> GetCurrentPriceAsync(
            int itemId,
            CancellationToken cancellationToken = default)
        {
            ValidateItemId(itemId);

            var price = await _repository.GetCurrentPriceAsync(
                itemId, DateTime.UtcNow, cancellationToken);

            if (price is null)
                return null;

            var totalDiscount = CalculateTotalDiscount(price, DateTime.UtcNow);
            return MapToResponse(price, totalDiscount);
        }

        public async Task<List<PriceHistoryResponse>> GetHistoryAsync(
            int itemId,
            CancellationToken cancellationToken = default)
        {
            ValidateItemId(itemId);

            var prices = await _repository.GetPriceHistoryAsync(
                itemId, cancellationToken);

            return prices
                .OrderByDescending(x => x.Version)
                .Select(x => new PriceHistoryResponse
                {
                    PriceId = x.Id,
                    BasePrice = x.BasePrice,
                    Currency = x.Currency,
                    Version = x.Version,
                    Status = x.Status,
                    EffectiveFromUtc = x.EffectiveFromUtc,
                    EffectiveToUtc = x.EffectiveToUtc,
                    CreatedAtUtc = x.CreatedAtUtc
                })
                .ToList();
        }

        public async Task<ItemPriceResponse?> UpdateAsync(
            UpdatePriceCommand command,
            CancellationToken cancellationToken = default)
        {
            var price = await _repository.GetByIdAsync(
                command.PriceId, cancellationToken);

            if (price is null)
                return null;

            var request = command.Request;
            if (request.BasePrice < 0)
                throw new ArgumentException("BasePrice cannot be negative.");

            ValidateDateRange(request.EffectiveFromUtc, request.EffectiveToUtc);

            var history = await _repository.GetPriceHistoryAsync(
                price.ItemId, cancellationToken);

            EnsureNoOverlappingPriceWindow(
                history.Where(x => x.Id != price.Id),
                request.EffectiveFromUtc,
                request.EffectiveToUtc);

            price.BasePrice = request.BasePrice;
            price.EffectiveFromUtc = EnsureUtc(request.EffectiveFromUtc);
            price.EffectiveToUtc = request.EffectiveToUtc.HasValue
                ? EnsureUtc(request.EffectiveToUtc.Value)
                : null;
            price.UpdatedAtUtc = DateTime.UtcNow;

            _repository.Update(price);
            await _repository.SaveChangesAsync(cancellationToken);

            await _eventPublisher.PublishAsync(
                "item.price.changed",
                new
                {
                    price.Id,
                    price.ItemId,
                    price.BasePrice,
                    price.Version,
                    price.EffectiveFromUtc,
                    price.EffectiveToUtc,
                    price.UpdatedAtUtc
                },
                cancellationToken);

            return MapToResponse(
                price, CalculateTotalDiscount(price, DateTime.UtcNow));
        }

        public async Task<ItemPriceResponse?> AddDiscountAsync(
            AddDiscountCommand command,
            CancellationToken cancellationToken = default)
        {
            var price = await _repository.GetByIdAsync(
                command.PriceId, cancellationToken);

            if (price is null)
                return null;

            var request = command.Request;
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("Discount name is required.");
            if (request.DiscountValue < 0)
                throw new ArgumentException("DiscountValue cannot be negative.");
            if (request.DiscountType == DiscountType.Percentage &&
                request.DiscountValue > 100)
                throw new ArgumentException("Percentage discount cannot exceed 100.");
            if (request.MaximumDiscountAmount < 0)
                throw new ArgumentException("MaximumDiscountAmount cannot be negative.");

            ValidateDateRange(request.EffectiveFromUtc, request.EffectiveToUtc);

            var discount = new PriceDiscount
            {
                ItemPriceId = price.Id,
                Name = request.Name.Trim(),
                DiscountType = request.DiscountType,
                DiscountValue = request.DiscountValue,
                MaximumDiscountAmount = request.MaximumDiscountAmount,
                EffectiveFromUtc = EnsureUtc(request.EffectiveFromUtc),
                EffectiveToUtc = request.EffectiveToUtc.HasValue
                    ? EnsureUtc(request.EffectiveToUtc.Value)
                    : null,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };

            await _repository.AddDiscountAsync(discount, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);

            await _eventPublisher.PublishAsync(
                "item.discount.changed",
                new
                {
                    DiscountId = discount.Id,
                    discount.ItemPriceId,
                    discount.Name,
                    discount.DiscountType,
                    discount.DiscountValue,
                    discount.EffectiveFromUtc,
                    discount.EffectiveToUtc
                },
                cancellationToken);

            // Reload current price so the response includes persisted discounts.
            var refreshed = await _repository.GetByIdAsync(
                price.Id, cancellationToken) ?? price;

            return MapToResponse(
                refreshed, CalculateTotalDiscount(refreshed, DateTime.UtcNow));
        }

        public async Task<bool> DeactivateAsync(
            DeactivatePriceCommand command,
            CancellationToken cancellationToken = default)
        {
            var price = await _repository.GetByIdAsync(
                command.PriceId, cancellationToken);

            if (price is null)
                return false;

            price.Status = PriceStatus.Inactive;
            price.UpdatedAtUtc = DateTime.UtcNow;

            _repository.Update(price);
            await _repository.SaveChangesAsync(cancellationToken);

            await _eventPublisher.PublishAsync(
                "item.price.changed",
                new
                {
                    price.Id,
                    price.ItemId,
                    price.Status,
                    price.UpdatedAtUtc
                },
                cancellationToken);

            return true;
        }

        public async Task<PagedResponse<ItemPriceResponse>> SearchAsync(
            SearchPricesQuery query,
            CancellationToken cancellationToken = default)
        {
            var request = query.Request;
            var page = Math.Max(request.Page, 1);
            var pageSize = Math.Clamp(request.PageSize, 1, 100);

            var (items, totalCount) = await _repository.SearchAsync(
                request.ItemId,
                string.IsNullOrWhiteSpace(request.SKU) ? null : request.SKU.Trim(),
                request.Status,
                page,
                pageSize,
                request.SortBy,
                request.Descending,
                cancellationToken);

            var now = DateTime.UtcNow;
            var responseItems = items
                .Select(x => MapToResponse(x, CalculateTotalDiscount(x, now)))
                .ToList();

            return new PagedResponse<ItemPriceResponse>
            {
                Items = responseItems,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }

        private static ItemPriceResponse MapToResponse(
            ItemPrice price,
            decimal totalDiscount)
        {
            var safeDiscount = Math.Clamp(totalDiscount, 0m, price.BasePrice);

            return new ItemPriceResponse
            {
                PriceId = price.Id,
                ItemId = price.ItemId,
                SKU = price.SKU,
                BasePrice = price.BasePrice,
                Currency = price.Currency,
                TotalDiscount = safeDiscount,
                FinalPrice = price.BasePrice - safeDiscount,
                EffectiveFromUtc = price.EffectiveFromUtc,
                EffectiveToUtc = price.EffectiveToUtc,
                Version = price.Version,
                Status = price.Status
            };
        }

        private static decimal CalculateTotalDiscount(
            ItemPrice price,
            DateTime utcNow)
        {
            decimal total = 0m;

            var discounts = price.Discounts.Where(d =>
                d.IsActive &&
                d.EffectiveFromUtc <= utcNow &&
                (!d.EffectiveToUtc.HasValue || d.EffectiveToUtc.Value > utcNow));

            foreach (var discount in discounts)
            {
                var amount = discount.DiscountType switch
                {
                    DiscountType.FixedAmount => discount.DiscountValue,
                    DiscountType.Percentage =>
                        price.BasePrice * discount.DiscountValue / 100m,
                    _ => 0m
                };

                if (discount.MaximumDiscountAmount.HasValue)
                    amount = Math.Min(amount, discount.MaximumDiscountAmount.Value);

                total += Math.Max(amount, 0m);
            }

            // Policy in this sample: discounts are additive and cannot exceed base price.
            return Math.Clamp(total, 0m, price.BasePrice);
        }

        private static void ValidateItemId(int itemId)
        {
            if (itemId <= 0)
                throw new ArgumentException("ItemId must be greater than zero.");
        }

        private static void ValidateDateRange(DateTime fromUtc, DateTime? toUtc)
        {
            if (toUtc.HasValue && toUtc.Value <= fromUtc)
                throw new ArgumentException(
                    "EffectiveToUtc must be later than EffectiveFromUtc.");
        }

        private static DateTime EnsureUtc(DateTime value)
        {
            // API contract expects UTC values. Unspecified values are treated as UTC.
            return value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };
        }

        private static void EnsureNoOverlappingPriceWindow(
            IEnumerable<ItemPrice> existingPrices,
            DateTime newFrom,
            DateTime? newTo)
        {
            var from = EnsureUtc(newFrom);
            var to = newTo.HasValue ? EnsureUtc(newTo.Value) : (DateTime?)null;

            var overlaps = existingPrices.Any(existing =>
            {
                var existingFrom = EnsureUtc(existing.EffectiveFromUtc);
                var existingTo = existing.EffectiveToUtc.HasValue
                    ? EnsureUtc(existing.EffectiveToUtc.Value)
                    : (DateTime?)null;

                var newEndsAfterExistingStarts =
                    !existingTo.HasValue || from < existingTo.Value;
                var existingEndsAfterNewStarts =
                    !to.HasValue || existingFrom < to.Value;

                return newEndsAfterExistingStarts && existingEndsAfterNewStarts;
            });

            if (overlaps)
                throw new InvalidOperationException(
                    "The effective date range overlaps an existing price version.");
        }
    }
}
