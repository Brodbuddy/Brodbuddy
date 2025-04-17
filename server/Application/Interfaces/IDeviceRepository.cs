using Core.Entities;

namespace Application.Interfaces;

public interface IDeviceRepository
{
    Task<Guid> SaveAsync(Device device);
    Task<Device> GetAsync(Guid id);
    Task<IEnumerable<Device>> GetByIdsAsync(IEnumerable<Guid> ids);
    Task<bool> ExistsAsync(Guid id);
    Task<bool> UpdateLastSeenAsync(Guid id, DateTime lastSeenTime);
    Task<bool> DisableAsync(Guid id);
}