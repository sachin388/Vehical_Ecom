using Accessory.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Accessory.Infrastructure.Repositories
{
    public class AccessoryRepository : IAccessoryRepository
    {
        private readonly AccessoryDbContext _context;

        public AccessoryRepository(AccessoryDbContext context)
        {
            _context = context;
        }

        public async Task<Accessory?> GetByIdAsync(
            long accessoryId,
            CancellationToken cancellationToken = default)
        {
            return await _context.Accessories
                .Include(x => x.Compatibilities)
                .FirstOrDefaultAsync(x => x.Id == accessoryId, cancellationToken);
        }

        public async Task<List<Accessory>> GetCompatibleAccessoriesAsync(
            int itemId,
            CancellationToken cancellationToken = default)
        {
            return await _context.Accessories
                .Include(x => x.Compatibilities)
                .Where(x =>
                    x.Status == AccessoryStatus.Active &&
                    x.Compatibilities.Any(c =>
                        c.ItemId == itemId && c.IsActive))
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> CompatibilityExistsAsync(
            long accessoryId,
            int itemId,
            CancellationToken cancellationToken = default)
        {
            return await _context.AccessoryCompatibilities
                .AnyAsync(x =>
                    x.AccessoryId == accessoryId &&
                    x.ItemId == itemId,
                    cancellationToken);
        }

        public async Task AddAsync(
            Accessory accessory,
            CancellationToken cancellationToken = default)
        {
            await _context.Accessories.AddAsync(accessory, cancellationToken);
        }

        public async Task AddCompatibilityAsync(
            AccessoryCompatibility compatibility,
            CancellationToken cancellationToken = default)
        {
            await _context.AccessoryCompatibilities
                .AddAsync(compatibility, cancellationToken);
        }

        public void Update(Accessory accessory)
        {
            _context.Accessories.Update(accessory);
        }

        public Task SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            return _context.SaveChangesAsync(cancellationToken);
        }
    }

}
