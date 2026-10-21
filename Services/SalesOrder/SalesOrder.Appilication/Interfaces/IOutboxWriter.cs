using System.Threading;
using System.Threading.Tasks;

namespace SalesOrder.Application.Interfaces
{
    public interface IOutboxWriter
    {
        Task AddAsync(string eventType, string aggregateId, object payload, CancellationToken ct);
    }
}
