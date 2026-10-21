using System.Threading;
using System.Threading.Tasks;

namespace SalesOrder.Infrastructure.Messaging
{
    public interface IIntegrationEventHandler
    {
        Task HandleAsync(string topic, string messageJson, CancellationToken ct);
    }
}
