using Core.Entities;

namespace Application.Interfaces.Data.Repositories.Auth;

public interface IMultiDeviceIdentityRepository
{
    Task<Guid> SaveIdentityAsync(Guid userId, Guid deviceId, Guid refreshTokenId);
    Task<bool> RevokeTokenContextAsync(Guid refreshTokenId);
    Task<TokenContext?> GetAsync(Guid refreshTokenId);
}