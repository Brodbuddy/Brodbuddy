using Application.Interfaces;
using Application.Interfaces.Data.Repositories;

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
    private readonly ITransactionManager _transactionManager;

    public DeviceRegistryService(IDeviceRegistryRepository repository, IDeviceService deviceService,
        IUserIdentityService userIdentityService, ITransactionManager transactionManager)
    {
        _repository = repository;
        _deviceService = deviceService;
        _userIdentityService = userIdentityService;
        _transactionManager = transactionManager;
    }

    public async Task<Guid> AssociateDeviceAsync(Guid userId, string browser, string os)
    {
        return await _transactionManager.ExecuteInTransactionAsync(async () =>
        {
            if (!await _userIdentityService.ExistsAsync(userId))
            {
                throw new ArgumentException($"User with ID {userId} does not exist");
            }

            var deviceId = await _deviceService.CreateAsync(browser, os);

            await _repository.SaveAsync(userId, deviceId);

            return deviceId;
        });
    }
}