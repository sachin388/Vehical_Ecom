using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Payment.Domain.Entities;
using Payment.Infrastructure.Messaging;
using Payment.Infrastructure.Persistence;
using Xunit;

namespace Payment.Tests
{
    public class OutboxProcessorTests
    {
        [Fact]
        public async Task ProcessOutboxBatchAsync_DispatchesPendingMessages_AndMarksThemAsProcessed()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<PaymentDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var db = new PaymentDbContext(options);

            var pendingMessage = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                EventId = Guid.NewGuid(),
                AggregateId = Guid.NewGuid().ToString(),
                EventType = "PaymentSucceeded",
                Topic = "payment.events",
                PayloadJson = JsonSerializer.Serialize(new { SalesOrderId = Guid.NewGuid(), Amount = 5000m, Currency = "INR" }),
                CreatedAtUtc = DateTime.UtcNow,
                ProcessedAtUtc = null
            };

            await db.OutboxMessages.AddAsync(pendingMessage);
            await db.SaveChangesAsync();

            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock
                .Setup(sp => sp.GetService(typeof(PaymentDbContext)))
                .Returns(new PaymentDbContext(options));

            var scopeMock = new Mock<IServiceScope>();
            scopeMock.Setup(s => s.ServiceProvider).Returns(serviceProviderMock.Object);

            var scopeFactoryMock = new Mock<IServiceScopeFactory>();
            scopeFactoryMock.Setup(f => f.CreateScope()).Returns(scopeMock.Object);

            var configMock = new ConfigurationBuilder().Build();
            var loggerMock = new Mock<ILogger<OutboxProcessor>>();

            var processor = new OutboxProcessor(scopeFactoryMock.Object, configMock, loggerMock.Object);

            // Act
            await processor.ProcessOutboxBatchAsync(CancellationToken.None);

            // Assert
            using var verifyDb = new PaymentDbContext(options);
            var updatedMessage = await verifyDb.OutboxMessages.SingleAsync(m => m.Id == pendingMessage.Id);
            Assert.NotNull(updatedMessage.ProcessedAtUtc);
        }
    }
}
