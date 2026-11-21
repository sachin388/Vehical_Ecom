using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.Infrastructure.Security
{
    public interface IKeyMaterialService
    {
        SigningCredentials GetSigningCredentials();
        SecurityKey GetValidationSecurityKey();
        JsonWebKeySet GetJsonWebKeySet();
        string KeyId { get; }
    }

    public class KeyMaterialService : IKeyMaterialService
    {
        private static readonly RSA RsaKey = RSA.Create(2048);
        private static readonly string Kid = "vehicle-platform-key-1";

        public string KeyId => Kid;

        public SigningCredentials GetSigningCredentials()
        {
            var key = new RsaSecurityKey(RsaKey)
            {
                KeyId = Kid
            };

            return new SigningCredentials(key, SecurityAlgorithms.RsaSha256);
        }

        public SecurityKey GetValidationSecurityKey()
        {
            return new RsaSecurityKey(RsaKey)
            {
                KeyId = Kid
            };
        }

        public JsonWebKeySet GetJsonWebKeySet()
        {
            var parameters = RsaKey.ExportParameters(false); // public key only

            var jwk = new JsonWebKey
            {
                Kty = JsonWebAlgorithmsKeyTypes.RSA,
                Use = "sig",
                Kid = Kid,
                Alg = SecurityAlgorithms.RsaSha256,
                N = Base64UrlEncoder.Encode(parameters.Modulus),
                E = Base64UrlEncoder.Encode(parameters.Exponent)
            };

            var jwks = new JsonWebKeySet();
            jwks.Keys.Add(jwk);
            return jwks;
        }
    }
}

