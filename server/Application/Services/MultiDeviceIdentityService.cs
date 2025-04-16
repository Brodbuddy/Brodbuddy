using Application.Interfaces;

namespace Application.Services;

public interface IMultiDeviceIdentityService
{
    Task<(string accessToken, string refreshToken)> EstablishIdentityAsync(Guid userId, string browser, string os);
    Task<(string accessToken, string refreshToken)> RefreshIdentityAsync(string refreshToken);
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

    public async Task<(string accessToken, string refreshToken)> EstablishIdentityAsync(Guid userId, string browser,
        string os)
    {
        var deviceId = await _deviceRegistryService.AssociateDeviceAsync(userId, browser, os);

        var userInfo = await _userIdentityService.GetAsync(userId);

        var (refreshToken, tokenId) = await _refreshTokenService.GenerateAsync();

        await _repository.SaveIdentityAsync(userId, deviceId, tokenId);

        var accessToken = _jwtService.Generate(
            userId.ToString(),
            userInfo.Email,
            "user");

        return (accessToken, refreshToken);
    }

    public async Task<(string accessToken, string refreshToken)> RefreshIdentityAsync(string refreshToken)
    {
        var validateResult = await _refreshTokenService.TryValidateAsync(refreshToken);
        if (!validateResult.isValid) throw new InvalidOperationException("Token context not found or revoked");

        var tokenContext = await _repository.GetAsync(validateResult.tokenId);

        var (newRefreshToken, newTokenId) = await _refreshTokenService.RotateAsync(refreshToken);

        if (string.IsNullOrEmpty(newRefreshToken))
        {
            throw new InvalidOperationException("Failed to rotate refresh token");
        }

        await _refreshTokenService.RevokeAsync(refreshToken);

        await _repository.RevokeTokenContextAsync(validateResult.tokenId);

        await _repository.SaveIdentityAsync(
            tokenContext.UserId,
            tokenContext.DeviceId,
            newTokenId);

        var accessToken = _jwtService.Generate(
            tokenContext.UserId.ToString(),
            tokenContext.User.Email,
            "user");

        return (accessToken, newRefreshToken);
    }
}