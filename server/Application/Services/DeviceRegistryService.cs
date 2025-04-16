using Application.Interfaces;

namespace Application.Services;

public interface IDeviceRegistryService
{
    Task<Guid> AssociateDeviceAsync(Guid userId, string browser, string os);
}

public class DeviceRegistryService : IDeviceRegistryService
{
    private readonly IDeviceRegistryRepository _repository;
    private readonly IDeviceService _deviceService;
    private readonly IUserIdentityService _userIdentityService;

    public DeviceRegistryService(IDeviceRegistryRepository repository, IDeviceService deviceService,
        IUserIdentityService userIdentityService)
    {
        _repository = repository;
        _deviceService = deviceService;
        _userIdentityService = userIdentityService;
    }

    public async Task<Guid> AssociateDeviceAsync(Guid userId, string browser, string os)
    {
        if (!await _userIdentityService.ExistsAsync(userId))
        {
            throw new ArgumentException($"User with ID {userId} does not exist", nameof(userId));
        }

        var deviceId = await _deviceService.CreateAsync(browser, os);

        await _repository.SaveAsync(userId, deviceId);

        return deviceId;
    }
}