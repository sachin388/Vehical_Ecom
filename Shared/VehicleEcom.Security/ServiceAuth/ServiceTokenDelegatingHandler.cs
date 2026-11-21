using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;

namespace VehicleEcom.Security.ServiceAuth;

public sealed class ServiceTokenDelegatingHandler : DelegatingHandler
{
    private readonly IServiceTokenClient _tokenClient;
    private readonly string? _scope;
    private readonly ILogger<ServiceTokenDelegatingHandler> _logger;

    public ServiceTokenDelegatingHandler(
        IServiceTokenClient tokenClient,
        ILogger<ServiceTokenDelegatingHandler> logger,
        string? scope = null)
    {
        _tokenClient = tokenClient;
        _logger = logger;
        _scope = scope;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        try
        {
            var token = await _tokenClient.GetServiceTokenAsync(_scope, cancellationToken);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to attach service bearer token to request: {RequestUri}", request.RequestUri);
            throw;
        }

        return await base.SendAsync(request, cancellationToken);
    }
}

