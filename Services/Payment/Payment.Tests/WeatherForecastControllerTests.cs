using Microsoft.Extensions.Logging;
using Moq;
using Payment.API;
using Payment.API.Controllers;
using Xunit;

namespace Payment.Tests.Controllers
{
    public class WeatherForecastControllerTests
    {
        private readonly Mock<ILogger<WeatherForecastController>> _loggerMock;
        private readonly WeatherForecastController _controller;

        public WeatherForecastControllerTests()
        {
            _loggerMock = new Mock<ILogger<WeatherForecastController>>();
            _controller = new WeatherForecastController(_loggerMock.Object);
        }

        [Fact]
        public void Get_ReturnsFiveWeatherForecasts()
        {
            // Act
            var result = _controller.Get();

            // Assert
            Assert.NotNull(result);
            var list = result.ToList();
            Assert.Equal(5, list.Count);
        }

        [Fact]
        public void Get_ForecastItemsHaveValidProperties()
        {
            // Act
            var result = _controller.Get().ToList();

            // Assert
            foreach (var item in result)
            {
                Assert.False(string.IsNullOrWhiteSpace(item.Summary));
                Assert.InRange(item.TemperatureC, -20, 55);
            }
        }
    }
}
