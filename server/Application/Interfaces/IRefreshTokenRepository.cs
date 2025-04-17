namespace Application.Interfaces;

public interface IRefreshTokenRepository
{
    Task<(string token, Guid tokenId)> CreateAsync(string token, DateTime expiresAt);
    Task<(bool isValid, Guid tokenId)> TryValidateAsync(string token);
    Task<bool> RevokeAsync(Guid tokenId);
    Task<(string token, Guid tokenId)> RotateAsync(Guid oldTokenId);
}