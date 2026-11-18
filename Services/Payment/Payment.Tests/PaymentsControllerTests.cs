using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Payment.API.Controllers;
using Payment.Application.DTOs;
using Payment.Application.Interfaces;
using Xunit;

namespace Payment.Tests
{
    public class PaymentsControllerTests
    {
        private readonly Mock<IPaymentApplicationService> _serviceMock;
        private readonly PaymentsController _controller;

        public PaymentsControllerTests()
        {
            _serviceMock = new Mock<IPaymentApplicationService>();
            _controller = new PaymentsController(_serviceMock.Object);
        }

        [Fact]
        public async Task InitiatePayment_ReturnsOkWithResponse_AndPassesIdempotencyHeader()
        {
            // Arrange
            var request = new InitiatePaymentRequest
            {
                SalesOrderId = Guid.NewGuid(),
                Amount = 1000m,
                CustomerName = "Sachin",
                CustomerEmail = "test@example.com",
                CustomerPhone = "1234567890",
                ProductInfo = "Item"
            };

            var expectedResponse = new InitiatePaymentResponse
            {
                PaymentId = 1L,
                SalesOrderId = request.SalesOrderId,
                PaymentReference = "TXN123",
                TransactionId = "TXN123",
                PaymentUrl = "https://test.payu.in/_payment",
                Status = "Initiated"
            };

            _serviceMock.Setup(s => s.InitiatePaymentAsync(It.Is<InitiatePaymentRequest>(r =>
                r.IdempotencyKey == "IDEMP-HEADER-123"), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.InitiatePayment(request, "IDEMP-HEADER-123", CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<InitiatePaymentResponse>(okResult.Value);
            Assert.Equal(expectedResponse.PaymentId, response.PaymentId);
        }

        [Fact]
        public async Task ProcessCallback_ReturnsOkWithPaymentResponse()
        {
            // Arrange
            var payload = new PayUCallbackPayload
            {
                TxnId = "TXN123",
                Status = "success"
            };

            var expectedResponse = new PaymentResponse
            {
                Id = 1L,
                PaymentReference = "TXN123",
                Status = "Captured"
            };

            _serviceMock.Setup(s => s.ProcessCallbackAsync(payload, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.ProcessCallback(payload, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<PaymentResponse>(okResult.Value);
            Assert.Equal("Captured", response.Status);
        }

        [Fact]
        public async Task GetById_WhenFound_ReturnsOk()
        {
            // Arrange
            var id = 1L;
            var expectedResponse = new PaymentResponse
            {
                Id = id,
                Status = "Captured"
            };

            _serviceMock.Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetById(id, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<PaymentResponse>(okResult.Value);
            Assert.Equal(id, response.Id);
        }

        [Fact]
        public async Task GetById_WhenNotFound_ReturnsNotFound()
        {
            // Arrange
            var id = 999L;
            _serviceMock.Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync((PaymentResponse?)null);

            // Act
            var result = await _controller.GetById(id, CancellationToken.None);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task RefundPayment_ReturnsOk()
        {
            // Arrange
            var request = new RefundPaymentRequest
            {
                PaymentId = 1L,
                Amount = 500m,
                Reason = "Cancelled"
            };

            var expectedResponse = new RefundPaymentResponse
            {
                RefundId = 10L,
                Status = "Completed",
                Amount = 500m
            };

            _serviceMock.Setup(s => s.RefundPaymentAsync(It.Is<RefundPaymentRequest>(r =>
                r.IdempotencyKey == "IDEMP-REF-1"), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.RefundPayment(request, "IDEMP-REF-1", CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<RefundPaymentResponse>(okResult.Value);
            Assert.Equal("Completed", response.Status);
        }
    }
}
