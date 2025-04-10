namespace Application.Interfaces;

public interface IDeviceRegistryRepository
{
    Task<Guid> SaveAsync(Guid userId, Guid deviceId);
}