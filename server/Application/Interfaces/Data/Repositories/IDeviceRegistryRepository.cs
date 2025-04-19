namespace Application.Interfaces.Data.Repositories;

public interface IDeviceRegistryRepository
{
    Task<Guid> SaveAsync(Guid userId, Guid deviceId);
}