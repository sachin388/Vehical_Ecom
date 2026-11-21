namespace VehicleEcom.Security.ServiceAuth;

public sealed class ServiceTokenOptions
{
    public string TokenEndpoint { get; set; } = "http://localhost:5149/connect/token";
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string DefaultScope { get; set; } = "internal.all";
}

