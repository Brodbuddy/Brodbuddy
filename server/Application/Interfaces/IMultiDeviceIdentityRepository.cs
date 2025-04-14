using Core.Entities;

namespace Application.Interfaces;

public interface IMultiDeviceIdentityRepository
{
    Task SaveIdentityAsync(Guid userId, Guid deviceId, Guid refreshToken);
    Task<bool> RevokeTokenContextAsync(Guid refreshTokenId);
    Task<TokenContext?> GetTokenContextByRefreshTokenIdAsync(Guid refreshTokenId);
}