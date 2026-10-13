using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Accessory.Application.Interfaces
{
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
