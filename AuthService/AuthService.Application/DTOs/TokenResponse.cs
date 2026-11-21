using System;
using System.Text.Json.Serialization;

namespace AuthService.Application.DTOs
{
    public class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = "Bearer";

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; } = 900;

        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; } = string.Empty;

        [JsonPropertyName("scope")]
        public string? Scope { get; set; }

        [JsonPropertyName("id_token")]
        public string? IdToken { get; set; }

        public DateTime AccessTokenExpiresAtUtc { get; set; }

        public DateTime RefreshTokenExpiresAtUtc { get; set; }
    }
}
