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
    
    
    public async Task<(string accessToken, string refreshToken)> EstablishIdentityAsync(Guid userId, string browser, string os)
    {
        throw new NotImplementedException();
    }
}