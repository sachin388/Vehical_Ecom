using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Payment.Infrastructure.Gateway.PayU;
using Xunit;

namespace Payment.Tests
{
    public class PayUResiliencePolicyTests
    {
        [Fact]
        public async Task ResiliencePipeline_WhenTransient503Occurs_RetriesUntilSuccess()
        {
            // Arrange
            var pipeline = PayUResiliencePolicy.CreateHttpRetryPipeline();
            var attempts = 0;

            // Act
            var response = await pipeline.ExecuteAsync(async _ =>
            {
                attempts++;
                if (attempts < 3)
                {
                    return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
                }
                return new HttpResponseMessage(HttpStatusCode.OK);
            });

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(3, attempts);
        }

        [Fact]
        public async Task ResiliencePipeline_WhenHttpRequestExceptionOccurs_RetriesAndRecovers()
        {
            // Arrange
            var pipeline = PayUResiliencePolicy.CreateHttpRetryPipeline();
            var attempts = 0;

            // Act
            var response = await pipeline.ExecuteAsync(async _ =>
            {
                attempts++;
                if (attempts < 2)
                {
                    throw new HttpRequestException("Temporary network timeout");
                }
                return new HttpResponseMessage(HttpStatusCode.OK);
            });

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(2, attempts);
        }

        [Fact]
        public async Task ResiliencePipeline_WhenNonTransient400Occurs_DoesNotRetry()
        {
            // Arrange
            var pipeline = PayUResiliencePolicy.CreateHttpRetryPipeline();
            var attempts = 0;

            // Act
            var response = await pipeline.ExecuteAsync(async _ =>
            {
                attempts++;
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            });

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(1, attempts);
        }
    }
}
