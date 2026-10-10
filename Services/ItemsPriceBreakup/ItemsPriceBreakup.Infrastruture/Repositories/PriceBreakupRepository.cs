using ItemsPriceBreakup.Application.Interfaces;
using ItemsPriceBreakup.Domain.Entities;
using ItemsPriceBreakup.Infrastruture.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItemsPriceBreakup.Infrastruture.Repositories
{
    public class PriceBreakupRepository : IPriceBreakupRepository
    {
        private readonly PriceBreakupDbContext _context;

        public PriceBreakupRepository(PriceBreakupDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(
            PriceBreakup entity,
            CancellationToken cancellationToken = default)
        {
            await _context.PriceBreakups.AddAsync(entity, cancellationToken);
        }

        public async Task<PriceBreakup?> GetByIdAsync(
            long id,
            CancellationToken cancellationToken = default)
        {
            return await _context.PriceBreakups
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public Task SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            return _context.SaveChangesAsync(cancellationToken);
        }
    }

}
