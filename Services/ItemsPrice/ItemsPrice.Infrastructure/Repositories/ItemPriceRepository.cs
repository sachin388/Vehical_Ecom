using ItemsPrice.Domain.Entities;
using ItemsPrice.Domain.Enums;
using ItemsPrice.Infrastructure.Interfaces;
using ItemsPrice.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ItemsPrice.Infrastructure.Repositories
{
    public class ItemPriceRepository : IItemPriceRepository
    {
        private readonly ItemPriceDbContext _context;

        public ItemPriceRepository(ItemPriceDbContext context)
        {
            _context = context;
        }

        public async Task<ItemPrice?> GetByIdAsync(
            long priceId,
            CancellationToken cancellationToken = default)
        {
            return await _context.ItemPrices
                .Include(x => x.Discounts)
                .FirstOrDefaultAsync(x => x.Id == priceId, cancellationToken);
        }

        public async Task<ItemPrice?> GetCurrentPriceAsync(
            int itemId,
            DateTime utcNow,
            CancellationToken cancellationToken = default)
        {
            return await _context.ItemPrices
                .Include(x => x.Discounts)
                .Where(x => x.ItemId == itemId
                    && x.Status == PriceStatus.Active
                    && x.EffectiveFromUtc <= utcNow
                    && (x.EffectiveToUtc == null || x.EffectiveToUtc >= utcNow))
                .OrderByDescending(x => x.EffectiveFromUtc)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<List<ItemPrice>> GetPriceHistoryAsync(
            int itemId,
            CancellationToken cancellationToken = default)
        {
            return await _context.ItemPrices
                .Include(x => x.Discounts)
                .Where(x => x.ItemId == itemId)
                .OrderByDescending(x => x.EffectiveFromUtc)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> GetNextVersionAsync(
            int itemId,
            CancellationToken cancellationToken = default)
        {
            var maxVersion = await _context.ItemPrices
                .Where(x => x.ItemId == itemId)
                .MaxAsync(x => (int?)x.Version, cancellationToken);

            return (maxVersion ?? 0) + 1;
        }

        public async Task AddAsync(
            ItemPrice price,
            CancellationToken cancellationToken = default)
        {
            await _context.ItemPrices.AddAsync(price, cancellationToken);
        }

        public void Update(ItemPrice price)
        {
            _context.ItemPrices.Update(price);
        }

        public async Task AddDiscountAsync(
            PriceDiscount discount,
            CancellationToken cancellationToken = default)
        {
            await _context.PriceDiscounts.AddAsync(discount, cancellationToken);
        }

        public async Task<(List<ItemPrice> Items, int TotalCount)> SearchAsync(
            int? itemId,
            string? sku,
            PriceStatus? status,
            int page,
            int pageSize,
            string sortBy,
            bool descending,
            CancellationToken cancellationToken = default)
        {
            var query = _context.ItemPrices
                .Include(x => x.Discounts)
                .AsQueryable();

            if (itemId.HasValue)
                query = query.Where(x => x.ItemId == itemId.Value);

            if (!string.IsNullOrWhiteSpace(sku))
                query = query.Where(x => x.SKU.Contains(sku));

            if (status.HasValue)
                query = query.Where(x => x.Status == status.Value);

            var totalCount = await query.CountAsync(cancellationToken);

            query = (sortBy?.ToLowerInvariant(), descending) switch
            {
                ("baseprice", true) => query.OrderByDescending(x => x.BasePrice),
                ("baseprice", false) => query.OrderBy(x => x.BasePrice),
                ("effectivefromutc", true) => query.OrderByDescending(x => x.EffectiveFromUtc),
                ("effectivefromutc", false) => query.OrderBy(x => x.EffectiveFromUtc),
                _ => descending ? query.OrderByDescending(x => x.Id) : query.OrderBy(x => x.Id)
            };

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        public async Task SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
