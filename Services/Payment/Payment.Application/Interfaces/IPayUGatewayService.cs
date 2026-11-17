using System.Threading;
using System.Threading.Tasks;
using Payment.Application.DTOs;

namespace Payment.Application.Interfaces
{
    public interface IPayUGatewayService
    {
        string GeneratePaymentHash(
            string key,
            string txnId,
            decimal amount,
            string productInfo,
            string firstName,
            string email,
            string? udf1,
            string? udf2,
            string? udf3,
            string? udf4,
            string? udf5,
            string salt);

        bool VerifyResponseHash(PayUCallbackPayload payload, string salt);

        Task<VerifyPaymentGatewayResult> VerifyPaymentAsync(string txnId, CancellationToken ct = default);

        Task<(bool Success, string? RefundId, string? ErrorMessage)> InitiateRefundAsync(
            string gatewayPaymentId,
            string refundReference,
            decimal amount,
            CancellationToken ct = default);
    }
}

