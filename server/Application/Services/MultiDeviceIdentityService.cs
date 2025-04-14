using Application.Interfaces;

namespace Application.Services;

public interface IMultiDeviceIdentityService
{
    Task<(string accessToken, string refreshToken)> EstablishIdentityAsync(Guid userId, string browser, string os);
}


public class MultiDeviceIdentityService : IMultiDeviceIdentityService
{
    private readonly IMultiDeviceIdentityRepository _repository;
    private readonly IDeviceRegistryService _deviceRegistryService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IJwtService _jwtService;
    private readonly IUserIdentityService _userIdentityService;

    public MultiDeviceIdentityService(
        IMultiDeviceIdentityRepository repository,
        IDeviceRegistryService deviceRegistryService, 
        IRefreshTokenService refreshTokenService, 
        IJwtService jwtService, 
        IUserIdentityService userIdentityService)
    {
        _repository = repository;
        _deviceRegistryService = deviceRegistryService;
        _refreshTokenService = refreshTokenService;
        _jwtService = jwtService;
        _userIdentityService = userIdentityService;
    }

    public async Task<(string accessToken, string refreshToken)> EstablishIdentityAsync(Guid userId, string browser, string os)
    { 
        var deviceId = await _deviceRegistryService.AssociateDeviceAsync(userId, browser, os);

        var userInfo = await _userIdentityService.GetAsync(userId);

        var refreshToken = await _refreshTokenService.GenerateAsync();

        var validationResult = await _refreshTokenService.TryValidateAsync(refreshToken);
       
        await _repository.SaveIdentityAsync(userId, deviceId, validationResult.tokenId);

        var accessToken = _jwtService.Generate(
            userId.ToString(),
            userInfo.Email,
            "user");

        return (accessToken, refreshToken);
    }
}