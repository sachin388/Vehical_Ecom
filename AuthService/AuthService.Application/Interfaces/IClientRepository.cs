using AuthService.Domain.Entities;

namespace AuthService.Application.Interfaces
{
    public interface IClientRepository
    {
        Task<Client?> GetByClientIdAsync(string clientId);
        Task<bool> ValidateClientCredentialsAsync(string clientId, string clientSecret);
    }
}

