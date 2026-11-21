using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AuthService.Application.DTOs
{
    public class OpenIdConfigurationResponse
    {
        [JsonPropertyName("issuer")]
        public string Issuer { get; set; } = string.Empty;

        [JsonPropertyName("authorization_endpoint")]
        public string AuthorizationEndpoint { get; set; } = string.Empty;

        [JsonPropertyName("token_endpoint")]
        public string TokenEndpoint { get; set; } = string.Empty;

        [JsonPropertyName("userinfo_endpoint")]
        public string UserInfoEndpoint { get; set; } = string.Empty;

        [JsonPropertyName("jwks_uri")]
        public string JwksUri { get; set; } = string.Empty;

        [JsonPropertyName("revocation_endpoint")]
        public string RevocationEndpoint { get; set; } = string.Empty;

        [JsonPropertyName("response_types_supported")]
        public IReadOnlyList<string> ResponseTypesSupported { get; set; } = Array.Empty<string>();

        [JsonPropertyName("subject_types_supported")]
        public IReadOnlyList<string> SubjectTypesSupported { get; set; } = Array.Empty<string>();

        [JsonPropertyName("id_token_signing_alg_values_supported")]
        public IReadOnlyList<string> IdTokenSigningAlgValuesSupported { get; set; } = Array.Empty<string>();

        [JsonPropertyName("scopes_supported")]
        public IReadOnlyList<string> ScopesSupported { get; set; } = Array.Empty<string>();

        [JsonPropertyName("token_endpoint_auth_methods_supported")]
        public IReadOnlyList<string> TokenEndpointAuthMethodsSupported { get; set; } = Array.Empty<string>();

        [JsonPropertyName("claims_supported")]
        public IReadOnlyList<string> ClaimsSupported { get; set; } = Array.Empty<string>();

        [JsonPropertyName("grant_types_supported")]
        public IReadOnlyList<string> GrantTypesSupported { get; set; } = Array.Empty<string>();
    }
}

