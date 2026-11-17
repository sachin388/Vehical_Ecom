using System.Threading;
using System.Threading.Tasks;

namespace Payment.Application.Interfaces
{
    public interface IOutboxWriter
    {
        Task AddAsync(string eventType, string aggregateId, object payload, CancellationToken ct = default);
    }
}

