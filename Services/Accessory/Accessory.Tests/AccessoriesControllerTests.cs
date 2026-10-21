using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Accessory.API.Controllers;
using Accessory.Application.DTOs;
using Accessory.Application.Interfaces;
using Accessory.Application.Services;
using Accessory.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Accessory.Tests
{
    using Accessory = Accessory.Domain.Entities.Accessory;

    public class AccessoriesControllerTests
    {
        private readonly Mock<IAccessoryRepository> _repoMock;
        private readonly Mock<IEventPublisher> _publisherMock;
        private readonly AccessoryApplicationService _service;
        private readonly AccessoriesController _controller;

        public AccessoriesControllerTests()
        {
            _repoMock = new Mock<IAccessoryRepository>();
            _publisherMock = new Mock<IEventPublisher>();
            _service = new AccessoryApplicationService(_repoMock.Object, _publisherMock.Object);
            _controller = new AccessoriesController(_service);
        }

        [Fact]
        public async Task Create_ValidRequest_ReturnsCreated()
        {
            var request = new CreateAccessoryRequest
            {
                Code = "ACC-01",
                Name = "Alloy Wheels",
                Price = 25000m,
                AvailableQuantity = 4
            };

            var result = await _controller.Create(request, CancellationToken.None);

            var createdResult = Assert.IsType<CreatedResult>(result);
            var response = Assert.IsType<AccessoryResponse>(createdResult.Value);
            Assert.Equal("ACC-01", response.Code);
        }

        [Fact]
        public async Task Get_ExistingId_ReturnsOk()
        {
            _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Accessory { Id = 1, Code = "ACC-01", Name = "Alloy Wheels" });

            var result = await _controller.Get(1, CancellationToken.None);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<AccessoryResponse>(okResult.Value);
            Assert.Equal(1, response.Id);
        }

        [Fact]
        public async Task Get_NonExistingId_ReturnsNotFound()
        {
            _repoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Accessory?)null);

            var result = await _controller.Get(99, CancellationToken.None);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task GetCompatible_ValidItem_ReturnsList()
        {
            _repoMock.Setup(r => r.GetCompatibleAccessoriesAsync(10, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Accessory>
                {
                    new() { Id = 1, Code = "ACC-01", Name = "Spoiler" }
                });

            var result = await _controller.GetCompatible(10, CancellationToken.None);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsType<List<AccessoryResponse>>(okResult.Value);
            Assert.Single(list);
        }

        [Fact]
        public async Task UpdatePrice_ExistingAccessory_ReturnsNoContent()
        {
            _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Accessory { Id = 1, Price = 1000m });

            var result = await _controller.UpdatePrice(1, new UpdateAccessoryPriceRequest { Price = 1200m }, CancellationToken.None);

            Assert.IsType<NoContentResult>(result);
        }
    }
}
