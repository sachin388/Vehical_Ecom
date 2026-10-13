using Dealer.API.Controllers;
using Dealer.Application.DTOs;
using Dealer.Application.Interfaces;
using Dealer.Application.Services;
using Dealer.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using EntityDealer = Dealer.Domain.Entities.Dealer;

namespace Dealer.Tests.Controllers
{
    public class DealersControllerTests
    {
        private readonly Mock<IDealerRepository> _dealerRepoMock;
        private readonly Mock<IDealerInventoryRepository> _inventoryRepoMock;
        private readonly Mock<IDealerSelectionStrategy> _strategyMock;
        private readonly Mock<IEventPublisher> _eventPublisherMock;
        private readonly DealerApplicationService _service;
        private readonly DealersController _controller;

        public DealersControllerTests()
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

            _controller = new DealersController(_service);
        }

        [Fact]
        public async Task Create_ValidRequest_ReturnsCreatedAtAction()
        {
            // Arrange
            var request = new CreateDealerRequest
            {
                DealerCode = "DLR001",
                Name = "Apex Auto",
                Email = "dealer@apex.com",
                Phone = "1234567890"
            };

            _dealerRepoMock
                .Setup(r => r.DealerCodeExistsAsync("DLR001", It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            _dealerRepoMock
                .Setup(r => r.AddAsync(It.IsAny<EntityDealer>(), It.IsAny<CancellationToken>()))
                .Callback<EntityDealer, CancellationToken>((d, ct) => d.Id = 10)
                .Returns(Task.CompletedTask);

            _dealerRepoMock
                .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Create(request, CancellationToken.None);

            // Assert
            var createdAtResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal(StatusCodes.Status201Created, createdAtResult.StatusCode);
            var response = Assert.IsType<DealerResponse>(createdAtResult.Value);
            Assert.Equal("DLR001", response.DealerCode);
            Assert.Equal(10, response.Id);
        }

        [Fact]
        public async Task Create_MissingDealerCode_ReturnsBadRequest()
        {
            // Arrange
            var request = new CreateDealerRequest
            {
                DealerCode = "",
                Name = "Apex Auto"
            };

            // Act
            var result = await _controller.Create(request, CancellationToken.None);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
            var problem = Assert.IsType<ProblemDetails>(badRequestResult.Value);
            Assert.Equal("Invalid dealer request", problem.Title);
        }

        [Fact]
        public async Task Create_DuplicateDealerCode_ReturnsConflict()
        {
            // Arrange
            var request = new CreateDealerRequest
            {
                DealerCode = "DLR001",
                Name = "Apex Auto"
            };

            _dealerRepoMock
                .Setup(r => r.DealerCodeExistsAsync("DLR001", It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.Create(request, CancellationToken.None);

            // Assert
            var conflictResult = Assert.IsType<ConflictObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status409Conflict, conflictResult.StatusCode);
            var problem = Assert.IsType<ProblemDetails>(conflictResult.Value);
            Assert.Equal("Dealer already exists", problem.Title);
        }

        [Fact]
        public async Task GetById_NonPositiveId_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.GetById(0, CancellationToken.None);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        }

        [Fact]
        public async Task GetById_DealerNotFound_ReturnsNotFound()
        {
            // Arrange
            _dealerRepoMock
                .Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                .ReturnsAsync((EntityDealer?)null);

            // Act
            var result = await _controller.GetById(99, CancellationToken.None);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundResult>(result.Result);
            Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
        }

        [Fact]
        public async Task GetById_DealerExists_ReturnsOkWithDealer()
        {
            // Arrange
            var dealer = new EntityDealer
            {
                Id = 1,
                DealerCode = "DLR001",
                Name = "Apex Auto",
                Email = "dealer@apex.com",
                Phone = "1234567890",
                Status = DealerStatus.Active
            };

            _dealerRepoMock
                .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(dealer);

            // Act
            var result = await _controller.GetById(1, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            var response = Assert.IsType<DealerResponse>(okResult.Value);
            Assert.Equal(1, response.Id);
            Assert.Equal("DLR001", response.DealerCode);
        }

        [Fact]
        public async Task GetAvailable_ValidRequest_ReturnsOkWithCandidates()
        {
            // Arrange
            var request = new DealerAvailabilityRequest
            {
                ItemId = 1,
                SKU = "BIKE-001",
                Quantity = 2,
                City = "Pune"
            };

            var candidates = new List<DealerAvailabilityCandidate>
            {
                new DealerAvailabilityCandidate
                {
                    DealerId = 10,
                    DealerName = "Pune Motors",
                    BranchName = "Main",
                    City = "Pune",
                    AvailableQuantity = 5
                }
            };

            _inventoryRepoMock
                .Setup(r => r.GetCandidatesAsync(1, "BIKE-001", 2, "Pune", null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(candidates);

            _strategyMock
                .Setup(s => s.Score(It.IsAny<DealerAvailabilityCandidate>(), request))
                .Returns(100.0m);

            // Act
            var result = await _controller.GetAvailable(request, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            var response = Assert.IsType<List<DealerAvailabilityResponse>>(okResult.Value);
            Assert.Single(response);
            Assert.Equal(10, response[0].DealerId);
        }

        [Fact]
        public async Task GetAvailable_InvalidRequest_ReturnsBadRequest()
        {
            // Arrange
            var request = new DealerAvailabilityRequest
            {
                ItemId = 0, // Invalid
                SKU = "BIKE-001",
                Quantity = 1
            };

            // Act
            var result = await _controller.GetAvailable(request, CancellationToken.None);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        }
    }
}
