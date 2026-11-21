using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace VehicleEcom.Security.ServiceAuth;

public sealed class ServiceTokenClient : IServiceTokenClient
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly IOptions<ServiceTokenOptions> _options;
    private readonly ILogger<ServiceTokenClient> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public ServiceTokenClient(
        HttpClient httpClient,
        IMemoryCache cache,
        IOptions<ServiceTokenOptions> options,
        ILogger<ServiceTokenClient> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _options = options;
        _logger = logger;
    }

    public async Task<string> GetServiceTokenAsync(string? scope = null, CancellationToken cancellationToken = default)
    {
        var targetScope = scope ?? _options.Value.DefaultScope;
        var cacheKey = $"service_token_{_options.Value.ClientId}_{targetScope}";

        if (_cache.TryGetValue<string>(cacheKey, out var cachedToken) && !string.IsNullOrEmpty(cachedToken))
        {
            return cachedToken;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            // Double-check cache inside lock
            if (_cache.TryGetValue<string>(cacheKey, out cachedToken) && !string.IsNullOrEmpty(cachedToken))
            {
                return cachedToken;
            }

            _logger.LogInformation("Requesting OAuth 2.0 M2M client token for {ClientId} with scope {Scope}",
                _options.Value.ClientId, targetScope);

            var requestBody = new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = _options.Value.ClientId,
                ["client_secret"] = _options.Value.ClientSecret,
                ["scope"] = targetScope
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, _options.Value.TokenEndpoint)
            {
                Content = new FormUrlEncodedContent(requestBody)
            };

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to obtain M2M service token from {Endpoint}. StatusCode: {StatusCode}, Error: {Error}",
                    _options.Value.TokenEndpoint, response.StatusCode, errorText);
                throw new InvalidOperationException($"Failed to acquire service token: {response.StatusCode} - {errorText}");
            }

            var tokenResult = await response.Content.ReadFromJsonAsync<OAuthTokenResponse>(cancellationToken: cancellationToken);
            if (tokenResult == null || string.IsNullOrEmpty(tokenResult.AccessToken))
            {
                throw new InvalidOperationException("Invalid token response received from authorization server.");
            }

            // Cache token with expiration buffer (refresh 60 seconds earlier)
            var expiresInSeconds = tokenResult.ExpiresIn > 60 ? tokenResult.ExpiresIn - 60 : tokenResult.ExpiresIn / 2;
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(Math.Max(10, expiresInSeconds))
            };

            _cache.Set(cacheKey, tokenResult.AccessToken, cacheOptions);
            _logger.LogInformation("Successfully acquired and cached M2M service token for {ClientId} (valid for {Duration}s)",
                _options.Value.ClientId, expiresInSeconds);

            return tokenResult.AccessToken;
        }
        finally
        {
            _lock.Release();
        }
    }

    private sealed class OAuthTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; } = 3600;

        [JsonPropertyName("scope")]
        public string? Scope { get; set; }
    }
}

