using Imtes.API.Controllers;
using Items.Application.Commands;
using Items.Application.DTOs;
using Items.Application.Interfaces;
using Items.Application.Services;
using ItemService.Application.DTOs;
using Items.Domain.Entities;
using Items.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Items.Tests.Controllers
{
    public class ItemsControllerTests
    {
        private readonly Mock<IItemRepository> _repoMock;
        private readonly Mock<IEventPublisher> _eventsMock;
        private readonly ItemApplicationService _service;
        private readonly ItemsController _controller;

        public ItemsControllerTests()
        {
            _repoMock = new Mock<IItemRepository>();
            _eventsMock = new Mock<IEventPublisher>();
            _service = new ItemApplicationService(_repoMock.Object, _eventsMock.Object);
            _controller = new ItemsController(_service);
        }

        [Fact]
        public async Task GetById_ItemFound_ReturnsOkWithItem()
        {
            // Arrange
            var item = new Item
            {
                Id = 1,
                SKU = "MODEL-S",
                Name = "Tesla Model S",
                Status = ItemStatus.Active,
                CreatedAtUtc = DateTime.UtcNow
            };

            _repoMock
                .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(item);

            // Act
            var result = await _controller.GetById(1, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            var response = Assert.IsType<ItemResponse>(okResult.Value);
            Assert.Equal(1, response.Id);
            Assert.Equal("MODEL-S", response.SKU);
        }

        [Fact]
        public async Task GetById_ItemNotFound_ReturnsNotFound()
        {
            // Arrange
            _repoMock
                .Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Item?)null);

            // Act
            var result = await _controller.GetById(99, CancellationToken.None);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
        }

        [Fact]
        public async Task Search_ValidRequest_ReturnsOkWithPagedResponse()
        {
            // Arrange
            var request = new ItemSearchRequest
            {
                Page = 1,
                PageSize = 10,
                Search = "Tesla"
            };

            var items = new List<Item>
            {
                new Item { Id = 1, SKU = "TESLA-1", Name = "Tesla Model 3", Status = ItemStatus.Active, CreatedAtUtc = DateTime.UtcNow }
            };

            _repoMock
                .Setup(r => r.SearchAsync(It.IsAny<ItemSearchRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((items, 1));

            // Act
            var result = await _controller.Search(request, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            var paged = Assert.IsType<PagedResponse<ItemResponse>>(okResult.Value);
            Assert.Single(paged.Items);
            Assert.Equal(1, paged.TotalCount);
        }

        [Fact]
        public async Task Create_ValidItem_ReturnsCreatedAtAction()
        {
            // Arrange
            var request = new CreateItemRequest
            {
                SKU = "FORD-MUSTANG",
                Name = "Ford Mustang GT",
                VehicleMakeId = 1,
                VehicleModelId = 2,
                VehicleVariantId = 3,
                VehicleCategoryId = 4
            };

            _repoMock
                .Setup(r => r.GetBySkuAsync("FORD-MUSTANG", It.IsAny<CancellationToken>()))
                .ReturnsAsync((Item?)null);

            _repoMock
                .Setup(r => r.AddAsync(It.IsAny<Item>(), It.IsAny<CancellationToken>()))
                .Callback<Item, CancellationToken>((i, ct) => i.Id = 42)
                .Returns(Task.CompletedTask);

            _repoMock
                .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var createdItem = new Item
            {
                Id = 42,
                SKU = "FORD-MUSTANG",
                Name = "Ford Mustang GT",
                VehicleMakeId = 1,
                VehicleModelId = 2,
                VehicleVariantId = 3,
                VehicleCategoryId = 4,
                Status = ItemStatus.Active,
                CreatedAtUtc = DateTime.UtcNow
            };

            _repoMock
                .Setup(r => r.GetByIdAsync(42, It.IsAny<CancellationToken>()))
                .ReturnsAsync(createdItem);

            // Act
            var result = await _controller.Create(request, CancellationToken.None);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(StatusCodes.Status201Created, createdResult.StatusCode);
            var response = Assert.IsType<ItemResponse>(createdResult.Value);
            Assert.Equal(42, response.Id);
            Assert.Equal("FORD-MUSTANG", response.SKU);
        }

        [Fact]
        public async Task Create_MissingSku_ReturnsBadRequest()
        {
            // Arrange
            var request = new CreateItemRequest
            {
                SKU = "",
                Name = "Ford Mustang"
            };

            // Act
            var result = await _controller.Create(request, CancellationToken.None);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        }

        [Fact]
        public async Task Create_DuplicateSku_ReturnsConflict()
        {
            // Arrange
            var request = new CreateItemRequest
            {
                SKU = "EXISTING-SKU",
                Name = "Vehicle"
            };

            _repoMock
                .Setup(r => r.GetBySkuAsync("EXISTING-SKU", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Item { Id = 1, SKU = "EXISTING-SKU" });

            // Act
            var result = await _controller.Create(request, CancellationToken.None);

            // Assert
            var conflictResult = Assert.IsType<ConflictObjectResult>(result);
            Assert.Equal(StatusCodes.Status409Conflict, conflictResult.StatusCode);
        }

        [Fact]
        public async Task Update_ItemFound_ReturnsOkWithUpdatedItem()
        {
            // Arrange
            var existingItem = new Item
            {
                Id = 1,
                SKU = "CAR-1",
                Name = "Old Name",
                Status = ItemStatus.Active,
                CreatedAtUtc = DateTime.UtcNow
            };

            var updateRequest = new UpdateItemRequest
            {
                Name = "New Name",
                VehicleMakeId = 1,
                VehicleModelId = 1,
                VehicleVariantId = 1,
                VehicleCategoryId = 1
            };

            _repoMock
                .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingItem);

            _repoMock
                .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var result = await _controller.Update(1, updateRequest, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            var response = Assert.IsType<ItemResponse>(okResult.Value);
            Assert.Equal("New Name", response.Name);
        }

        [Fact]
        public async Task Update_ItemNotFound_ReturnsNotFound()
        {
            // Arrange
            _repoMock
                .Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Item?)null);

            var updateRequest = new UpdateItemRequest { Name = "New Name" };

            // Act
            var result = await _controller.Update(99, updateRequest, CancellationToken.None);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
        }

        [Fact]
        public async Task ChangeStatus_ItemFound_ReturnsOkWithNewStatus()
        {
            // Arrange
            var existingItem = new Item
            {
                Id = 1,
                SKU = "CAR-1",
                Name = "Car",
                Status = ItemStatus.Active,
                CreatedAtUtc = DateTime.UtcNow
            };

            var request = new ChangeItemStatusRequest { Status = ItemStatus.Inactive };

            _repoMock
                .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingItem);

            _repoMock
                .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var result = await _controller.ChangeStatus(1, request, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            var response = Assert.IsType<ItemResponse>(okResult.Value);
            Assert.Equal(ItemStatus.Inactive, response.Status);
        }

        [Fact]
        public async Task ChangeStatus_ItemNotFound_ReturnsNotFound()
        {
            // Arrange
            _repoMock
                .Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Item?)null);

            var request = new ChangeItemStatusRequest { Status = ItemStatus.Inactive };

            // Act
            var result = await _controller.ChangeStatus(99, request, CancellationToken.None);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
        }
    }
}
