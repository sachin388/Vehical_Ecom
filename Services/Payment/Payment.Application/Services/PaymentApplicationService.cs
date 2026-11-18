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

            // 1. Idempotency Check: prevent duplicate payment creation for the same idempotency key
            var idempotencyKey = string.IsNullOrWhiteSpace(request.IdempotencyKey)
                ? $"IDEMP-SO-{request.SalesOrderId:N}"
                : request.IdempotencyKey.Trim();

            var existingPayment = await _repository.GetByIdempotencyKeyAsync(idempotencyKey, ct);
            if (existingPayment is not null)
            {
                // Verify parameters match to detect conflicting requests with the same key
                if (existingPayment.SalesOrderId != request.SalesOrderId || existingPayment.Amount != request.Amount)
                {
                    throw new InvalidOperationException(
                        $"Idempotency key '{idempotencyKey}' was already used for a different order or amount.");
                }

                _logger.LogInformation("Idempotent request: returning existing payment {PaymentReference} for key {IdempotencyKey}.",
                    existingPayment.PaymentReference, idempotencyKey);

                return BuildInitiateResponse(
                    existingPayment,
                    merchantKey,
                    merchantSalt,
                    paymentUrl,
                    surl,
                    furl,
                    request);
            }

            var txnId = $"TXN{DateTime.UtcNow:yyyyMMddHHmmss}{Guid.NewGuid():N}"[..24].ToUpperInvariant();
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
            try
            {
                await _repository.AddAsync(payment, ct);
                await _unitOfWork.CommitTransactionAsync(ct);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(ct);

                // Handle concurrent race condition: another thread inserted the same IdempotencyKey simultaneously
                _logger.LogWarning(ex, "Concurrency collision on IdempotencyKey {IdempotencyKey}. Reloading existing payment.", idempotencyKey);
                var reloaded = await _repository.GetByIdempotencyKeyAsync(idempotencyKey, ct);
                if (reloaded is not null)
                {
                    return BuildInitiateResponse(reloaded, merchantKey, merchantSalt, paymentUrl, surl, furl, request);
                }
                throw;
            }

            return BuildInitiateResponse(payment, merchantKey, merchantSalt, paymentUrl, surl, furl, request);
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

            // Idempotency: if already Captured, Refunded, or Failed, return existing result without re-emitting events
            if (string.Equals(payment.Status, "Captured", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(payment.Status, "Refunded", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(payment.Status, "Failed", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Callback for payment {TxnId} received but status is already '{Status}'. Returning existing state idempotently.",
                    payload.TxnId, payment.Status);
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
                // Active server-to-server verification with PayU to ensure transaction integrity
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

            // Idempotent: if already Captured or Refunded, return existing state
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

            // 1. Idempotency Check for Saga & Refund
            if (request.SagaId.HasValue)
            {
                var existingSagaRefund = await _repository.GetRefundBySagaIdAsync(request.SagaId.Value, ct);
                if (existingSagaRefund is not null && string.Equals(existingSagaRefund.Status, "Completed", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Refund for saga {SagaId} already completed. Returning existing refund result idempotently.", request.SagaId.Value);
                    await _unitOfWork.CommitTransactionAsync(ct);
                    return new RefundPaymentResponse
                    {
                        RefundId = existingSagaRefund.Id,
                        PaymentId = existingSagaRefund.PaymentId,
                        SalesOrderId = existingSagaRefund.SalesOrderPublicId,
                        RefundReference = existingSagaRefund.RefundReference,
                        Amount = existingSagaRefund.Amount,
                        Status = existingSagaRefund.Status,
                        CreatedAtUtc = existingSagaRefund.CreatedAtUtc,
                        CompletedAtUtc = existingSagaRefund.CompletedAtUtc
                    };
                }
            }

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

            // Formulate deterministic refund reference for idempotency across network retries
            var refundRef = !string.IsNullOrWhiteSpace(request.IdempotencyKey)
                ? request.IdempotencyKey.Trim()
                : request.SagaId.HasValue
                    ? $"RFND-SAGA-{request.SagaId:N}"[..20].ToUpperInvariant()
                    : $"RFND-2026-{Guid.NewGuid():N}"[..20].ToUpperInvariant();

            // Check if this refund reference was already recorded
            var existingRefund = await _repository.GetRefundByReferenceAsync(refundRef, ct);
            if (existingRefund is not null && string.Equals(existingRefund.Status, "Completed", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Refund with reference {RefundRef} already completed. Returning existing refund result idempotently.", refundRef);
                await _unitOfWork.CommitTransactionAsync(ct);
                return new RefundPaymentResponse
                {
                    RefundId = existingRefund.Id,
                    PaymentId = existingRefund.PaymentId,
                    SalesOrderId = existingRefund.SalesOrderPublicId,
                    RefundReference = existingRefund.RefundReference,
                    Amount = existingRefund.Amount,
                    Status = existingRefund.Status,
                    CreatedAtUtc = existingRefund.CreatedAtUtc,
                    CompletedAtUtc = existingRefund.CompletedAtUtc
                };
            }

            var refundReason = request.SagaId.HasValue
                ? $"Order cancellation saga {request.SagaId.Value}: {(string.IsNullOrWhiteSpace(request.Reason) ? "Customer cancellation" : request.Reason)}"
                : string.IsNullOrWhiteSpace(request.Reason) ? "Order cancellation" : request.Reason;

            var refund = new PaymentRefund
            {
                PaymentId = payment.Id,
                SalesOrderPublicId = payment.SalesOrderId,
                RefundReference = refundRef,
                Amount = request.Amount,
                Reason = refundReason,
                Status = "Requested"
            };

            await _repository.AddRefundAsync(refund, ct);

            // Call PayU Refund API with safe retry & constant refund reference (var2)
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

        private InitiatePaymentResponse BuildInitiateResponse(
            PaymentEntity payment,
            string merchantKey,
            string merchantSalt,
            string paymentUrl,
            string surl,
            string furl,
            InitiatePaymentRequest request)
        {
            var formattedAmount = payment.Amount.ToString("F2", CultureInfo.InvariantCulture);

            var hash = _payUGatewayService.GeneratePaymentHash(
                key: merchantKey,
                txnId: payment.PaymentReference,
                amount: payment.Amount,
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
                ["txnid"] = payment.PaymentReference,
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
                TransactionId = payment.PaymentReference,
                PaymentUrl = paymentUrl,
                FormParameters = formParameters,
                Status = payment.Status
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
