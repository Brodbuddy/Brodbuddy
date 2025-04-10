namespace Application.Interfaces;

public interface IDeviceRegistryRepository
{
    Task<Guid> SaveAsync(Guid userId, string browser, string os);
}