using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesOrder.Infrastructure.Interfaces
{
    public interface IOutboxWriter
    {
        Task AddAsync(string eventType, string aggregateId, object payload, CancellationToken ct);
    }

}
