using Application.Interfaces;
using Core.Entities;
using Core.Extensions;

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
        ArgumentException.ThrowIfNullOrWhiteSpace(browser, nameof(browser));
        ArgumentException.ThrowIfNullOrWhiteSpace(os, nameof(os));
        
        browser = browser.Trim().ToLowerInvariant();
        os = os.Trim().ToLowerInvariant();

        string name = $"{browser}_{os}";
        var device = new Device
        {
            Browser = browser,
            Os = os,
            Name = name
        };

        return await _repository.SaveAsync(device);
    }

    public async Task<Device> GetByIdAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Device ID cannot be empty", nameof(id));
        }

        return await _repository.GetAsync(id);
    }

    public async Task<IEnumerable<Device>> GetByIdsAsync(IEnumerable<Guid> ids)
    {
        ArgumentNullException.ThrowIfNull(ids);

        var validIds = ids.Where(id => id != Guid.Empty).ToList();

        if (validIds.Count == 0) return [];

        return await _repository.GetByIdsAsync(validIds);
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        if (id == Guid.Empty) throw new ArgumentException("Device ID cannot be empty", nameof(id));

        return await _repository.ExistsAsync(id);
    }

    public async Task<bool> UpdateLastSeenAsync(Guid id)
    {
        if (id == Guid.Empty) throw new ArgumentException("Device ID cannot be empty", nameof(id));

        var now = _timeProvider.Now();
        return await _repository.UpdateLastSeenAsync(id, now);
    }

    public async Task<bool> DisableAsync(Guid id)
    {
        if (id == Guid.Empty) throw new ArgumentException("Device ID cannot be empty", nameof(id));

        return await _repository.DisableAsync(id);
    }
}