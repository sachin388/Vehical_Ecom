using ItemsPriceBreakup.API.Controllers;
using ItemsPriceBreakup.Application.DTO;
using ItemsPriceBreakup.Application.Interfaces;
using ItemsPriceBreakup.Application.Pricing;
using ItemsPriceBreakup.Application.Services;
using ItemsPriceBreakup.Domain.Entities;
using ItemsPriceBreakup.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace ItemsPriceBreakup.Tests.Controllers
{
    public class PriceBreakupsControllerTests
    {
        private readonly Mock<IPriceBreakupRepository> _repositoryMock;
        private readonly Mock<IPriceBreakupCalculator> _calculatorMock;
        private readonly Mock<IEventPublisher> _eventPublisherMock;
        private readonly PriceBreakupApplicationService _service;
        private readonly PriceBreakupsController _controller;

        public PriceBreakupsControllerTests()
        {
            _repositoryMock = new Mock<IPriceBreakupRepository>();
            _calculatorMock = new Mock<IPriceBreakupCalculator>();
            _eventPublisherMock = new Mock<IEventPublisher>();

            _service = new PriceBreakupApplicationService(
                _repositoryMock.Object,
                _calculatorMock.Object,
                _eventPublisherMock.Object);

            _controller = new PriceBreakupsController(_service);
        }

        [Fact]
        public async Task Calculate_ValidRequest_ReturnsOkWithPriceBreakupResponse()
        {
            // Arrange
            var request = new CalculatePriceBreakupRequest
            {
                ItemId = 101,
                SKU = "E-BIKE-X1",
                StateCode = "DL",
                VehicleCategory = "2-Wheeler",
                PriceVersion = 1,
                ExShowroomPrice = 85000m,
                TotalDiscount = 5000m,
                GstRate = 18m,
                RtoAmount = 6000m,
                InsuranceAmount = 3500m,
                Currency = "INR"
            };

            var calculatedLines = new List<PriceBreakupLine>
            {
                new PriceBreakupLine
                {
                    Id = 1,
                    ChargeType = ChargeType.ExShowroom,
                    Code = "EX_SHOWROOM",
                    Name = "Ex Showroom Price",
                    Amount = 85000m,
                    TaxAmount = 15300m
                },
                new PriceBreakupLine
                {
                    Id = 2,
                    ChargeType = ChargeType.RTO,
                    Code = "RTO",
                    Name = "RTO Charges",
                    Amount = 6000m,
                    TaxAmount = 0m
                }
            };

            _calculatorMock
                .Setup(c => c.Calculate(It.IsAny<PriceCalculationContext>()))
                .Returns(calculatedLines);

            _repositoryMock
                .Setup(r => r.AddAsync(It.IsAny<PriceBreakup>(), It.IsAny<CancellationToken>()))
                .Callback<PriceBreakup, CancellationToken>((pb, ct) => pb.Id = 999)
                .Returns(Task.CompletedTask);

            _repositoryMock
                .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Calculate(request, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);

            var response = Assert.IsType<PriceBreakupResponse>(okResult.Value);
            Assert.Equal(999, response.Id);
            Assert.Equal(101, response.ItemId);
            Assert.Equal("E-BIKE-X1", response.SKU);
            Assert.Equal("DL", response.StateCode);
            Assert.Equal(80000m, response.SubTotal); // 85000 - 5000
            Assert.Equal(15300m, response.TotalTax);
            Assert.Equal(6000m, response.TotalCharges);
            Assert.Equal(101300m, response.FinalOnRoadPrice); // 80000 + 15300 + 6000
            Assert.Equal(2, response.Lines.Count);

            _repositoryMock.Verify(r => r.AddAsync(It.IsAny<PriceBreakup>(), It.IsAny<CancellationToken>()), Times.Once);
            _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            _eventPublisherMock.Verify(p => p.PublishAsync(
                "vehicle.price.breakup.calculated",
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Calculate_InvalidItemId_ThrowsArgumentException()
        {
            // Arrange
            var request = new CalculatePriceBreakupRequest
            {
                ItemId = -1,
                SKU = "E-BIKE-X1",
                StateCode = "DL"
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _controller.Calculate(request, CancellationToken.None));
        }

        [Fact]
        public async Task Calculate_MissingSku_ThrowsArgumentException()
        {
            // Arrange
            var request = new CalculatePriceBreakupRequest
            {
                ItemId = 101,
                SKU = "",
                StateCode = "DL"
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _controller.Calculate(request, CancellationToken.None));
        }

        [Fact]
        public async Task Calculate_MissingStateCode_ThrowsArgumentException()
        {
            // Arrange
            var request = new CalculatePriceBreakupRequest
            {
                ItemId = 101,
                SKU = "E-BIKE-X1",
                StateCode = "   "
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _controller.Calculate(request, CancellationToken.None));
        }
    }
}
