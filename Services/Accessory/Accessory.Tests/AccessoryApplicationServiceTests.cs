using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Accessory.Application.DTOs;
using Accessory.Application.Interfaces;
using Accessory.Application.Services;
using Accessory.Domain.Entities;
using Accessory.Domain.Enums;
using Accessory.Domain.Events;
using Moq;
using Xunit;

namespace Accessory.Tests
{
    using Accessory = Accessory.Domain.Entities.Accessory;

    public class AccessoryApplicationServiceTests
    {
        private readonly Mock<IAccessoryRepository> _repositoryMock;
        private readonly Mock<IEventPublisher> _publisherMock;
        private readonly AccessoryApplicationService _service;

        public AccessoryApplicationServiceTests()
        {
            _repositoryMock = new Mock<IAccessoryRepository>();
            _publisherMock = new Mock<IEventPublisher>();
            _service = new AccessoryApplicationService(_repositoryMock.Object, _publisherMock.Object);
        }

        [Fact]
        public async Task CreateAsync_ValidRequest_ReturnsAccessoryResponse()
        {
            var request = new CreateAccessoryRequest
            {
                Code = "ACC-001",
                Name = "Floor Mats",
                Description = "All-weather floor mats",
                Price = 1500m,
                Currency = "INR",
                AvailableQuantity = 10
            };

            var response = await _service.CreateAsync(request, CancellationToken.None);

            Assert.NotNull(response);
            Assert.Equal("ACC-001", response.Code);
            Assert.Equal("Floor Mats", response.Name);
            Assert.Equal(1500m, response.Price);
            Assert.Equal(10, response.AvailableQuantity);
            _repositoryMock.Verify(x => x.AddAsync(It.IsAny<Accessory>(), It.IsAny<CancellationToken>()), Times.Once);
            _repositoryMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_InvalidRequest_ThrowsArgumentException()
        {
            var request = new CreateAccessoryRequest
            {
                Code = "",
                Name = "Floor Mats",
                Price = 100m,
                AvailableQuantity = 1
            };

            await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(request));
        }

        [Fact]
        public async Task GetAsync_ExistingId_ReturnsResponse()
        {
            var entity = new Accessory
            {
                Id = 1,
                Code = "ACC-001",
                Name = "Floor Mats",
                Price = 1500m,
                Status = AccessoryStatus.Active
            };

            _repositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(entity);

            var result = await _service.GetAsync(1, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("ACC-001", result.Code);
        }

        [Fact]
        public async Task GetAsync_NonExistingId_ReturnsNull()
        {
            _repositoryMock.Setup(x => x.GetByIdAsync(999, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Accessory?)null);

            var result = await _service.GetAsync(999, CancellationToken.None);

            Assert.Null(result);
        }

        [Fact]
        public async Task UpdatePriceAsync_ValidPrice_UpdatesAndPublishes()
        {
            var entity = new Accessory
            {
                Id = 1,
                Code = "ACC-001",
                Name = "Floor Mats",
                Price = 1500m,
                Currency = "INR"
            };

            _repositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(entity);

            var result = await _service.UpdatePriceAsync(1, new UpdateAccessoryPriceRequest { Price = 1800m });

            Assert.True(result);
            Assert.Equal(1800m, entity.Price);
            _repositoryMock.Verify(x => x.Update(entity), Times.Once);
            _publisherMock.Verify(x => x.PublishAsync(
                "accessory.price.changed",
                It.Is<AccessoryPriceChangedEvent>(e => e.AccessoryId == 1 && e.Price == 1800m),
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
