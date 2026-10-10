using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItemsPriceBreakup.Application.Interfaces
{
    public interface IEventPublisher
    {
        Task PublishAsync<TEvent>(
            string topic,
            TEvent message,
            CancellationToken cancellationToken = default);
    }

}
