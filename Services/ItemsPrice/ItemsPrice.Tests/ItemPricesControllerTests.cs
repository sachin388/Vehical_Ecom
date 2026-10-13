using ItemsPrice.API.Controllers;
using ItemsPrice.Application.DTOs;
using ItemsPrice.Application.Services.NewFolder;
using ItemsPrice.Domain.Entities;
using ItemsPrice.Domain.Enums;
using ItemsPrice.Infrastructure.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace ItemsPrice.Tests.Controllers
{
    public class ItemPricesControllerTests
    {
        private readonly Mock<IItemPriceRepository> _repoMock;
        private readonly Mock<IEventPublisher> _eventsMock;
        private readonly ItemPriceApplicationService _service;
        private readonly ItemPricesController _controller;

        public ItemPricesControllerTests()
        {
            _repoMock = new Mock<IItemPriceRepository>();
            _eventsMock = new Mock<IEventPublisher>();
            _service = new ItemPriceApplicationService(_repoMock.Object, _eventsMock.Object);
            _controller = new ItemPricesController(_service);
        }

        [Fact]
        public async Task GetCurrentPrice_PriceFound_ReturnsOk()
        {
            // Arrange
            var price = new ItemPrice
            {
                Id = 1,
                ItemId = 10,
                SKU = "BIKE-10",
                BasePrice = 50000m,
                Currency = "INR",
                Version = 1,
                Status = PriceStatus.Active,
                EffectiveFromUtc = DateTime.UtcNow.AddDays(-1),
                CreatedAtUtc = DateTime.UtcNow
            };

            _repoMock
                .Setup(r => r.GetCurrentPriceAsync(10, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(price);

            // Act
            var result = await _controller.GetCurrentPrice(10, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            var response = Assert.IsType<ItemPriceResponse>(okResult.Value);
            Assert.Equal(50000m, response.BasePrice);
            Assert.Equal(1, response.PriceId);
        }

        [Fact]
        public async Task GetCurrentPrice_PriceNotFound_ReturnsNotFound()
        {
            // Arrange
            _repoMock
                .Setup(r => r.GetCurrentPriceAsync(99, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ItemPrice?)null);

            // Act
            var result = await _controller.GetCurrentPrice(99, CancellationToken.None);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundResult>(result);
            Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
        }

        [Fact]
        public async Task GetHistory_ValidItemId_ReturnsOkWithHistoryList()
        {
            // Arrange
            var list = new List<ItemPrice>
            {
                new ItemPrice { Id = 1, ItemId = 10, BasePrice = 45000m, Version = 1, Currency = "INR", Status = PriceStatus.Active, EffectiveFromUtc = DateTime.UtcNow, CreatedAtUtc = DateTime.UtcNow },
                new ItemPrice { Id = 2, ItemId = 10, BasePrice = 50000m, Version = 2, Currency = "INR", Status = PriceStatus.Active, EffectiveFromUtc = DateTime.UtcNow, CreatedAtUtc = DateTime.UtcNow }
            };

            _repoMock
                .Setup(r => r.GetPriceHistoryAsync(10, It.IsAny<CancellationToken>()))
                .ReturnsAsync(list);

            // Act
            var result = await _controller.GetHistory(10, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            var history = Assert.IsType<List<PriceHistoryResponse>>(okResult.Value);
            Assert.Equal(2, history.Count);
        }

        [Fact]
        public async Task Create_ValidRequest_ReturnsCreatedResult()
        {
            // Arrange
            var request = new CreatePriceRequest
            {
                ItemId = 10,
                SKU = "BIKE-10",
                BasePrice = 60000m,
                Currency = "INR",
                EffectiveFromUtc = DateTime.UtcNow
            };

            _repoMock
                .Setup(r => r.GetPriceHistoryAsync(10, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ItemPrice>());

            _repoMock
                .Setup(r => r.GetNextVersionAsync(10, It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            _repoMock
                .Setup(r => r.AddAsync(It.IsAny<ItemPrice>(), It.IsAny<CancellationToken>()))
                .Callback<ItemPrice, CancellationToken>((p, ct) => p.Id = 100)
                .Returns(Task.CompletedTask);

            _repoMock
                .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Create(request, CancellationToken.None);

            // Assert
            var createdResult = Assert.IsType<CreatedResult>(result);
            Assert.Equal(StatusCodes.Status201Created, createdResult.StatusCode);
            var response = Assert.IsType<ItemPriceResponse>(createdResult.Value);
            Assert.Equal(100, response.PriceId);
            Assert.Equal("/api/item-prices/100", createdResult.Location);
        }

        [Fact]
        public async Task Update_PriceFound_ReturnsOk()
        {
            // Arrange
            var existing = new ItemPrice
            {
                Id = 100,
                ItemId = 10,
                SKU = "BIKE-10",
                BasePrice = 50000m,
                Currency = "INR",
                Version = 1,
                EffectiveFromUtc = DateTime.UtcNow.AddDays(-1)
            };

            var updateRequest = new UpdatePriceRequest
            {
                BasePrice = 55000m,
                EffectiveFromUtc = DateTime.UtcNow
            };

            _repoMock
                .Setup(r => r.GetByIdAsync(100, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existing);

            _repoMock
                .Setup(r => r.GetPriceHistoryAsync(10, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ItemPrice> { existing });

            _repoMock
                .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Update(100, updateRequest, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            var response = Assert.IsType<ItemPriceResponse>(okResult.Value);
            Assert.Equal(55000m, response.BasePrice);
        }

        [Fact]
        public async Task Update_PriceNotFound_ReturnsNotFound()
        {
            // Arrange
            _repoMock
                .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
                .ReturnsAsync((ItemPrice?)null);

            var updateRequest = new UpdatePriceRequest
            {
                BasePrice = 55000m,
                EffectiveFromUtc = DateTime.UtcNow
            };

            // Act
            var result = await _controller.Update(999, updateRequest, CancellationToken.None);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundResult>(result);
            Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
        }

        [Fact]
        public async Task AddDiscount_PriceFound_ReturnsOkWithDiscountedPrice()
        {
            // Arrange
            var existing = new ItemPrice
            {
                Id = 100,
                ItemId = 10,
                SKU = "BIKE-10",
                BasePrice = 50000m,
                Currency = "INR",
                EffectiveFromUtc = DateTime.UtcNow.AddDays(-1)
            };

            var discountRequest = new DiscountRequest
            {
                Name = "Festive Offer",
                DiscountType = DiscountType.FixedAmount,
                DiscountValue = 5000m,
                EffectiveFromUtc = DateTime.UtcNow.AddMinutes(-5)
            };

            _repoMock
                .Setup(r => r.GetByIdAsync(100, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existing);

            _repoMock
                .Setup(r => r.AddDiscountAsync(It.IsAny<PriceDiscount>(), It.IsAny<CancellationToken>()))
                .Callback<PriceDiscount, CancellationToken>((d, ct) => existing.Discounts.Add(d))
                .Returns(Task.CompletedTask);

            _repoMock
                .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.AddDiscount(100, discountRequest, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            var response = Assert.IsType<ItemPriceResponse>(okResult.Value);
            Assert.Equal(45000m, response.FinalPrice);
        }

        [Fact]
        public async Task AddDiscount_PriceNotFound_ReturnsNotFound()
        {
            // Arrange
            _repoMock
                .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
                .ReturnsAsync((ItemPrice?)null);

            var discountRequest = new DiscountRequest
            {
                Name = "Offer",
                DiscountType = DiscountType.FixedAmount,
                DiscountValue = 100m,
                EffectiveFromUtc = DateTime.UtcNow
            };

            // Act
            var result = await _controller.AddDiscount(999, discountRequest, CancellationToken.None);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundResult>(result);
            Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
        }

        [Fact]
        public async Task Deactivate_PriceFound_ReturnsNoContent()
        {
            // Arrange
            var existing = new ItemPrice
            {
                Id = 100,
                ItemId = 10,
                BasePrice = 50000m,
                Status = PriceStatus.Active
            };

            _repoMock
                .Setup(r => r.GetByIdAsync(100, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existing);

            _repoMock
                .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Deactivate(100, CancellationToken.None);

            // Assert
            var noContentResult = Assert.IsType<NoContentResult>(result);
            Assert.Equal(StatusCodes.Status204NoContent, noContentResult.StatusCode);
            Assert.Equal(PriceStatus.Inactive, existing.Status);
        }

        [Fact]
        public async Task Deactivate_PriceNotFound_ReturnsNotFound()
        {
            // Arrange
            _repoMock
                .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
                .ReturnsAsync((ItemPrice?)null);

            // Act
            var result = await _controller.Deactivate(999, CancellationToken.None);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundResult>(result);
            Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
        }
    }
}
