using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Items.Application.Commands;
using Items.Application.DTOs;
using Items.Application.Interfaces;
using Items.Application.Queries;
using Items.Application.Services;
using Items.Domain.Entities;
using Items.Domain.Enums;
using Moq;
using Xunit;

namespace Items.Tests
{
    public class ItemApplicationServiceTests
    {
        private readonly Mock<IItemRepository> _repoMock;
        private readonly Mock<IEventPublisher> _publisherMock;
        private readonly ItemApplicationService _service;

        public ItemApplicationServiceTests()
        {
            _repoMock = new Mock<IItemRepository>();
            _publisherMock = new Mock<IEventPublisher>();
            _service = new ItemApplicationService(_repoMock.Object, _publisherMock.Object);
        }

        [Fact]
        public async Task CreateAsync_ValidCommand_CreatesItem()
        {
            var command = new CreateItemCommand(new CreateItemRequest
            {
                SKU = "SEDAN-001",
                Name = "Executive Sedan",
                VehicleMakeId = 1,
                VehicleModelId = 2,
                VehicleVariantId = 3,
                VehicleCategoryId = 1,
                Description = "Executive luxury sedan"
            });

            _repoMock.Setup(r => r.GetBySkuAsync("SEDAN-001", It.IsAny<CancellationToken>()))
                .ReturnsAsync((Item?)null);
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Item { Id = 1, SKU = "SEDAN-001", Name = "Executive Sedan", Status = ItemStatus.Active });

            var response = await _service.CreateAsync(command, CancellationToken.None);

            Assert.NotNull(response);
            Assert.Equal("SEDAN-001", response.SKU);
            Assert.Equal("Executive Sedan", response.Name);
            _repoMock.Verify(r => r.AddAsync(It.IsAny<Item>(), It.IsAny<CancellationToken>()), Times.Once);
            _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_DuplicateSku_ThrowsInvalidOperationException()
        {
            var command = new CreateItemCommand(new CreateItemRequest
            {
                SKU = "SEDAN-001",
                Name = "Executive Sedan"
            });

            _repoMock.Setup(r => r.GetBySkuAsync("SEDAN-001", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Item { SKU = "SEDAN-001" });

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.CreateAsync(command, CancellationToken.None));
        }

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsItem()
        {
            var query = new GetItemByIdQuery(1);
            _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Item { Id = 1, SKU = "SEDAN-001", Name = "Executive Sedan", Status = ItemStatus.Active });

            var result = await _service.GetByIdAsync(query, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("SEDAN-001", result.SKU);
        }

        [Fact]
        public async Task GetByIdAsync_NotFound_ReturnsNull()
        {
            var query = new GetItemByIdQuery(99);
            _repoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Item?)null);

            var result = await _service.GetByIdAsync(query, CancellationToken.None);

            Assert.Null(result);
        }
    }
}
