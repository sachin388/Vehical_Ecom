using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Payment.Application.DTOs;
using Payment.Application.Interfaces;

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
        /// Initiates a payment session for an order, returning the PayU checkout form parameters and SHA-512 hash.
        /// </summary>
        [HttpPost("initiate")]
        public async Task<IActionResult> InitiatePayment(
            [FromBody] InitiatePaymentRequest request,
            CancellationToken ct)
        {
            var response = await _service.InitiatePaymentAsync(request, ct);
            return Ok(response);
        }

        /// <summary>
        /// PayU POST callback endpoint (surl / furl / webhook notification).
        /// PayU posts application/x-www-form-urlencoded data here when user completes payment.
        /// </summary>
        [HttpPost("callback")]
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
        public async Task<IActionResult> GetBySalesOrderId(Guid salesOrderId, CancellationToken ct)
        {
            var results = await _service.GetBySalesOrderIdAsync(salesOrderId, ct);
            return Ok(results);
        }

        /// <summary>
        /// Server-to-server active verification with PayU postservice API for a payment reference.
        /// </summary>
        [HttpPost("transaction/{txnId}/verify")]
        public async Task<IActionResult> VerifyPayment(string txnId, CancellationToken ct)
        {
            var result = await _service.VerifyPaymentStatusAsync(txnId, ct);
            return Ok(result);
        }

        /// <summary>
        /// Initiates refund / compensation for an order payment via PayU.
        /// </summary>
        [HttpPost("refund")]
        public async Task<IActionResult> RefundPayment(
            [FromBody] RefundPaymentRequest request,
            CancellationToken ct)
        {
            var result = await _service.RefundPaymentAsync(request, ct);
            return Ok(result);
        }
    }
}
