using Core.Entities;

namespace Application.Interfaces;

public interface IMultiDeviceIdentityRepository
{
    Task<Guid> SaveIdentityAsync(Guid userId, Guid deviceId, Guid refreshToken);
    Task<bool> RevokeTokenContextAsync(Guid refreshTokenId);
    Task<TokenContext?> GetAsync(Guid refreshTokenId);
}