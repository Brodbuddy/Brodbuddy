using Core.Entities;

namespace Application.Services;

public interface IDeviceService
{
    Task<Guid> CreateAsync(string name, string browser, string os);
    Task<Device> GetByIdAsync(Guid id);
    Task<IEnumerable<Device>> GetByIdsAsync(IEnumerable<Guid> ids);
    Task<bool> ExistsAsync(Guid id);
    Task<bool> UpdateLastSeenAsync(Guid id);
    Task<bool> DisableAsync(Guid id);
}

public class DeviceService : IDeviceService
{
    public Task<Guid> CreateAsync(string name, string browser, string os)
    {
        throw new NotImplementedException();
    }

    public Task<Device> GetByIdAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<Device>> GetByIdsAsync(IEnumerable<Guid> ids)
    {
        throw new NotImplementedException();
    }

    public Task<bool> ExistsAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    public Task<bool> UpdateLastSeenAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    public Task<bool> DisableAsync(Guid id)
    {
        throw new NotImplementedException();
    }
}