using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;

namespace SalesOrder.Infrastructure.Messaging
{
    public sealed class KafkaDeadLetterPublisher
    {
        private readonly IProducer<string, string> _producer;

        public KafkaDeadLetterPublisher(
            IProducer<string, string> producer)
        {
            _producer = producer;
        }

        public async Task PublishAsync(
            ConsumeResult<string, string> record,
            Exception error,
            CancellationToken ct)
        {
            var dlqTopic = $"{record.Topic}.dlq";

            var headers = new Headers();

            // Copy original headers without modifying the consumed message.
            if (record.Message.Headers is not null)
            {
                foreach (var header in record.Message.Headers)
                {
                    headers.Add(
                        header.Key,
                        header.GetValueBytes());
                }
            }

            headers.Add(
                "original-topic",
                Encoding.UTF8.GetBytes(record.Topic));

            headers.Add(
                "original-partition",
                Encoding.UTF8.GetBytes(
                    record.Partition.Value.ToString()));

            headers.Add(
                "original-offset",
                Encoding.UTF8.GetBytes(
                    record.Offset.Value.ToString()));

            headers.Add(
                "failure-type",
                Encoding.UTF8.GetBytes(
                    error.GetType().FullName ?? "Exception"));

            var failureMessage = error.Message.Length > 1000
                ? error.Message[..1000]
                : error.Message;

            headers.Add(
                "failure-message",
                Encoding.UTF8.GetBytes(failureMessage));

            await _producer.ProduceAsync(
                dlqTopic,
                new Message<string, string>
                {
                    Key = record.Message.Key,
                    Value = record.Message.Value,
                    Headers = headers
                },
                ct);
        }
    }
}
