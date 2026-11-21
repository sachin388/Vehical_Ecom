namespace VehicleEcom.Security.ServiceAuth;

public interface IServiceTokenClient
{
    Task<string> GetServiceTokenAsync(string? scope = null, CancellationToken cancellationToken = default);
}

