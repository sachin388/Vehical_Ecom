using Items.Application.DTOs;
using Items.Application.Interfaces;
using Items.Domain.Entities;
using Items.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Items.Infrastructure.Repositories
{
    public sealed class ItemRepository : IItemRepository
    {
        private readonly ItemDbContext _context;

        public ItemRepository(ItemDbContext context)
        {
            _context = context;
        }

        public async Task<Item?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            return await _context.Items
                .Include(x => x.VehicleMake)
                .Include(x => x.VehicleModel)
                .Include(x => x.VehicleVariant)
                .Include(x => x.VehicleCategory)
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.Id == id,
                    cancellationToken);
        }

        public async Task<Item?> GetBySkuAsync(
            string sku,
            CancellationToken cancellationToken = default)
        {
            return await _context.Items
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.SKU == sku,
                    cancellationToken);
        }

        public async Task AddAsync(
            Item item,
            CancellationToken cancellationToken = default)
        {
            await _context.Items.AddAsync(item, cancellationToken);
        }

        public void Update(Item item)
        {
            _context.Items.Update(item);
        }

        public async Task<(IReadOnlyList<Item> Items, int TotalCount)> SearchAsync(
            ItemSearchRequest request,
            CancellationToken cancellationToken = default)
        {
            IQueryable<Item> query = _context.Items
                .AsNoTracking()
                .Include(x => x.VehicleMake)
                .Include(x => x.VehicleModel)
                .Include(x => x.VehicleVariant)
                .Include(x => x.VehicleCategory);

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var search = request.Search.Trim();

                query = query.Where(x =>
                    x.Name.Contains(search) ||
                    x.SKU.Contains(search));
            }

            if (request.MakeId.HasValue)
            {
                query = query.Where(x =>
                    x.VehicleMakeId == request.MakeId.Value);
            }

            if (request.ModelId.HasValue)
            {
                query = query.Where(x =>
                    x.VehicleModelId == request.ModelId.Value);
            }

            if (request.VariantId.HasValue)
            {
                query = query.Where(x =>
                    x.VehicleVariantId == request.VariantId.Value);
            }

            if (request.CategoryId.HasValue)
            {
                query = query.Where(x =>
                    x.VehicleCategoryId == request.CategoryId.Value);
            }

            if (request.Status.HasValue)
            {
                query = query.Where(x =>
                    x.Status == request.Status.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            query = request.SortBy.ToLowerInvariant() switch
            {
                "sku" => request.Descending
                    ? query.OrderByDescending(x => x.SKU)
                    : query.OrderBy(x => x.SKU),

                "created" => request.Descending
                    ? query.OrderByDescending(x => x.CreatedAtUtc)
                    : query.OrderBy(x => x.CreatedAtUtc),

                "updated" => request.Descending
                    ? query.OrderByDescending(x => x.UpdatedAtUtc)
                    : query.OrderBy(x => x.UpdatedAtUtc),

                _ => request.Descending
                    ? query.OrderByDescending(x => x.Name)
                    : query.OrderBy(x => x.Name)
            };

            var page = request.Page < 1 ? 1 : request.Page;

            var pageSize = request.PageSize switch
            {
                < 1 => 20,
                > 100 => 100,
                _ => request.PageSize
            };

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        public Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            return _context.SaveChangesAsync(cancellationToken);
        }

        //public Task<(IReadOnlyList<Item> Items, int TotalCount)> SearchAsync(ItemSearchRequest request, CancellationToken cancellationToken = default)
        //{
        //    throw new NotImplementedException();
        //}
    }

}
