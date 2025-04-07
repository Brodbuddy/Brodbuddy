using Application.Interfaces;
using Core.Entities;

namespace Application.Services;

public interface IDeviceService
{
    Task<Guid> CreateAsync(string browser, string os);
    Task<Device> GetByIdAsync(Guid id);
    Task<IEnumerable<Device>> GetByIdsAsync(IEnumerable<Guid> ids);
    Task<bool> ExistsAsync(Guid id);
    Task<bool> UpdateLastSeenAsync(Guid id);
    Task<bool> DisableAsync(Guid id);
}

public class DeviceService : IDeviceService
{
    private readonly IDeviceRepository _repository;
    private readonly TimeProvider _timeProvider;

    public DeviceService(IDeviceRepository repository, TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async Task<Guid> CreateAsync(string browser, string os)
    {
        if (string.IsNullOrWhiteSpace(browser))
        {
            throw new ArgumentException("Browser cannot be null or empty", nameof(browser));
        }
        if (string.IsNullOrWhiteSpace(os))
        {
            throw new ArgumentException("Operating system cannot be null or empty", nameof(os));
        }

        
        browser = browser.Trim().ToLowerInvariant();
        os = os.Trim().ToLowerInvariant();
        
        string name = $"{os}_{browser}";
        var device = new Device
        {
            Browser = browser,
            Os = os,
            Name = name
        };

        return await _repository.SaveAsync(device);

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