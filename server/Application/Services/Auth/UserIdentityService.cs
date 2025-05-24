using System.Net.Mail;
using Application.Interfaces.Data.Repositories;
using Application.Interfaces.Data.Repositories.Auth;
using Core.Entities;

namespace Application.Services.Auth;

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

    public UserIdentityService(IUserIdentityRepository repository)
    {
        _repository = repository;
    }

    public async Task<Guid> CreateAsync(string email)
    {
        if (!IsValidEmail(email))
        {
            throw new ArgumentException("Invalid email format", nameof(email));
        }

        email = email.Trim().ToLowerInvariant();

        if (!await ExistsAsync(email)) return await _repository.SaveAsync(email);
        
        var existingUser = await _repository.GetAsync(email);
        return existingUser!.Id;
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

        var user = await _repository.GetAsync(id);
        if (user == null)
        {
            throw new ArgumentException($"User with ID {id} not found");
        }

        return user;
    }

    public async Task<User> GetAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email cannot be null or empty", nameof(email));
        }

        email = email.Trim().ToLowerInvariant();

        var user = await _repository.GetAsync(email);
        if (user == null)
        {
            throw new ArgumentException($"User with email {email} not found");
        }

        return user;
    }
    
    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email cannot be null or empty", nameof(email));
        }

        if (!MailAddress.TryCreate(email, out _))
        {
            return false;
        }
        
        var parts = email.Split('@');
        
        return parts[1].Contains('.');
    }
}