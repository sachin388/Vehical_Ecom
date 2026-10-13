using Dealer.API.Controllers;
using Dealer.Application.DTOs;
using Dealer.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Dealer.Tests.Controllers
{
    public class DealerInventoryControllerTests
    {
        private readonly Mock<IDealerInventoryApplicationService> _serviceMock;
        private readonly DealerInventoryController _controller;

        public DealerInventoryControllerTests()
        {
            _serviceMock = new Mock<IDealerInventoryApplicationService>();
            _controller = new DealerInventoryController(_serviceMock.Object);
        }

        [Fact]
        public async Task SetInventory_NonPositiveDealerId_ReturnsBadRequest()
        {
            // Arrange
            var request = new SetDealerInventoryRequest { ItemId = 1, SKU = "CAR-1", Quantity = 10 };

            // Act
            var result = await _controller.SetInventory(0, request, CancellationToken.None);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        }

        [Fact]
        public async Task SetInventory_ValidRequest_ReturnsOkWithInventory()
        {
            // Arrange
            var request = new SetDealerInventoryRequest { ItemId = 1, SKU = "CAR-1", Quantity = 10 };
            var response = new DealerInventoryResponse
            {
                Id = 1,
                DealerId = 5,
                ItemId = 1,
                SKU = "CAR-1",
                Quantity = 10,
                AvailableQuantity = 10
            };

            _serviceMock
                .Setup(s => s.SetInventoryAsync(5, request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            // Act
            var result = await _controller.SetInventory(5, request, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            var item = Assert.IsType<DealerInventoryResponse>(okResult.Value);
            Assert.Equal(10, item.Quantity);
            Assert.Equal(5, item.DealerId);
        }

        [Fact]
        public async Task SetInventory_ArgumentException_ReturnsBadRequest()
        {
            // Arrange
            var request = new SetDealerInventoryRequest { ItemId = 1, SKU = "CAR-1", Quantity = -1 };

            _serviceMock
                .Setup(s => s.SetInventoryAsync(5, request, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ArgumentException("Quantity cannot be negative"));

            // Act
            var result = await _controller.SetInventory(5, request, CancellationToken.None);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        }

        [Fact]
        public async Task SetInventory_KeyNotFoundException_ReturnsNotFound()
        {
            // Arrange
            var request = new SetDealerInventoryRequest { ItemId = 1, SKU = "CAR-1", Quantity = 5 };

            _serviceMock
                .Setup(s => s.SetInventoryAsync(5, request, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new KeyNotFoundException("Dealer not found"));

            // Act
            var result = await _controller.SetInventory(5, request, CancellationToken.None);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
        }

        [Fact]
        public async Task SetInventory_InvalidOperationException_ReturnsConflict()
        {
            // Arrange
            var request = new SetDealerInventoryRequest { ItemId = 1, SKU = "CAR-1", Quantity = 2 };

            _serviceMock
                .Setup(s => s.SetInventoryAsync(5, request, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Quantity cannot be lower than reserved"));

            // Act
            var result = await _controller.SetInventory(5, request, CancellationToken.None);

            // Assert
            var conflictResult = Assert.IsType<ConflictObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status409Conflict, conflictResult.StatusCode);
        }

        [Fact]
        public async Task GetInventory_NonPositiveDealerId_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.GetInventory(-1, CancellationToken.None);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        }

        [Fact]
        public async Task GetInventory_Success_ReturnsOkWithList()
        {
            // Arrange
            var list = new List<DealerInventoryResponse>
            {
                new DealerInventoryResponse { Id = 1, DealerId = 5, ItemId = 1, SKU = "CAR-1", Quantity = 10 }
            };

            _serviceMock
                .Setup(s => s.GetInventoryAsync(5, It.IsAny<CancellationToken>()))
                .ReturnsAsync(list);

            // Act
            var result = await _controller.GetInventory(5, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            var response = Assert.IsAssignableFrom<IReadOnlyList<DealerInventoryResponse>>(okResult.Value);
            Assert.Single(response);
        }

        [Fact]
        public async Task GetInventory_KeyNotFoundException_ReturnsNotFound()
        {
            // Arrange
            _serviceMock
                .Setup(s => s.GetInventoryAsync(99, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new KeyNotFoundException("Dealer not found"));

            // Act
            var result = await _controller.GetInventory(99, CancellationToken.None);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
        }
    }
}
