using ItemsPrice.Domain.Entities;
using ItemsPrice.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItemsPrice.Infrastructure.Interfaces
{
    public interface IItemPriceRepository
    {
        Task<ItemPrice?> GetByIdAsync(
            long priceId,
            CancellationToken cancellationToken = default);

        Task<ItemPrice?> GetCurrentPriceAsync(
            int itemId,
            DateTime utcNow,
            CancellationToken cancellationToken = default);

        Task<List<ItemPrice>> GetPriceHistoryAsync(
            int itemId,
            CancellationToken cancellationToken = default);

        Task<int> GetNextVersionAsync(
            int itemId,
            CancellationToken cancellationToken = default);

        Task AddAsync(
            ItemPrice price,
            CancellationToken cancellationToken = default);

        void Update(ItemPrice price);

        Task AddDiscountAsync(
            PriceDiscount discount,
            CancellationToken cancellationToken = default);

        Task<(List<ItemPrice> Items, int TotalCount)> SearchAsync(
            int? itemId,
            string? sku,
            PriceStatus? status,
            int page,
            int pageSize,
            string sortBy,
            bool descending,
            CancellationToken cancellationToken = default);

        Task SaveChangesAsync(
            CancellationToken cancellationToken = default);
        Task<(IEnumerable<ItemPrice> items, int totalCount)> SearchAsync(int? itemId, string v, PriceStatus? status, int page, int pageSize, string sortBy, bool descending, CancellationToken cancellationToken);
    }

}
