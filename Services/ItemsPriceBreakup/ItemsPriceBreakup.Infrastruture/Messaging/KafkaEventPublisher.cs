using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ItemsPriceBreakup.Infrastruture.Messaging
{
    public class KafkaEventPublisher : IEventPublisher, IDisposable
    {
        private readonly IProducer<string, string> _producer;

        public KafkaEventPublisher(IConfiguration configuration)
        {
            var config = new ProducerConfig
            {
                BootstrapServers = configuration["Kafka:BootstrapServers"],
                Acks = Acks.All,
                EnableIdempotence = true
            };

            _producer = new ProducerBuilder<string, string>(config).Build();
        }

        public async Task PublishAsync<TEvent>(
            string topic,
            TEvent message,
            CancellationToken cancellationToken = default)
        {
            var payload = JsonSerializer.Serialize(message);

            await _producer.ProduceAsync(
                topic,
                new Message<string, string>
                {
                    Key = Guid.NewGuid().ToString(),
                    Value = payload
                },
                cancellationToken);
        }

        public void Dispose()
        {
            _producer.Flush(TimeSpan.FromSeconds(5));
            _producer.Dispose();
        }
    }

}
