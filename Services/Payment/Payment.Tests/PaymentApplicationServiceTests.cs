using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Payment.Application.DTOs;
using Payment.Application.IntegrationEvents;
using Payment.Application.Interfaces;
using Payment.Application.Services;
using Payment.Domain.Entities;
using Xunit;

namespace Payment.Tests
{
    using PaymentEntity = Payment.Domain.Entities.Payment;

    public class PaymentApplicationServiceTests
    {
        private readonly Mock<IPaymentRepository> _repoMock;
        private readonly Mock<IPayUGatewayService> _gatewayMock;
        private readonly Mock<IOutboxWriter> _outboxMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ILogger<PaymentApplicationService>> _loggerMock;
        private readonly IConfiguration _configuration;
        private readonly PaymentApplicationService _service;

        public PaymentApplicationServiceTests()
        {
            _repoMock = new Mock<IPaymentRepository>();
            _gatewayMock = new Mock<IPayUGatewayService>();
            _outboxMock = new Mock<IOutboxWriter>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _loggerMock = new Mock<ILogger<PaymentApplicationService>>();

            var configDict = new Dictionary<string, string?>
            {
                ["PayU:MerchantKey"] = "test_key",
                ["PayU:MerchantSalt"] = "test_salt",
                ["PayU:PaymentUrl"] = "https://test.payu.in/_payment",
                ["PayU:DefaultSuccessUrl"] = "https://localhost:7001/api/payments/callback",
                ["PayU:DefaultFailureUrl"] = "https://localhost:7001/api/payments/callback"
            };

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configDict)
                .Build();

            _service = new PaymentApplicationService(
                _repoMock.Object,
                _gatewayMock.Object,
                _outboxMock.Object,
                _unitOfWorkMock.Object,
                _configuration,
                _loggerMock.Object);
        }

