using System.Security.Cryptography;
using System.Text;
using Application.Interfaces.Data.Repositories;
using Application.Models;
using Core.Exceptions;

namespace Application.Services;

public interface IDeviceRegistryService
{
    Task<Guid> AssociateDeviceAsync(Guid userId, DeviceDetails deviceDetails);
}

public class DeviceRegistryService : IDeviceRegistryService
{
    private const int MaxDevices = 5;
    private readonly IDeviceRegistryRepository _repository;
    private readonly IDeviceService _deviceService;
    private readonly IUserIdentityService _userIdentityService;

    public DeviceRegistryService(IDeviceRegistryRepository repository,
                                 IDeviceService deviceService,
                                 IUserIdentityService userIdentityService)
    {
        _repository = repository;
        _deviceService = deviceService;
        _userIdentityService = userIdentityService;
    }

    public async Task<Guid> AssociateDeviceAsync(Guid userId, DeviceDetails deviceDetails)
    {
        if (!await _userIdentityService.ExistsAsync(userId))
        {
            throw new ArgumentException($"User with ID {userId} does not exist", nameof(userId));
        }

        var fingerprint = GenerateFingerprint(userId, deviceDetails);
        var existingDeviceId = await _repository.GetDeviceIdByFingerprintAsync(userId, fingerprint);
        if (existingDeviceId.HasValue)
        {
            await _deviceService.UpdateLastSeenAsync(existingDeviceId.Value);
            return existingDeviceId.Value;
        }
        
        await ValidateDeviceLimitAsync(userId);
        
        var deviceId = await _deviceService.CreateAsync(deviceDetails);

        await _repository.SaveAsync(userId, deviceId, fingerprint);
        
        return deviceId;
    }
    
    private static string GenerateFingerprint(Guid userId, DeviceDetails deviceDetails)
    {
        var input = $"{userId}-{deviceDetails.Browser}-{deviceDetails.Os}-{deviceDetails.UserAgent}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(hash);
    }
    
    public async Task ValidateDeviceLimitAsync(Guid userId)
    {
        var deviceCount = await _repository.CountByUserIdAsync(userId);
        
        if (deviceCount >= MaxDevices) 
            throw new BusinessRuleViolationException(
                "DeviceLimit", 
                "Maximum number of devices reached. Please remove an existing device before adding a new one."
            );
    }
}