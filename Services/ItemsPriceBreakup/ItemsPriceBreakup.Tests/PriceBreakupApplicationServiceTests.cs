using ItemsPriceBreakup.Application.DTO;
using ItemsPriceBreakup.Application.Interfaces;
using ItemsPriceBreakup.Application.Pricing;
using ItemsPriceBreakup.Application.Services;
using ItemsPriceBreakup.Domain.Entities;
using ItemsPriceBreakup.Domain.Enums;
using Moq;
using Xunit;

namespace ItemsPriceBreakup.Tests.Services
{
    public class PriceBreakupApplicationServiceTests
    {
        private readonly Mock<IPriceBreakupRepository> _repoMock;
        private readonly Mock<IPriceBreakupCalculator> _calculatorMock;
        private readonly Mock<IEventPublisher> _eventPublisherMock;
        private readonly PriceBreakupApplicationService _service;

        public PriceBreakupApplicationServiceTests()
        {
            _repoMock = new Mock<IPriceBreakupRepository>();
            _calculatorMock = new Mock<IPriceBreakupCalculator>();
            _eventPublisherMock = new Mock<IEventPublisher>();

            _service = new PriceBreakupApplicationService(
                _repoMock.Object,
                _calculatorMock.Object,
                _eventPublisherMock.Object);
        }

        [Fact]
        public async Task CalculateAsync_ValidRequest_CalculatesAndSavesSuccessfully()
        {
            // Arrange
            var request = new CalculatePriceBreakupRequest
            {
                ItemId = 101,
                SKU = "E-SCOOTER-01",
                StateCode = "MH",
                VehicleCategory = "2-Wheeler",
                PriceVersion = 1,
                ExShowroomPrice = 100000m,
                TotalDiscount = 5000m,
                GstRate = 18m,
                RtoAmount = 8000m,
                InsuranceAmount = 4000m,
                Currency = "INR"
            };

            var lines = new List<PriceBreakupLine>
            {
                new PriceBreakupLine
                {
                    Id = 1,
                    ChargeType = ChargeType.ExShowroom,
                    Code = "EX_SHOWROOM",
                    Name = "Ex Showroom",
                    Amount = 100000m,
                    TaxAmount = 18000m
                },
                new PriceBreakupLine
                {
                    Id = 2,
                    ChargeType = ChargeType.RTO,
                    Code = "RTO",
                    Name = "RTO Tax",
                    Amount = 8000m,
                    TaxAmount = 0m
                }
            };

            _calculatorMock
                .Setup(c => c.Calculate(It.IsAny<PriceCalculationContext>()))
                .Returns(lines);

            _repoMock
                .Setup(r => r.AddAsync(It.IsAny<PriceBreakup>(), It.IsAny<CancellationToken>()))
                .Callback<PriceBreakup, CancellationToken>((pb, ct) => pb.Id = 501)
                .Returns(Task.CompletedTask);

            _repoMock
                .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.CalculateAsync(request, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(501, result.Id);
            Assert.Equal(95000m, result.SubTotal); // 100000 - 5000
            Assert.Equal(18000m, result.TotalTax);
            Assert.Equal(8000m, result.TotalCharges);
            Assert.Equal(121000m, result.FinalOnRoadPrice); // 95000 + 18000 + 8000
            Assert.Equal(2, result.Lines.Count);

            _repoMock.Verify(r => r.AddAsync(It.IsAny<PriceBreakup>(), It.IsAny<CancellationToken>()), Times.Once);
            _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            _eventPublisherMock.Verify(p => p.PublishAsync(
                "vehicle.price.breakup.calculated",
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CalculateAsync_InvalidItemId_ThrowsArgumentException()
        {
            // Arrange
            var request = new CalculatePriceBreakupRequest
            {
                ItemId = 0,
                SKU = "SKU-01",
                StateCode = "MH"
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.CalculateAsync(request, CancellationToken.None));
        }

        [Fact]
        public async Task CalculateAsync_MissingSku_ThrowsArgumentException()
        {
            // Arrange
            var request = new CalculatePriceBreakupRequest
            {
                ItemId = 10,
                SKU = "",
                StateCode = "MH"
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.CalculateAsync(request, CancellationToken.None));
        }

        [Fact]
        public async Task CalculateAsync_MissingStateCode_ThrowsArgumentException()
        {
            // Arrange
            var request = new CalculatePriceBreakupRequest
            {
                ItemId = 10,
                SKU = "SKU-10",
                StateCode = "  "
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.CalculateAsync(request, CancellationToken.None));
        }
    }
}