        [Fact]
        public async Task InitiatePaymentAsync_WithValidRequest_ReturnsFormParametersAndInitiatedStatus()
        {
            // Arrange
            var request = new InitiatePaymentRequest
            {
                SalesOrderId = Guid.NewGuid(),
                Amount = 25000.00m,
                Currency = "INR",
                CustomerName = "Sachin C",
                CustomerEmail = "sachin@example.com",
                CustomerPhone = "9876543210",
                ProductInfo = "Vehicle accessories package",
                IdempotencyKey = "IDEMP-TEST-001"
            };

            _gatewayMock
                .Setup(g => g.GeneratePaymentHash(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                    It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>()))
                .Returns("mock_sha512_hash");

            // Act
            var result = await _service.InitiatePaymentAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Initiated", result.Status);
            Assert.Equal(request.SalesOrderId, result.SalesOrderId);
            Assert.StartsWith("TXN", result.TransactionId);
            Assert.Equal("mock_sha512_hash", result.FormParameters["hash"]);
            Assert.Equal("test_key", result.FormParameters["key"]);
            Assert.Equal("25000.00", result.FormParameters["amount"]);

            _repoMock.Verify(r => r.AddAsync(It.Is<PaymentEntity>(t =>
                t.SalesOrderId == request.SalesOrderId &&
                t.Amount == request.Amount &&
                t.IdempotencyKey == "IDEMP-TEST-001" &&
                t.Status == "Initiated"), It.IsAny<CancellationToken>()), Times.Once);

            _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task InitiatePaymentAsync_WithDuplicateIdempotencyKey_ReturnsExistingPaymentWithoutDuplicateInsert()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var idempotencyKey = "IDEMP-RETRY-999";
            var existingPayment = new PaymentEntity
            {
                Id = 42,
                PaymentReference = "TXN_EXISTING_123",
                SalesOrderId = orderId,
                CustomerId = "CUST-001",
                Amount = 15000.00m,
                Currency = "INR",
                Status = "Initiated",
                IdempotencyKey = idempotencyKey
            };

            _repoMock.Setup(r => r.GetByIdempotencyKeyAsync(idempotencyKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingPayment);

            var retryRequest = new InitiatePaymentRequest
            {
                SalesOrderId = orderId,
                Amount = 15000.00m,
                Currency = "INR",
                CustomerName = "Sachin",
                CustomerEmail = "sachin@example.com",
                CustomerPhone = "9876543210",
                ProductInfo = "Item package",
                IdempotencyKey = idempotencyKey
            };

            _gatewayMock
                .Setup(g => g.GeneratePaymentHash(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                    It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>()))
                .Returns("mock_sha512_hash");

            // Act
            var result = await _service.InitiatePaymentAsync(retryRequest);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(42, result.PaymentId);
            Assert.Equal("TXN_EXISTING_123", result.TransactionId);
            Assert.Equal("Initiated", result.Status);

            // Ensure AddAsync was NEVER called again
            _repoMock.Verify(r => r.AddAsync(It.IsAny<PaymentEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task InitiatePaymentAsync_WithSameIdempotencyKeyButDifferentAmount_ThrowsInvalidOperationException()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var idempotencyKey = "IDEMP-CONFLICT";
            var existingPayment = new PaymentEntity
            {
                Id = 1,
                PaymentReference = "TXN123",
                SalesOrderId = orderId,
                Amount = 1000.00m,
                IdempotencyKey = idempotencyKey
            };

            _repoMock.Setup(r => r.GetByIdempotencyKeyAsync(idempotencyKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingPayment);

            var conflictingRequest = new InitiatePaymentRequest
            {
                SalesOrderId = orderId,
                Amount = 5000.00m, // Different amount!
                CustomerName = "Sachin",
                CustomerEmail = "sachin@example.com",
                CustomerPhone = "9876543210",
                ProductInfo = "Item",
                IdempotencyKey = idempotencyKey
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.InitiatePaymentAsync(conflictingRequest));
        }

        [Fact]
        public async Task InitiatePaymentAsync_WithInvalidAmount_ThrowsArgumentException()
        {
            // Arrange
            var request = new InitiatePaymentRequest
            {
                SalesOrderId = Guid.NewGuid(),
                Amount = 0,
                CustomerName = "Sachin",
                CustomerEmail = "sachin@example.com",
                CustomerPhone = "9876543210",
                ProductInfo = "Test"
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.InitiatePaymentAsync(request));
        }

        [Fact]
        public async Task ProcessCallbackAsync_WithValidSuccessCallback_MarksCapturedAndPublishesEvent()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var txnId = "TXN123456789";
            var existingPayment = new PaymentEntity
            {
                Id = 1,
                SalesOrderId = orderId,
                PaymentReference = txnId,
                Amount = 1500.00m,
                Currency = "INR",
                Status = "Initiated"
            };

            _repoMock.Setup(r => r.GetByPaymentReferenceAsync(txnId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingPayment);

            var payload = new PayUCallbackPayload
            {
                TxnId = txnId,
                MihpayId = "MIH987654",
                Status = "success",
                Amount = "1500.00",
                Hash = "valid_hash",
                Mode = "UPI",
                BankRefNum = "BANK_REF_111"
            };

            _gatewayMock.Setup(g => g.VerifyResponseHash(payload, "test_salt"))
                .Returns(true);

            _gatewayMock.Setup(g => g.VerifyPaymentAsync(txnId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new VerifyPaymentGatewayResult
                {
                    IsSuccess = true,
                    Status = "success",
                    MihpayId = "MIH987654",
                    PaymentMode = "UPI",
                    BankRefNum = "BANK_REF_111"
                });

            // Act
            var result = await _service.ProcessCallbackAsync(payload);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Captured", result.Status);
            Assert.Equal("MIH987654", result.GatewayTransactionId);
            Assert.Equal("UPI", result.PaymentMethod);

            _outboxMock.Verify(o => o.AddAsync(
                "PaymentSucceeded",
                orderId.ToString(),
                It.Is<PaymentSucceededPayload>(p =>
                    p.SalesOrderId == orderId &&
                    p.Amount == 1500.00m &&
                    p.Currency == "INR"),
                It.IsAny<CancellationToken>()), Times.Once);

            _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ProcessCallbackAsync_WhenAlreadyCaptured_ReturnsExistingResultIdempotentlyWithoutDuplicateEvents()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var txnId = "TXN123456789";
            var existingPayment = new PaymentEntity
            {
                Id = 1,
                SalesOrderId = orderId,
                PaymentReference = txnId,
                Amount = 1500.00m,
                Currency = "INR",
                Status = "Captured", // Already captured previously
                GatewayTransactionId = "MIH987654"
            };

            _repoMock.Setup(r => r.GetByPaymentReferenceAsync(txnId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingPayment);

            var payload = new PayUCallbackPayload
            {
                TxnId = txnId,
                Status = "success"
            };

            // Act
            var result = await _service.ProcessCallbackAsync(payload);

            // Assert
            Assert.Equal("Captured", result.Status);
            Assert.Equal("MIH987654", result.GatewayTransactionId);

            // Ensure NO duplicate outbox event was emitted on retry
            _outboxMock.Verify(o => o.AddAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ProcessCallbackAsync_WithTamperedHash_MarksTamperedAndDoesNotPublishEvent()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var txnId = "TXN123456789";
            var existingPayment = new PaymentEntity
            {
                Id = 1,
                SalesOrderId = orderId,
                PaymentReference = txnId,
                Amount = 1500.00m,
                Currency = "INR",
                Status = "Initiated"
            };

            _repoMock.Setup(r => r.GetByPaymentReferenceAsync(txnId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingPayment);

            var payload = new PayUCallbackPayload
            {
                TxnId = txnId,
                Status = "success",
                Hash = "tampered_fake_hash"
            };

            _gatewayMock.Setup(g => g.VerifyResponseHash(payload, "test_salt"))
                .Returns(false); // Tampered!

            // Act
            var result = await _service.ProcessCallbackAsync(payload);

            // Assert
            Assert.Equal("Tampered", result.Status);
            Assert.Equal("Tampered", existingPayment.Status);

            _outboxMock.Verify(o => o.AddAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ProcessCallbackAsync_WithFailedStatus_MarksFailed()
        {
            // Arrange
            var txnId = "TXN123456789";
            var existingPayment = new PaymentEntity
            {
                Id = 1,
                SalesOrderId = Guid.NewGuid(),
                PaymentReference = txnId,
                Amount = 1500.00m,
                Currency = "INR",
                Status = "Initiated"
            };

            _repoMock.Setup(r => r.GetByPaymentReferenceAsync(txnId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingPayment);

            var payload = new PayUCallbackPayload
            {
                TxnId = txnId,
                Status = "failure",
                ErrorMessage = "Insufficient funds",
                Hash = "valid_hash"
            };

            _gatewayMock.Setup(g => g.VerifyResponseHash(payload, "test_salt"))
                .Returns(true);

            // Act
            var result = await _service.ProcessCallbackAsync(payload);

            // Assert
            Assert.Equal("Failed", result.Status);
            Assert.Equal("Failed", existingPayment.Status);

            _outboxMock.Verify(o => o.AddAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task RefundPaymentAsync_WithSuccessfulPayment_InitiatesRefundAndEmitsCompensationEvent()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var sagaId = Guid.NewGuid();
            var paymentId = 100L;

            var existingPayment = new PaymentEntity
            {
                Id = paymentId,
                SalesOrderId = orderId,
                PaymentReference = "TXN123",
                GatewayTransactionId = "MIH777",
                Amount = 5000.00m,
                Status = "Captured"
            };

            _repoMock.Setup(r => r.GetByIdAsync(paymentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingPayment);

            _gatewayMock.Setup(g => g.InitiateRefundAsync(
                "MIH777", It.IsAny<string>(), 5000.00m, It.IsAny<CancellationToken>()))
                .ReturnsAsync((true, "REFUND_GATEWAY_123", null));

            var request = new RefundPaymentRequest
            {
                PaymentId = paymentId,
                Amount = 5000.00m,
                Reason = "Customer request",
                SagaId = sagaId
            };

            // Act
            var result = await _service.RefundPaymentAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Completed", result.Status);
            Assert.Equal("REFUND_GATEWAY_123", result.GatewayRefundId);
            Assert.Equal("Refunded", existingPayment.Status);

            _outboxMock.Verify(o => o.AddAsync(
                "PaymentCompensationCompleted",
                orderId.ToString(),
                It.Is<PaymentCompensationCompletedPayload>(p =>
                    p.SagaId == sagaId &&
                    p.SalesOrderId == orderId &&
                    p.ProviderReference == "REFUND_GATEWAY_123"),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RefundPaymentAsync_WhenSagaRefundAlreadyCompleted_ReturnsExistingResultIdempotentlyWithoutGatewayCall()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var sagaId = Guid.NewGuid();

            var completedRefund = new PaymentRefund
            {
                Id = 999,
                PaymentId = 100,
                SalesOrderPublicId = orderId,
                RefundReference = "RFND-SAGA-COMPLETED",
                Amount = 5000.00m,
                Status = "Completed"
            };

            _repoMock.Setup(r => r.GetRefundBySagaIdAsync(sagaId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(completedRefund);

            var retryRequest = new RefundPaymentRequest
            {
                SalesOrderId = orderId,
                Amount = 5000.00m,
                SagaId = sagaId
            };

            // Act
            var result = await _service.RefundPaymentAsync(retryRequest);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(999, result.RefundId);
            Assert.Equal("Completed", result.Status);

            // Ensure gateway was NOT called again to avoid duplicate refund on PayU
            _gatewayMock.Verify(g => g.InitiateRefundAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
