using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Payment.Infrastructure.Persistence;

namespace Payment.Infrastructure.Messaging
{
    public sealed class OutboxProcessor : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<OutboxProcessor> _logger;
        private IProducer<string, string>? _producer;

        public OutboxProcessor(
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration,
            ILogger<OutboxProcessor> logger)
        {
            _scopeFactory = scopeFactory;
            _configuration = configuration;
            _logger = logger;

            InitializeKafkaProducer();
        }

        private void InitializeKafkaProducer()
        {
            try
            {
                var bootstrapServers = _configuration["Kafka:BootstrapServers"];
                if (string.IsNullOrWhiteSpace(bootstrapServers))
                {
                    _logger.LogInformation("Kafka:BootstrapServers not configured. Outbox will operate in development log-only mode.");
                    _producer = null;
                    return;
                }

                var config = new ProducerConfig
                {
                    BootstrapServers = bootstrapServers,
                    EnableIdempotence = true,
                    MessageTimeoutMs = 3000,
                    Acks = Acks.All
                };

                _producer = new ProducerBuilder<string, string>(config).Build();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Kafka producer could not be initialized. Outbox will operate in development log-only mode.");
                _producer = null;
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("OutboxProcessor background service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessOutboxBatchAsync(stoppingToken);
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogError(ex, "Error occurred while processing outbox messages.");
                }

                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            }

            _logger.LogInformation("OutboxProcessor background service stopped.");
        }

        public async Task ProcessOutboxBatchAsync(CancellationToken ct = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();

            var pending = await db.OutboxMessages
                .Where(x => x.ProcessedAtUtc == null)
                .OrderBy(x => x.CreatedAtUtc)
                .Take(50)
                .ToListAsync(ct);

            if (pending.Count == 0)
                return;

            _logger.LogInformation("Found {Count} pending outbox messages to dispatch.", pending.Count);

            foreach (var message in pending)
            {
                // Format event envelope matching SalesOrder expectations
                var envelopeJson = JsonSerializer.Serialize(new
                {
                    eventId = message.EventId,
                    eventType = message.EventType,
                    aggregateId = message.AggregateId,
                    timestamp = message.CreatedAtUtc,
                    payload = JsonDocument.Parse(message.PayloadJson).RootElement
                });

                var published = false;

                if (_producer is not null)
                {
                    try
                    {
                        var kafkaMessage = new Message<string, string>
                        {
                            Key = message.AggregateId,
                            Value = envelopeJson
                        };

                        await _producer.ProduceAsync(message.Topic, kafkaMessage, ct);
                        published = true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to publish message {EventId} to Kafka topic {Topic}. Will retry next cycle.",
                            message.EventId, message.Topic);
                    }
                }
                else
                {
                    // Fallback for offline local development without running Kafka container
                    _logger.LogInformation("[DEV_MOCK_DISPATCH] Dispatched outbox message {EventId} of type '{EventType}' to topic '{Topic}'.",
                        message.EventId, message.EventType, message.Topic);
                    published = true;
                }

                if (published)
                {
                    message.ProcessedAtUtc = DateTime.UtcNow;
                }
            }

            await db.SaveChangesAsync(ct);
        }

        public override void Dispose()
        {
            _producer?.Dispose();
            base.Dispose();
        }
    }
}
