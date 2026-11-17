using Payment.Application.DTOs;
using Payment.Infrastructure.Gateway.PayU;
using Xunit;

namespace Payment.Tests
{
    public class PayUHashServiceTests
    {
        private const string TestKey = "test_merchant_key";
        private const string TestSalt = "test_merchant_salt";

        [Fact]
        public void GeneratePaymentHash_ShouldGenerateCorrectSha512Hash()
        {
            // Arrange
            var txnId = "TXN123456789";
            var amount = 1500.50m;
            var productInfo = "Vehicle accessories";
            var firstName = "Sachin";
            var email = "sachin@example.com";
            var udf1 = "CustomVal1";

            // Act
            var hash = PayUHashService.GeneratePaymentHash(
                TestKey, txnId, amount, productInfo, firstName, email,
                udf1, null, null, null, null, TestSalt);

            // Assert
            Assert.NotNull(hash);
            Assert.Equal(128, hash.Length); // SHA-512 hex is 128 characters
        }

        [Fact]
        public void VerifyResponseHash_WithoutAdditionalCharges_ShouldReturnTrue_ForValidHash()
        {
            // Arrange
            var payload = new PayUCallbackPayload
            {
                Key = TestKey,
                TxnId = "TXN99999",
                Amount = "1500.00",
                ProductInfo = "Car Polish",
                FirstName = "Sachin",
                Email = "sachin@example.com",
                Status = "success",
                Udf1 = "udf1_val"
            };

            // salt|status||||||udf5|udf4|udf3|udf2|udf1|email|firstname|productinfo|amount|txnid|key
            var expectedRaw = $"{TestSalt}|{payload.Status}||||||||||{payload.Udf1}|{payload.Email}|{payload.FirstName}|{payload.ProductInfo}|{payload.Amount}|{payload.TxnId}|{payload.Key}";
            payload.Hash = PayUHashService.ComputeSha512(expectedRaw);

            // Act
            var isValid = PayUHashService.VerifyResponseHash(payload, TestSalt);

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public void VerifyResponseHash_WithAdditionalCharges_ShouldReturnTrue_ForValidHash()
        {
            // Arrange
            var payload = new PayUCallbackPayload
            {
                Key = TestKey,
                TxnId = "TXN99999",
                Amount = "1500.00",
                ProductInfo = "Car Polish",
                FirstName = "Sachin",
                Email = "sachin@example.com",
                Status = "success",
                AdditionalCharges = "15.00"
            };

            // additionalCharges|SALT|status||||||udf5|udf4|udf3|udf2|udf1|email|firstname|productinfo|amount|txnid|key
            var expectedRaw = $"{payload.AdditionalCharges}|{TestSalt}|{payload.Status}|||||||||||{payload.Email}|{payload.FirstName}|{payload.ProductInfo}|{payload.Amount}|{payload.TxnId}|{payload.Key}";
            payload.Hash = PayUHashService.ComputeSha512(expectedRaw);

            // Act
            var isValid = PayUHashService.VerifyResponseHash(payload, TestSalt);

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public void VerifyResponseHash_WithTamperedAmount_ShouldReturnFalse()
        {
            // Arrange
            var payload = new PayUCallbackPayload
            {
                Key = TestKey,
                TxnId = "TXN99999",
                Amount = "1500.00",
                ProductInfo = "Car Polish",
                FirstName = "Sachin",
                Email = "sachin@example.com",
                Status = "success"
            };

            var validRaw = $"{TestSalt}|{payload.Status}|||||||||||{payload.Email}|{payload.FirstName}|{payload.ProductInfo}|{payload.Amount}|{payload.TxnId}|{payload.Key}";
            payload.Hash = PayUHashService.ComputeSha512(validRaw);

            // Tamper with amount
            payload.Amount = "10.00";

            // Act
            var isValid = PayUHashService.VerifyResponseHash(payload, TestSalt);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void GenerateVerifyPaymentHash_ShouldReturn128CharHex()
        {
            // Act
            var hash = PayUHashService.GenerateVerifyPaymentHash(TestKey, "verify_payment", "TXN123", TestSalt);

            // Assert
            Assert.NotNull(hash);
            Assert.Equal(128, hash.Length);
        }

        [Fact]
        public void GenerateRefundHash_ShouldReturn128CharHex()
        {
            // Act
            var hash = PayUHashService.GenerateRefundHash(TestKey, "cancel_refund_transaction", "MIHPAY123", TestSalt);

            // Assert
            Assert.NotNull(hash);
            Assert.Equal(128, hash.Length);
        }
    }
}

