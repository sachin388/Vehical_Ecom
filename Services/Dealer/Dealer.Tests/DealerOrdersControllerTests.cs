using Dealer.API.Controllers;
using Dealer.Application.DTOs;
using Dealer.Application.Interfaces;
using Dealer.Application.Services;
using Dealer.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Dealer.Tests.Controllers
{
    public class DealerOrdersControllerTests
    {
        private readonly Mock<IDealerRepository> _dealerRepoMock;
        private readonly Mock<IDealerInventoryRepository> _inventoryRepoMock;
        private readonly Mock<IDealerSelectionStrategy> _strategyMock;
        private readonly Mock<IEventPublisher> _eventPublisherMock;
        private readonly DealerApplicationService _service;
        private readonly DealerOrdersController _controller;

        public DealerOrdersControllerTests()
        {
            _dealerRepoMock = new Mock<IDealerRepository>();
            _inventoryRepoMock = new Mock<IDealerInventoryRepository>();
            _strategyMock = new Mock<IDealerSelectionStrategy>();
            _eventPublisherMock = new Mock<IEventPublisher>();

            _service = new DealerApplicationService(
                _dealerRepoMock.Object,
                _inventoryRepoMock.Object,
                _strategyMock.Object,
                _eventPublisherMock.Object);

            _controller = new DealerOrdersController(_service);
        }

        [Fact]
        public async Task Assign_SuccessfulAllocation_ReturnsOkWithAssignment()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var request = new AssignDealerRequest
            {
                SalesOrderId = orderId,
                ItemId = 10,
                SKU = "CAR-10",
                Quantity = 1,
                City = "Mumbai"
            };

            _inventoryRepoMock
                .Setup(r => r.GetAssignmentByOrderIdAsync(orderId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((DealerOrderAssignment?)null);

            var candidates = new List<DealerAvailabilityCandidate>
            {
                new DealerAvailabilityCandidate
                {
                    DealerId = 1,
                    DealerName = "Mumbai Motors",
                    DealerBranchId = 101,
                    AvailableQuantity = 3
                }
            };

            _inventoryRepoMock
                .Setup(r => r.GetCandidatesAsync(10, "CAR-10", 1, "Mumbai", null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(candidates);

            _inventoryRepoMock
                .Setup(r => r.TryReserveAsync(1, 10, 1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _inventoryRepoMock
                .Setup(r => r.AddAssignmentAsync(It.IsAny<DealerOrderAssignment>(), It.IsAny<CancellationToken>()))
                .Callback<DealerOrderAssignment, CancellationToken>((a, ct) => a.Id = 55)
                .Returns(Task.CompletedTask);

            _inventoryRepoMock
                .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Assign(request, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            var response = Assert.IsType<DealerAssignmentResponse>(okResult.Value);
            Assert.Equal(55, response.AssignmentId);
            Assert.Equal(orderId, response.SalesOrderId);
            Assert.Equal(1, response.DealerId);
        }

        [Fact]
        public async Task Assign_NoEligibleDealer_ReturnsConflict()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var request = new AssignDealerRequest
            {
                SalesOrderId = orderId,
                ItemId = 10,
                SKU = "CAR-10",
                Quantity = 1
            };

            _inventoryRepoMock
                .Setup(r => r.GetAssignmentByOrderIdAsync(orderId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((DealerOrderAssignment?)null);

            _inventoryRepoMock
                .Setup(r => r.GetCandidatesAsync(10, "CAR-10", 1, null, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<DealerAvailabilityCandidate>());

            // Act
            var result = await _controller.Assign(request, CancellationToken.None);

            // Assert
            var conflictResult = Assert.IsType<ConflictObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status409Conflict, conflictResult.StatusCode);
        }

        [Fact]
        public async Task Assign_InvalidRequest_ReturnsBadRequest()
        {
            // Arrange
            var request = new AssignDealerRequest
            {
                SalesOrderId = Guid.Empty // Invalid
            };

            // Act
            var result = await _controller.Assign(request, CancellationToken.None);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        }

        [Fact]
        public async Task GetBySalesOrder_EmptyGuid_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.GetBySalesOrder(Guid.Empty, CancellationToken.None);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        }

        [Fact]
        public async Task GetBySalesOrder_NotFound_ReturnsNotFound()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            _inventoryRepoMock
                .Setup(r => r.GetAssignmentByOrderIdAsync(orderId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((DealerOrderAssignment?)null);

            // Act
            var result = await _controller.GetBySalesOrder(orderId, CancellationToken.None);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundResult>(result.Result);
            Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
        }

        [Fact]
        public async Task GetBySalesOrder_Found_ReturnsOkWithAssignment()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var assignment = new DealerOrderAssignment
            {
                Id = 1,
                SalesOrderId = orderId,
                DealerId = 10,
                SKU = "CAR-01",
                Quantity = 1,
                Status = "Assigned",
                AssignedAtUtc = DateTime.UtcNow
            };

            _inventoryRepoMock
                .Setup(r => r.GetAssignmentByOrderIdAsync(orderId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(assignment);

            // Act
            var result = await _controller.GetBySalesOrder(orderId, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            var response = Assert.IsType<DealerAssignmentResponse>(okResult.Value);
            Assert.Equal(orderId, response.SalesOrderId);
        }

        [Fact]
        public async Task UpdateStatus_EmptyGuid_ReturnsBadRequest()
        {
            // Arrange
            var request = new UpdateDealerAssignmentStatusRequest { Status = "Completed" };

            // Act
            var result = await _controller.UpdateStatus(Guid.Empty, request, CancellationToken.None);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        }

        [Fact]
        public async Task UpdateStatus_MissingStatus_ReturnsBadRequest()
        {
            // Arrange
            var request = new UpdateDealerAssignmentStatusRequest { Status = "" };

            // Act
            var result = await _controller.UpdateStatus(Guid.NewGuid(), request, CancellationToken.None);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        }

        [Fact]
        public async Task UpdateStatus_NotFound_ReturnsNotFound()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var request = new UpdateDealerAssignmentStatusRequest { Status = "Delivered" };

            _inventoryRepoMock
                .Setup(r => r.GetAssignmentByOrderIdAsync(orderId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((DealerOrderAssignment?)null);

            // Act
            var result = await _controller.UpdateStatus(orderId, request, CancellationToken.None);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundResult>(result.Result);
            Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
        }

        [Fact]
        public async Task UpdateStatus_Success_ReturnsOk()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var assignment = new DealerOrderAssignment
            {
                Id = 1,
                SalesOrderId = orderId,
                DealerId = 10,
                SKU = "CAR-01",
                Quantity = 1,
                Status = "Assigned",
                AssignedAtUtc = DateTime.UtcNow
            };
            var request = new UpdateDealerAssignmentStatusRequest { Status = "Delivered" };

            _inventoryRepoMock
                .Setup(r => r.GetAssignmentByOrderIdAsync(orderId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(assignment);

            _inventoryRepoMock
                .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.UpdateStatus(orderId, request, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            var response = Assert.IsType<DealerAssignmentResponse>(okResult.Value);
            Assert.Equal("Delivered", response.Status);
        }
    }
}
