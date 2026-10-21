using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using SalesOrder.Application.DTOs;
using SalesOrder.Application.Interfaces;
using SalesOrder.Application.Services;
using SalesOrder.Domain.Entities;
using SalesOrder.Domain.Enums;
using Xunit;

namespace SalesOrder.Tests
{
    using SalesOrder = SalesOrder.Domain.Entities.SalesOrder;

    public class SalesOrderApplicationServiceTests
    {
        private readonly Mock<ISalesOrderRepository> _ordersRepoMock;
        private readonly Mock<IPricingClient> _pricingClientMock;
        private readonly Mock<IOutboxWriter> _outboxMock;
        private readonly SalesOrderApplicationService _service;

        public SalesOrderApplicationServiceTests()
        {
            _ordersRepoMock = new Mock<ISalesOrderRepository>();
            _pricingClientMock = new Mock<IPricingClient>();
            _outboxMock = new Mock<IOutboxWriter>();
            _service = new SalesOrderApplicationService(
                _ordersRepoMock.Object,
                _pricingClientMock.Object,
                _outboxMock.Object);
        }

        [Fact]
        public async Task CreateAsync_ValidRequest_CalculatesTotalsAndSavesOrder()
        {
            var customerId = "cust-123";
            var request = new CreateSalesOrderRequest
            {
                Currency = "INR",
                Lines = new List<CreateSalesOrderLineRequest>
                {
                    new() { ItemId = 10, Quantity = 2 }
                }
            };

            var prices = new List<PricedItemSnapshot>
            {
                new(10, "VEH-10", "Car Model X", 50000m, 5000m, 0.18m, "INR", new List<PricedAccessorySnapshot>())
            };

            _pricingClientMock.Setup(p => p.GetPriceSnapshotsAsync(
                It.IsAny<IReadOnlyList<CreateSalesOrderLineRequest>>(),
                "INR",
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(prices);

            var result = await _service.CreateAsync(customerId, request, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("cust-123", result.CustomerId);
            Assert.Equal("INR", result.Currency);
            Assert.Single(result.Lines);
            // Gross = 50000 * 2 = 100000, Discount = 5000 * 2 = 10000, Taxable = 90000, Tax = 16200, Total = 106200
            Assert.Equal(106200m, result.GrandTotal);

            _ordersRepoMock.Verify(o => o.AddAsync(It.IsAny<SalesOrder>(), It.IsAny<CancellationToken>()), Times.Once);
            _outboxMock.Verify(o => o.AddAsync("SalesOrderCreated", It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
            _ordersRepoMock.Verify(o => o.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_NoLines_ThrowsArgumentException()
        {
            var request = new CreateSalesOrderRequest
            {
                Currency = "INR",
                Lines = new List<CreateSalesOrderLineRequest>()
            };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.CreateAsync("cust-1", request, CancellationToken.None));
        }

        [Fact]
        public async Task GetAsync_ExistingOrder_ReturnsMappedResponse()
        {
            var orderId = Guid.NewGuid();
            var order = new SalesOrder
            {
                PublicId = orderId,
                OrderNumber = "SO-2026-001",
                CustomerId = "cust-1",
                GrandTotalSnapshot = 50000m,
                Currency = "INR"
            };

            _ordersRepoMock.Setup(o => o.GetByPublicIdAsync(orderId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);

            var result = await _service.GetAsync(orderId, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(orderId, result.Id);
            Assert.Equal("SO-2026-001", result.OrderNumber);
        }

        [Fact]
        public async Task CancelAsync_ValidOrder_TransitionsStatusAndWritesOutbox()
        {
            var orderId = Guid.NewGuid();
            var order = new SalesOrder
            {
                PublicId = orderId,
                OrderNumber = "SO-2026-001",
                CustomerId = "cust-1",
                Currency = "INR"
            };
            // Status is Draft

            _ordersRepoMock.Setup(o => o.GetByPublicIdAsync(orderId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);

            await _service.CancelAsync(orderId, "cust-1", "Changed my mind", CancellationToken.None);

            Assert.Equal(SalesOrderStatus.Cancelled, order.Status);
            _outboxMock.Verify(o => o.AddAsync("SalesOrderCancelled", orderId.ToString(), It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
            _ordersRepoMock.Verify(o => o.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CancelAsync_CustomerMismatch_ThrowsUnauthorizedAccessException()
        {
            var orderId = Guid.NewGuid();
            var order = new SalesOrder
            {
                PublicId = orderId,
                CustomerId = "cust-1"
            };

            _ordersRepoMock.Setup(o => o.GetByPublicIdAsync(orderId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.CancelAsync(orderId, "other-customer", "Some reason", CancellationToken.None));
        }
    }
}
