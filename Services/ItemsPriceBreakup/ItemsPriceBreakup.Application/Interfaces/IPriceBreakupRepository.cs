using ItemsPriceBreakup.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItemsPriceBreakup.Application.Interfaces
{
    public interface IPriceBreakupRepository
    {
        Task AddAsync(
            PriceBreakup entity,
            CancellationToken cancellationToken = default);

        Task<PriceBreakup?> GetByIdAsync(
            long id,
            CancellationToken cancellationToken = default);

        Task SaveChangesAsync(
            CancellationToken cancellationToken = default);
    }

}
