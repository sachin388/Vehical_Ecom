using Items.Application.DTOs;
using Items.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Items.Application.Interfaces
{
    public interface IItemRepository
    {
        Task<Item?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default);

        Task<Item?> GetBySkuAsync(
            string sku,
            CancellationToken cancellationToken = default);

        Task AddAsync(
            Item item,
            CancellationToken cancellationToken = default);

        void Update(Item item);

        Task<(IReadOnlyList<Item> Items, int TotalCount)> SearchAsync(
            ItemSearchRequest request,
            CancellationToken cancellationToken = default);

        Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default);
    }

}
