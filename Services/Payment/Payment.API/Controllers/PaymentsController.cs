using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payment.Application.DTOs;
using Payment.Application.Interfaces;
using VehicleEcom.Security.Authorization;
using VehicleEcom.Security.Constants;

namespace Payment.API.Controllers
{
    [ApiController]
    [Route("api/payments")]
    public sealed class PaymentsController : ControllerBase
    {
        private readonly IPaymentApplicationService _service;

        public PaymentsController(IPaymentApplicationService service)
        {
            _service = service;
        }

        /// <summary>
        /// Initiates a payment transaction session with idempotency protection.
        /// Prevents duplicate transactions if retried with the same Idempotency-Key.
        /// </summary>
        [HttpPost]
        [HttpPost("initiate")]
        [Authorize]
        [HasPermission(VehiclePermissions.PaymentsProcess)]
        public async Task<IActionResult> InitiatePayment(
            [FromBody] InitiatePaymentRequest request,
            [FromHeader(Name = "Idempotency-Key")] string? idempotencyKeyHeader,
            CancellationToken ct)
        {
            if (!string.IsNullOrWhiteSpace(idempotencyKeyHeader) && string.IsNullOrWhiteSpace(request.IdempotencyKey))
            {
                request.IdempotencyKey = idempotencyKeyHeader.Trim();
            }

            var response = await _service.InitiatePaymentAsync(request, ct);
            return Ok(response);
        }

        /// <summary>
        /// PayU POST callback endpoint (surl / furl / webhook notification).
        /// PayU posts application/x-www-form-urlencoded data here when user completes payment.
        /// Safe and idempotent against duplicate callback submissions.
        /// </summary>
        [HttpPost("callback")]
        [AllowAnonymous]
        [Consumes("application/x-www-form-urlencoded", "application/json")]
        public async Task<IActionResult> ProcessCallback(
            [FromForm] PayUCallbackPayload formPayload,
            CancellationToken ct)
        {
            var payload = formPayload;
            if (string.IsNullOrWhiteSpace(payload.TxnId) && Request.HasJsonContentType())
            {
                payload = await Request.ReadFromJsonAsync<PayUCallbackPayload>(cancellationToken: ct)
                    ?? formPayload;
            }

            var result = await _service.ProcessCallbackAsync(payload, ct);
            return Ok(result);
        }

        /// <summary>
        /// Gets payment details by Payment primary key ID.
        /// </summary>
        [HttpGet("{id:long}")]
        [Authorize]
        [HasPermission(VehiclePermissions.PaymentsRead)]
        public async Task<IActionResult> GetById(long id, CancellationToken ct)
        {
            var result = await _service.GetByIdAsync(id, ct);
            if (result is null)
                return NotFound(new { message = $"Payment '{id}' not found." });

            return Ok(result);
        }

        /// <summary>
        /// Gets payment details by PayU Transaction ID / Payment Reference (txnid).
        /// </summary>
        [HttpGet("transaction/{txnId}")]
        [Authorize]
        [HasPermission(VehiclePermissions.PaymentsRead)]
        public async Task<IActionResult> GetByPaymentReference(string txnId, CancellationToken ct)
        {
            var result = await _service.GetByPaymentReferenceAsync(txnId, ct);
            if (result is null)
                return NotFound(new { message = $"Payment with reference '{txnId}' not found." });

            return Ok(result);
        }

        /// <summary>
        /// Gets all payment attempts for a given Sales Order.
        /// </summary>
        [HttpGet("order/{salesOrderId:guid}")]
        [Authorize]
        [HasPermission(VehiclePermissions.PaymentsRead)]
        public async Task<IActionResult> GetBySalesOrderId(Guid salesOrderId, CancellationToken ct)
        {
            var results = await _service.GetBySalesOrderIdAsync(salesOrderId, ct);
            return Ok(results);
        }

        /// <summary>
        /// Server-to-server active verification with PayU postservice API for a payment reference.
        /// Resilient against temporary server or network drops.
        /// </summary>
        [HttpPost("transaction/{txnId}/verify")]
        [Authorize]
        [HasPermission(VehiclePermissions.PaymentsProcess)]
        public async Task<IActionResult> VerifyPayment(string txnId, CancellationToken ct)
        {
            var result = await _service.VerifyPaymentStatusAsync(txnId, ct);
            return Ok(result);
        }

        /// <summary>
        /// Initiates refund / compensation for an order payment via PayU.
        /// Idempotent across retries using SagaId / Idempotency-Key.
        /// </summary>
        [HttpPost("refund")]
        [Authorize]
        [HasPermission(VehiclePermissions.PaymentsRefund)]
        public async Task<IActionResult> RefundPayment(
            [FromBody] RefundPaymentRequest request,
            [FromHeader(Name = "Idempotency-Key")] string? idempotencyKeyHeader,
            CancellationToken ct)
        {
            if (!string.IsNullOrWhiteSpace(idempotencyKeyHeader) && string.IsNullOrWhiteSpace(request.IdempotencyKey))
            {
                request.IdempotencyKey = idempotencyKeyHeader.Trim();
            }

            var result = await _service.RefundPaymentAsync(request, ct);
            return Ok(result);
        }
    }
}
