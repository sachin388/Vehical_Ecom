using Confluent.Kafka;
using Items.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Items.Infrastructure.Messaging
{
    public sealed class KafkaEventPublisher : IEventPublisher
    {
        private readonly IProducer<string, string> _producer;

        public KafkaEventPublisher(IConfiguration configuration)
        {
            var bootstrapServers =
                configuration["Kafka:BootstrapServers"];

            if (string.IsNullOrWhiteSpace(bootstrapServers))
            {
                throw new InvalidOperationException(
                    "Kafka BootstrapServers is not configured.");
            }

            var config = new ProducerConfig
            {
                BootstrapServers = bootstrapServers,
                Acks = Acks.All,
                EnableIdempotence = true
            };

            _producer = new ProducerBuilder<string, string>(config)
                .Build();
        }

        public async Task PublishAsync<T>(
            string topic,
            T message,
            CancellationToken cancellationToken = default)
        {
            var json = JsonSerializer.Serialize(message);

            var kafkaMessage = new Message<string, string>
            {
                Key = Guid.NewGuid().ToString(),
                Value = json
            };

            await _producer.ProduceAsync(
                topic,
                kafkaMessage,
                cancellationToken);
        }
    }

}
