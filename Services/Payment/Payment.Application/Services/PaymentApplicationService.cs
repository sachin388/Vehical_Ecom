using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Payment.Application.DTOs;
using Payment.Application.IntegrationEvents;
using Payment.Application.Interfaces;
using Payment.Domain.Entities;

namespace Payment.Application.Services
{
    using PaymentEntity = Payment.Domain.Entities.Payment;

    public sealed class PaymentApplicationService : IPaymentApplicationService
    {
        private readonly IPaymentRepository _repository;
        private readonly IPayUGatewayService _payUGatewayService;
        private readonly IOutboxWriter _outboxWriter;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PaymentApplicationService> _logger;

        public PaymentApplicationService(
            IPaymentRepository repository,
            IPayUGatewayService payUGatewayService,
            IOutboxWriter outboxWriter,
            IUnitOfWork unitOfWork,
            IConfiguration configuration,
            ILogger<PaymentApplicationService> logger)
        {
            _repository = repository;
            _payUGatewayService = payUGatewayService;
            _outboxWriter = outboxWriter;
            _unitOfWork = unitOfWork;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<InitiatePaymentResponse> InitiatePaymentAsync(
            InitiatePaymentRequest request,
            CancellationToken ct = default)
        {
            if (request.SalesOrderId == Guid.Empty)
                throw new ArgumentException("SalesOrderId is required.", nameof(request));

            if (request.Amount <= 0)
                throw new ArgumentException("Amount must be greater than zero.", nameof(request));

            if (string.IsNullOrWhiteSpace(request.CustomerName))
                throw new ArgumentException("Customer name is required.", nameof(request));

            if (string.IsNullOrWhiteSpace(request.CustomerEmail))
                throw new ArgumentException("Customer email is required.", nameof(request));

            if (string.IsNullOrWhiteSpace(request.CustomerPhone))
                throw new ArgumentException("Customer phone is required.", nameof(request));

            if (string.IsNullOrWhiteSpace(request.ProductInfo))
                throw new ArgumentException("Product info is required.", nameof(request));

            var merchantKey = _configuration["PayU:MerchantKey"] ?? "payu_test_key";
            var merchantSalt = _configuration["PayU:MerchantSalt"] ?? "payu_test_salt";
            var paymentUrl = _configuration["PayU:PaymentUrl"] ?? "https://test.payu.in/_payment";
            var defaultSuccessUrl = _configuration["PayU:DefaultSuccessUrl"] ?? "https://localhost:7001/api/payments/callback";
            var defaultFailureUrl = _configuration["PayU:DefaultFailureUrl"] ?? "https://localhost:7001/api/payments/callback";

            var surl = string.IsNullOrWhiteSpace(request.SuccessUrl) ? defaultSuccessUrl : request.SuccessUrl;
            var furl = string.IsNullOrWhiteSpace(request.FailureUrl) ? defaultFailureUrl : request.FailureUrl;

            var txnId = $"TXN{DateTime.UtcNow:yyyyMMddHHmmss}{Guid.NewGuid():N}"[..24].ToUpperInvariant();
            var idempotencyKey = $"IDEMP-{Guid.NewGuid():N}";
            var formattedAmount = request.Amount.ToString("F2", CultureInfo.InvariantCulture);
            var customerId = string.IsNullOrWhiteSpace(request.CustomerId) ? $"CUST-{Guid.NewGuid():N}"[..12] : request.CustomerId;

            var payment = new PaymentEntity
            {
                PaymentReference = txnId,
                SalesOrderId = request.SalesOrderId,
                CustomerId = customerId,
                Amount = request.Amount,
                Currency = string.IsNullOrWhiteSpace(request.Currency) ? "INR" : request.Currency.ToUpperInvariant(),
                Status = "Initiated",
                PaymentMethod = "PayU",
                IdempotencyKey = idempotencyKey,
                CreatedAtUtc = DateTime.UtcNow
            };

            payment.Transactions.Add(new PaymentTransaction
            {
                TransactionType = "Capture",
                Amount = request.Amount,
                Currency = payment.Currency,
                Status = "Pending",
                GatewayMessage = "Payment initiated, awaiting customer checkout on PayU.",
                ProcessedAtUtc = DateTime.UtcNow
            });

            await _unitOfWork.BeginTransactionAsync(ct);
            await _repository.AddAsync(payment, ct);
            await _unitOfWork.CommitTransactionAsync(ct);

            var hash = _payUGatewayService.GeneratePaymentHash(
                key: merchantKey,
                txnId: txnId,
                amount: request.Amount,
                productInfo: request.ProductInfo.Trim(),
                firstName: request.CustomerName.Trim(),
                email: request.CustomerEmail.Trim(),
                udf1: request.Udf1,
                udf2: request.Udf2,
                udf3: request.Udf3,
                udf4: request.Udf4,
                udf5: request.Udf5,
                salt: merchantSalt);

            var formParameters = new Dictionary<string, string>
            {
                ["key"] = merchantKey,
                ["txnid"] = txnId,
                ["amount"] = formattedAmount,
                ["productinfo"] = request.ProductInfo.Trim(),
                ["firstname"] = request.CustomerName.Trim(),
                ["email"] = request.CustomerEmail.Trim(),
                ["phone"] = request.CustomerPhone.Trim(),
                ["surl"] = surl,
                ["furl"] = furl,
                ["hash"] = hash,
                ["udf1"] = request.Udf1 ?? string.Empty,
                ["udf2"] = request.Udf2 ?? string.Empty,
                ["udf3"] = request.Udf3 ?? string.Empty,
                ["udf4"] = request.Udf4 ?? string.Empty,
                ["udf5"] = request.Udf5 ?? string.Empty
            };

            return new InitiatePaymentResponse
            {
                PaymentId = payment.Id,
                SalesOrderId = payment.SalesOrderId,
                PaymentReference = payment.PaymentReference,
                TransactionId = txnId,
                PaymentUrl = paymentUrl,
                FormParameters = formParameters,
                Status = payment.Status
            };
        }

        public async Task<PaymentResponse> ProcessCallbackAsync(
            PayUCallbackPayload payload,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(payload.TxnId))
                throw new ArgumentException("Transaction reference (txnid) is missing in callback.", nameof(payload));

            var merchantSalt = _configuration["PayU:MerchantSalt"] ?? "payu_test_salt";

            await _unitOfWork.BeginTransactionAsync(ct);

            var payment = await _repository.GetByPaymentReferenceAsync(payload.TxnId, ct)
                ?? throw new KeyNotFoundException($"Payment with reference '{payload.TxnId}' was not found.");

            // Idempotency: if already Captured or Refunded
            if (string.Equals(payment.Status, "Captured", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(payment.Status, "Refunded", StringComparison.OrdinalIgnoreCase))
            {
                await _unitOfWork.CommitTransactionAsync(ct);
                return Map(payment);
            }

            var isHashValid = _payUGatewayService.VerifyResponseHash(payload, merchantSalt);
            if (!isHashValid)
            {
                _logger.LogWarning("PayU callback signature hash mismatch for transaction {TxnId}. Marking as Tampered.", payload.TxnId);
                payment.MarkTampered("Response signature verification failed (Tampered).");
                await _repository.SaveChangesAsync(ct);
                await _unitOfWork.CommitTransactionAsync(ct);
                return Map(payment);
            }

            var isSuccessStatus = string.Equals(payload.Status, "success", StringComparison.OrdinalIgnoreCase);

            if (isSuccessStatus)
            {
                var verifyResult = await _payUGatewayService.VerifyPaymentAsync(payload.TxnId, ct);

                if (verifyResult.IsSuccess)
                {
                    var mihpayId = !string.IsNullOrWhiteSpace(payload.MihpayId) ? payload.MihpayId : verifyResult.MihpayId ?? string.Empty;
                    var mode = !string.IsNullOrWhiteSpace(payload.Mode) ? payload.Mode : verifyResult.PaymentMode;
                    var bankRef = !string.IsNullOrWhiteSpace(payload.BankRefNum) ? payload.BankRefNum : verifyResult.BankRefNum;

                    payment.MarkSuccess(mihpayId, mode, bankRef);

                    // Add Outbox event for SalesOrder confirmation
                    var eventPayload = new PaymentSucceededPayload(
                        SalesOrderId: payment.SalesOrderId,
                        Amount: payment.Amount,
                        Currency: payment.Currency);

                    await _outboxWriter.AddAsync(
                        eventType: "PaymentSucceeded",
                        aggregateId: payment.SalesOrderId.ToString(),
                        payload: eventPayload,
                        ct: ct);

                    _logger.LogInformation("Payment {PaymentRef} Captured. Published PaymentSucceeded event for order {OrderId}.",
                        payment.PaymentReference, payment.SalesOrderId);
                }
                else
                {
                    _logger.LogWarning("PayU verify_payment rejected transaction {TxnId}: {Error}",
                        payload.TxnId, verifyResult.ErrorMessage);
                    payment.MarkFailed(verifyResult.ErrorMessage ?? "Payment verification failed on gateway.");
                }
            }
            else
            {
                payment.MarkFailed(payload.ErrorMessage ?? $"PayU returned status: {payload.Status}");
            }

            await _repository.SaveChangesAsync(ct);
            await _unitOfWork.CommitTransactionAsync(ct);

            return Map(payment);
        }

        public async Task<PaymentResponse?> GetByIdAsync(long id, CancellationToken ct = default)
        {
            var payment = await _repository.GetByIdAsync(id, ct);
            return payment is null ? null : Map(payment);
        }

        public async Task<PaymentResponse?> GetByPaymentReferenceAsync(string paymentReference, CancellationToken ct = default)
        {
            var payment = await _repository.GetByPaymentReferenceAsync(paymentReference, ct);
            return payment is null ? null : Map(payment);
        }

        public async Task<IReadOnlyList<PaymentResponse>> GetBySalesOrderIdAsync(Guid salesOrderId, CancellationToken ct = default)
        {
            var list = await _repository.GetBySalesOrderIdAsync(salesOrderId, ct);
            return list.Select(Map).ToList();
        }

        public async Task<PaymentResponse> VerifyPaymentStatusAsync(string paymentReference, CancellationToken ct = default)
        {
            await _unitOfWork.BeginTransactionAsync(ct);

            var payment = await _repository.GetByPaymentReferenceAsync(paymentReference, ct)
                ?? throw new KeyNotFoundException($"Payment with reference '{paymentReference}' was not found.");

            if (string.Equals(payment.Status, "Captured", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(payment.Status, "Refunded", StringComparison.OrdinalIgnoreCase))
            {
                await _unitOfWork.CommitTransactionAsync(ct);
                return Map(payment);
            }

            var verifyResult = await _payUGatewayService.VerifyPaymentAsync(paymentReference, ct);

            if (verifyResult.IsSuccess)
            {
                payment.MarkSuccess(
                    verifyResult.MihpayId ?? string.Empty,
                    verifyResult.PaymentMode,
                    verifyResult.BankRefNum);

                var eventPayload = new PaymentSucceededPayload(
                    SalesOrderId: payment.SalesOrderId,
                    Amount: payment.Amount,
                    Currency: payment.Currency);

                await _outboxWriter.AddAsync(
                    eventType: "PaymentSucceeded",
                    aggregateId: payment.SalesOrderId.ToString(),
                    payload: eventPayload,
                    ct: ct);
            }
            else if (!string.IsNullOrWhiteSpace(verifyResult.Status) &&
                     !string.Equals(verifyResult.Status, "pending", StringComparison.OrdinalIgnoreCase))
            {
                payment.MarkFailed(verifyResult.ErrorMessage ?? $"PayU status: {verifyResult.Status}");
            }

            await _repository.SaveChangesAsync(ct);
            await _unitOfWork.CommitTransactionAsync(ct);

            return Map(payment);
        }

        public async Task<RefundPaymentResponse> RefundPaymentAsync(
            RefundPaymentRequest request,
            CancellationToken ct = default)
        {
            if (request.Amount <= 0)
                throw new ArgumentException("Refund amount must be greater than zero.", nameof(request));

            await _unitOfWork.BeginTransactionAsync(ct);

            PaymentEntity? payment = null;
            if (request.PaymentId.HasValue)
            {
                payment = await _repository.GetByIdAsync(request.PaymentId.Value, ct);
            }
            else if (request.SalesOrderId.HasValue)
            {
                var orders = await _repository.GetBySalesOrderIdAsync(request.SalesOrderId.Value, ct);
                payment = orders.FirstOrDefault(x => string.Equals(x.Status, "Captured", StringComparison.OrdinalIgnoreCase));
            }

            if (payment is null)
                throw new KeyNotFoundException("Captured payment was not found for refund.");

            if (!string.Equals(payment.Status, "Captured", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Cannot refund payment in '{payment.Status}' status.");

            if (string.IsNullOrWhiteSpace(payment.GatewayTransactionId))
                throw new InvalidOperationException("Gateway transaction ID (mihpayid) is missing for refund.");

            var refundRef = $"RFND-2026-{Guid.NewGuid():N}"[..20].ToUpperInvariant();

            var refund = new PaymentRefund
            {
                PaymentId = payment.Id,
                SalesOrderPublicId = payment.SalesOrderId,
                RefundReference = refundRef,
                Amount = request.Amount,
                Reason = string.IsNullOrWhiteSpace(request.Reason) ? "Order cancellation" : request.Reason,
                Status = "Requested"
            };

            await _repository.AddRefundAsync(refund, ct);

            // Call PayU Refund API
            var (gatewaySuccess, gatewayRefundId, errorMessage) = await _payUGatewayService.InitiateRefundAsync(
                gatewayPaymentId: payment.GatewayTransactionId,
                refundReference: refundRef,
                amount: request.Amount,
                ct: ct);

            if (gatewaySuccess)
            {
                refund.MarkSuccess(gatewayRefundId);
                payment.MarkRefunded(request.Amount);

                if (request.SagaId.HasValue)
                {
                    var compensationPayload = new PaymentCompensationCompletedPayload(
                        SagaId: request.SagaId.Value,
                        SalesOrderId: payment.SalesOrderId,
                        ProviderReference: gatewayRefundId ?? refundRef);

                    await _outboxWriter.AddAsync(
                        eventType: "PaymentCompensationCompleted",
                        aggregateId: payment.SalesOrderId.ToString(),
                        payload: compensationPayload,
                        ct: ct);

                    _logger.LogInformation("Published PaymentCompensationCompleted for saga {SagaId}, order {OrderId}.",
                        request.SagaId.Value, payment.SalesOrderId);
                }
            }
            else
            {
                refund.MarkFailed(errorMessage ?? "Gateway refund failed.");
            }

            await _repository.SaveChangesAsync(ct);
            await _unitOfWork.CommitTransactionAsync(ct);

            return new RefundPaymentResponse
            {
                RefundId = refund.Id,
                PaymentId = payment.Id,
                SalesOrderId = payment.SalesOrderId,
                RefundReference = refund.RefundReference,
                GatewayRefundId = gatewayRefundId,
                Amount = refund.Amount,
                Status = refund.Status,
                Error = gatewaySuccess ? null : errorMessage,
                CreatedAtUtc = refund.CreatedAtUtc,
                CompletedAtUtc = refund.CompletedAtUtc
            };
        }

        private static PaymentResponse Map(PaymentEntity entity) => new()
        {
            Id = entity.Id,
            PaymentReference = entity.PaymentReference,
            SalesOrderId = entity.SalesOrderId,
            CustomerId = entity.CustomerId,
            Amount = entity.Amount,
            Currency = entity.Currency,
            Status = entity.Status,
            PaymentMethod = entity.PaymentMethod,
            GatewayTransactionId = entity.GatewayTransactionId,
            CreatedAtUtc = entity.CreatedAtUtc,
            UpdatedAtUtc = entity.UpdatedAtUtc,
            Transactions = entity.Transactions.Select(t => new PaymentTransactionDto
            {
                Id = t.Id,
                TransactionType = t.TransactionType,
                Amount = t.Amount,
                Currency = t.Currency,
                Status = t.Status,
                GatewayResponseCode = t.GatewayResponseCode,
                GatewayMessage = t.GatewayMessage,
                ProcessedAtUtc = t.ProcessedAtUtc
            }).ToList(),
            Refunds = entity.Refunds.Select(r => new PaymentRefundDto
            {
                Id = r.Id,
                RefundReference = r.RefundReference,
                Amount = r.Amount,
                Reason = r.Reason,
                Status = r.Status,
                CreatedAtUtc = r.CreatedAtUtc,
                CompletedAtUtc = r.CompletedAtUtc
            }).ToList()
        };
    }
}
