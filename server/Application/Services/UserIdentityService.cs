using Application.Interfaces;
using Core.Entities;

namespace Application.Services;

public interface IUserIdentityService
{
    Task<Guid> CreateAsync(string email);
    Task<bool> ExistsAsync(Guid id);
    Task<bool> ExistsAsync(string email);
    Task<User> GetAsync(Guid id);
    Task<User> GetAsync(string email);
}

public class UserIdentityService : IUserIdentityService
{
    private readonly IUserIdentityRepository _repository;
    private readonly TimeProvider _timeProvider;
    
    
    public UserIdentityService(IUserIdentityRepository repository, TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async Task<Guid> CreateAsync(string email)
    {
        return await _repository.CreateAsync(email);
    }

    public Task<bool> ExistsAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    public Task<bool> ExistsAsync(string email)
    {
        throw new NotImplementedException();
    }

    public Task<User> GetAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    public Task<User> GetAsync(string email)
    {
        throw new NotImplementedException();
    }
}