using Application.Interfaces;

namespace Application.Services;

public interface IDeviceRegistryService
{
    Task<Guid> AssociateDeviceAsync(Guid userId, string browser, string os);
}


public class DeviceRegistryService : IDeviceRegistryService
{
    private readonly IDeviceRegistryRepository _repository;
    private readonly TimeProvider _timeProvider;

    public DeviceRegistryService(IDeviceRegistryRepository repository, TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public Task<Guid> AssociateDeviceAsync(Guid userId, string browser, string os)
    {
        throw new NotImplementedException();
    }
}