using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dealer.Application.Interfaces
{
    public interface IDealerRepository
    {
        Task<Dealer?> GetByIdAsync(long id, CancellationToken ct = default);
        Task<bool> DealerCodeExistsAsync(string code, CancellationToken ct = default);
        Task AddAsync(Dealer dealer, CancellationToken ct = default);
        Task SaveChangesAsync(CancellationToken ct = default);
    }

}
