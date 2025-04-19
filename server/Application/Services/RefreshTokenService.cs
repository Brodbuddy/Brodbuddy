using System.Security.Cryptography;
using Application.Interfaces;
using Core.Extensions;


namespace Application.Services;

public interface IRefreshTokenService
{
    Task<(string token, Guid tokenId)> GenerateAsync();
    Task<(bool isValid, Guid tokenId)> TryValidateAsync(string token);
    Task<bool> RevokeAsync(string token);
    Task<(string token, Guid tokenId)> RotateAsync(string token);
}

public class RefreshTokenService : IRefreshTokenService
{
    private readonly IRefreshTokenRepository _repository;
    private readonly TimeProvider _timeProvider;

    public RefreshTokenService(IRefreshTokenRepository repository, TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    private readonly TimeSpan _tokenValidity = TimeSpan.FromDays(30);

    public async Task<(string token, Guid tokenId)> GenerateAsync()
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var expiresAt = _timeProvider.Now().Add(_tokenValidity);
        var result = await _repository.CreateAsync(token, expiresAt);
        return result;
    }

    public async Task<(bool isValid, Guid tokenId)> TryValidateAsync(string token)
    {
        if (string.IsNullOrEmpty(token))
            return (false, Guid.Empty);

        return await _repository.TryValidateAsync(token);
    }

    public async Task<bool> RevokeAsync(string token)
    {
        if (string.IsNullOrEmpty(token))
            return false;

        var validationResult = await TryValidateAsync(token);
        if (!validationResult.isValid)
            return false;

        return await _repository.RevokeAsync(validationResult.tokenId);
    }

    public async Task<(string token, Guid tokenId)> RotateAsync(string token)
    {
        if (string.IsNullOrEmpty(token)) return (string.Empty, Guid.Empty);

        var validationResult = await TryValidateAsync(token);
        
        if (!validationResult.isValid) return (string.Empty, Guid.Empty);

        try
        {
            return await _repository.RotateAsync(validationResult.tokenId);
        }
        catch (InvalidOperationException)
        {
            return (string.Empty, Guid.Empty);
        }
    }
}