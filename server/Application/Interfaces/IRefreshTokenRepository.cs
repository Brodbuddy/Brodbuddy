namespace Application.Interfaces;

public interface IRefreshTokenRepository
{
    Task<Guid> CreateAsync(string token, DateTime expiresAt);
    Task<(bool isValid, Guid tokenId)> TryValidateAsync(string token);
    Task<bool> RevokeAsync(Guid tokenId);  
    Task<string> RotateAsync(Guid oldTokenId);
}