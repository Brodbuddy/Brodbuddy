namespace Application.Interfaces;

public interface IMultiDeviceIdentityRepository
{
    Task SaveIdentityAsync(Guid userId, Guid deviceId, Guid refreshToken);
}