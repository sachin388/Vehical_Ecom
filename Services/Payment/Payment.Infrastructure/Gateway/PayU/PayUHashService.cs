using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Payment.Application.DTOs;

namespace Payment.Infrastructure.Gateway.PayU
{
    public static class PayUHashService
    {
        public static string ComputeSha512(string input)
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            var hashBytes = SHA512.HashData(bytes);
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }

        public static string GeneratePaymentHash(
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
            string salt)
        {
            var formattedAmount = amount.ToString("F2", CultureInfo.InvariantCulture);
            var u1 = udf1 ?? string.Empty;
            var u2 = udf2 ?? string.Empty;
            var u3 = udf3 ?? string.Empty;
            var u4 = udf4 ?? string.Empty;
            var u5 = udf5 ?? string.Empty;

            // Pattern: key|txnid|amount|productinfo|firstname|email|udf1|udf2|udf3|udf4|udf5||||||SALT
            var raw = $"{key}|{txnId}|{formattedAmount}|{productInfo}|{firstName}|{email}|{u1}|{u2}|{u3}|{u4}|{u5}||||||{salt}";
            return ComputeSha512(raw);
        }

        public static bool VerifyResponseHash(PayUCallbackPayload payload, string salt)
        {
            if (string.IsNullOrWhiteSpace(payload.Hash))
                return false;

            var status = payload.Status ?? string.Empty;
            var u5 = payload.Udf5 ?? string.Empty;
            var u4 = payload.Udf4 ?? string.Empty;
            var u3 = payload.Udf3 ?? string.Empty;
            var u2 = payload.Udf2 ?? string.Empty;
            var u1 = payload.Udf1 ?? string.Empty;
            var email = payload.Email ?? string.Empty;
            var firstName = payload.FirstName ?? string.Empty;
            var productInfo = payload.ProductInfo ?? string.Empty;
            var amount = payload.Amount ?? string.Empty;
            var txnId = payload.TxnId ?? string.Empty;
            var key = payload.Key ?? string.Empty;

            string raw;
            if (!string.IsNullOrWhiteSpace(payload.AdditionalCharges))
            {
                // With additional charges:
                // additionalCharges|SALT|status||||||udf5|udf4|udf3|udf2|udf1|email|firstname|productinfo|amount|txnid|key
                raw = $"{payload.AdditionalCharges}|{salt}|{status}||||||{u5}|{u4}|{u3}|{u2}|{u1}|{email}|{firstName}|{productInfo}|{amount}|{txnId}|{key}";
            }
            else
            {
                // Without additional charges:
                // SALT|status||||||udf5|udf4|udf3|udf2|udf1|email|firstname|productinfo|amount|txnid|key
                raw = $"{salt}|{status}||||||{u5}|{u4}|{u3}|{u2}|{u1}|{email}|{firstName}|{productInfo}|{amount}|{txnId}|{key}";
            }

            var calculated = ComputeSha512(raw);
            return string.Equals(calculated, payload.Hash, StringComparison.OrdinalIgnoreCase);
        }

        public static string GenerateVerifyPaymentHash(string key, string command, string var1, string salt)
        {
            // Pattern: key|command|var1|salt
            var raw = $"{key}|{command}|{var1}|{salt}";
            return ComputeSha512(raw);
        }

        public static string GenerateRefundHash(string key, string command, string var1, string salt)
        {
            // Pattern: key|command|var1|salt
            var raw = $"{key}|{command}|{var1}|{salt}";
            return ComputeSha512(raw);
        }
    }
}

