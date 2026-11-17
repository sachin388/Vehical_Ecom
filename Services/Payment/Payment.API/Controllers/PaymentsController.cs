using System;
using Microsoft.AspNetCore.Mvc;

namespace Payment.API.Controllers
{
    [ApiController]
    [Route("api/payments")]
    public class PaymentsController : ControllerBase
    {
        [HttpPost]
        public IActionResult InitiatePayment([FromBody] PaymentRequestDto request)
        {
            var response = new
            {
                paymentId = "PAY-" + Guid.NewGuid().ToString("N").Substring(0, 12).ToUpper(),
                orderId = request.OrderId,
                amount = request.Amount,
                status = "Completed",
                redirectUrl = (string)null
            };
            return Ok(response);
        }
    }

    public class PaymentRequestDto
    {
        public long OrderId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string IdempotencyKey { get; set; } = string.Empty;
    }
}
