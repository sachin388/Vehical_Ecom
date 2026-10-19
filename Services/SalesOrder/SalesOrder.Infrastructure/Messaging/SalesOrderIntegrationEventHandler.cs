using SalesOrder.Infrastructure.Messaging.IntegrationEvents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SalesOrder.Infrastructure.Messaging
{
    public sealed class SalesOrderIntegrationEventHandler : IIntegrationEventHandler
    {
        private readonly PaymentSucceededHandler _paymentSucceeded;
        private readonly DealerAllocatedHandler _dealerAllocated;
        private readonly PaymentCompensationCompletedHandler _paymentCompensation;
        private readonly DealerReleaseCompletedHandler _dealerRelease;

        public SalesOrderIntegrationEventHandler(
            PaymentSucceededHandler paymentSucceeded,
            DealerAllocatedHandler dealerAllocated,
            PaymentCompensationCompletedHandler paymentCompensation,
            DealerReleaseCompletedHandler dealerRelease)
        {
            _paymentSucceeded = paymentSucceeded;
            _dealerAllocated = dealerAllocated;
            _paymentCompensation = paymentCompensation;
            _dealerRelease = dealerRelease;
        }

        public async Task HandleAsync(
            string topic, string messageJson, CancellationToken ct)
        {
            using var json = JsonDocument.Parse(messageJson);
            var root = json.RootElement;
            var eventType = root.GetProperty("eventType").GetString();
            var eventId = root.GetProperty("eventId").GetGuid();
            var payload = root.GetProperty("payload");

            switch (eventType)
            {
                case "PaymentSucceeded":
                    await _paymentSucceeded.HandleAsync(
                        eventId, payload.Deserialize<PaymentSucceededPayload>()
                            ?? throw new JsonException("PaymentSucceeded payload is empty."), ct);
                    break;

                case "DealerAllocated":
                    await _dealerAllocated.HandleAsync(
                        eventId, payload.Deserialize<DealerAllocatedPayload>()
                            ?? throw new JsonException("DealerAllocated payload is empty."), ct);
                    break;

                case "PaymentCompensationCompleted":
                    await _paymentCompensation.HandleAsync(
                        eventId, payload.Deserialize<PaymentCompensationCompletedPayload>()
                            ?? throw new JsonException("Payment compensation payload is empty."), ct);
                    break;

                case "DealerReleaseCompleted":
                    await _dealerRelease.HandleAsync(
                        eventId, payload.Deserialize<DealerReleaseCompletedPayload>()
                            ?? throw new JsonException("Dealer release payload is empty."), ct);
                    break;

                default:
                    throw new NotSupportedException(
                        $"Unsupported event type '{eventType}' from topic '{topic}'.");
            }
        }
    }

}
