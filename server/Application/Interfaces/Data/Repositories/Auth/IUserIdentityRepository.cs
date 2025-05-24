using Core.Entities;

namespace Application.Interfaces.Data.Repositories.Auth;

public interface IUserIdentityRepository
{
    Task<Guid> SaveAsync(string email);
    Task<bool> ExistsAsync(Guid id);
    Task<bool> ExistsAsync(string email);
    Task<User?> GetAsync(Guid id);
    Task<User?> GetAsync(string email);
}