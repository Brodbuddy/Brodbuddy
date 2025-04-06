using System.Net.Mail;
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
        if (string.IsNullOrWhiteSpace(email))
        { 
            throw new ArgumentException("Email cannot be null or empty", nameof(email)); 
        }

        email = email.Trim().ToLowerInvariant();

        if (!IsValidEmail(email))
        {
            throw new ArgumentException("Invalid email format", nameof(email));
        }

        if (await ExistsAsync(email))
        {
            var existingUser = await _repository.GetAsync(email);
            return existingUser.Id;
        }
        
        return await _repository.SaveAsync(email);
    }

    private bool IsValidEmail(string email)
    {
        if (!MailAddress.TryCreate(email, out var _))
            return false;
        
        if (!email.Contains('@') || !email.Contains('.'))
            return false;
        
        var parts = email.Split('@');
        if (parts.Length != 2 || string.IsNullOrEmpty(parts[0]) || string.IsNullOrEmpty(parts[1]))
            return false;
        
        if (!parts[1].Contains('.'))
            return false;
        
        return true;
    }

    public Task<bool> ExistsAsync(Guid id)
    {
        return _repository.ExistsAsync(id);
    }

    public Task<bool> ExistsAsync(string email)
    {
        return _repository.ExistsAsync(email);
    }

    public async Task<User> GetAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("User ID cannot be empty", nameof(id));
        }

        return await _repository.GetAsync(id);
    }

    public async Task<User> GetAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email cannot be null or empty", nameof(email));
        }

        email = email.Trim().ToLowerInvariant();
        return await _repository.GetAsync(email);
    }
}