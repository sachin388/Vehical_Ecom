using System.Text.Json.Serialization;

namespace AuthService.Application.DTOs
{
    public class UserInfoResponse
    {
        [JsonPropertyName("sub")]
        public string Sub { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("given_name")]
        public string GivenName { get; set; } = string.Empty;

        [JsonPropertyName("family_name")]
        public string FamilyName { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("email_verified")]
        public bool EmailVerified { get; set; } = true;

        [JsonPropertyName("roles")]
        public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();

        [JsonPropertyName("permissions")]
        public IReadOnlyList<string> Permissions { get; set; } = Array.Empty<string>();

        [JsonPropertyName("client_id")]
        public string? ClientId { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAtUtc { get; set; }
    }
}

