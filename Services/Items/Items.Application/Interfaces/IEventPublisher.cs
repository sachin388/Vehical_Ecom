using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Items.Application.Interfaces
{
    public interface IEventPublisher
    {
        Task PublishAsync<T>(

            string topic,
            T message,
            CancellationToken cancellationToken = default);
    }
}


