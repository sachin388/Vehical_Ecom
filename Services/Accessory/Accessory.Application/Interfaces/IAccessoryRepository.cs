using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Accessory.Domain.Entities;
using Accessory.Domain.Enums;

namespace Accessory.Application.Interfaces
{
    using Accessory = Accessory.Domain.Entities.Accessory;

    public interface IAccessoryRepository
    {
        Task<Accessory?> GetByIdAsync(
            long accessoryId,
            CancellationToken cancellationToken = default);

        Task<List<Accessory>> GetCompatibleAccessoriesAsync(
            int itemId,
            CancellationToken cancellationToken = default);

        Task<bool> CompatibilityExistsAsync(
            long accessoryId,
            int itemId,
            CancellationToken cancellationToken = default);

        Task AddAsync(
            Accessory accessory,
            CancellationToken cancellationToken = default);

        Task AddCompatibilityAsync(
            AccessoryCompatibility compatibility,
            CancellationToken cancellationToken = default);

        void Update(Accessory accessory);

        Task SaveChangesAsync(
            CancellationToken cancellationToken = default);
    }
}
