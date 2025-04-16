using System.Security.Cryptography;
using Application.Interfaces;
using Microsoft.Extensions.Logging;


namespace Application;


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
    private readonly ILogger<RefreshTokenService> _logger;

    public RefreshTokenService(
        IRefreshTokenRepository repository,
        TimeProvider timeProvider,
        ILogger<RefreshTokenService> logger)
    {
        _repository = repository;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    private readonly TimeSpan _tokenValidity = TimeSpan.FromDays(30);

    public async Task<(string token, Guid tokenId)> GenerateAsync()
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var expiresAt = _timeProvider.GetUtcNow().UtcDateTime.Add(_tokenValidity);
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
        if (string.IsNullOrEmpty(token))
            return (string.Empty, Guid.Empty);

        var validationResult = await TryValidateAsync(token);
        if (!validationResult.isValid)
            return (string.Empty, Guid.Empty);
            
        try
        {
            var result = await _repository.RotateAsync(validationResult.tokenId);
            return result;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to rotate refresh token. Token ID: {TokenId}", validationResult.tokenId);
            return (string.Empty, Guid.Empty);
        }
    }
}