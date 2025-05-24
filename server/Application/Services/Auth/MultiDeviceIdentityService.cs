using Application.Interfaces;
using Application.Interfaces.Data;
using Application.Interfaces.Data.Repositories;
using Application.Interfaces.Data.Repositories.Auth;
using Application.Models.DTOs;
using Core.Entities;

namespace Application.Services.Auth;

public interface IMultiDeviceIdentityService
{
    Task<(string accessToken, string refreshToken)> EstablishIdentityAsync(Guid userId, DeviceDetails deviceDetails);
    Task<(string accessToken, string refreshToken)> RefreshIdentityAsync(string refreshToken);
}

public class MultiDeviceIdentityService : IMultiDeviceIdentityService
{
    private readonly IMultiDeviceIdentityRepository _repository;
    private readonly IDeviceRegistryService _deviceRegistryService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IJwtService _jwtService;
    private readonly IUserIdentityService _userIdentityService;
    private readonly IUserRoleService _userRoleService;
    private readonly ITransactionManager _transactionManager;

    public MultiDeviceIdentityService(
        IMultiDeviceIdentityRepository repository,
        IDeviceRegistryService deviceRegistryService,
        IRefreshTokenService refreshTokenService,
        IJwtService jwtService,
        IUserIdentityService userIdentityService,
        IUserRoleService userRoleService,
        ITransactionManager transactionManager)
    {
        _repository = repository;
        _deviceRegistryService = deviceRegistryService;
        _refreshTokenService = refreshTokenService;
        _jwtService = jwtService;
        _userIdentityService = userIdentityService;
        _userRoleService = userRoleService;
        _transactionManager = transactionManager;
    }

    public async Task<(string accessToken, string refreshToken)> EstablishIdentityAsync(Guid userId,
        DeviceDetails deviceDetails)
    {
        if (Guid.Empty == userId) throw new ArgumentException("UserId cannot be empty");
        ArgumentNullException.ThrowIfNull(deviceDetails);
        
        return await _transactionManager.ExecuteInTransactionAsync(async () =>
        {
            var deviceId = await _deviceRegistryService.AssociateDeviceAsync(userId, deviceDetails);
            var userInfo = await _userIdentityService.GetAsync(userId);

            var (refreshToken, tokenId) = await _refreshTokenService.GenerateAsync();

            var roles = await _userRoleService.GetUserRolesAsync(userId);
            var firstRole = roles.FirstOrDefault();
            await _repository.SaveIdentityAsync(userId, deviceId, tokenId);
            
            var accessToken = _jwtService.Generate(userId.ToString(), userInfo.Email, firstRole?.Name ?? Role.Member);
            
            return (accessToken, refreshToken);
        });
    }

    public async Task<(string accessToken, string refreshToken)> RefreshIdentityAsync(string refreshToken)
    {
        return await _transactionManager.ExecuteInTransactionAsync(async () =>
        {
            var validateResult = await _refreshTokenService.TryValidateAsync(refreshToken);
            if (!validateResult.isValid) throw new InvalidOperationException("Token context not found or revoked");

            var tokenContext = await _repository.GetAsync(validateResult.tokenId);
            if (tokenContext == null) throw new InvalidOperationException("Failed to rotate refresh token");

            var (newRefreshToken, newTokenId) = await _refreshTokenService.RotateAsync(refreshToken);

            var roles = await _userRoleService.GetUserRolesAsync(tokenContext.UserId);
            var firstRole = roles.FirstOrDefault();
            if (string.IsNullOrEmpty(newRefreshToken))
            {
                throw new InvalidOperationException("Failed to rotate refresh token");
            }

            await _refreshTokenService.RevokeAsync(refreshToken);

            await _repository.RevokeTokenContextAsync(validateResult.tokenId);
            
            var accessToken = _jwtService.Generate(tokenContext.UserId.ToString(), tokenContext.User.Email, firstRole?.Name ?? Role.Member);
            
            await _repository.SaveIdentityAsync(
                tokenContext.UserId,
                tokenContext.DeviceId,
                newTokenId);
            
            return (accessToken, newRefreshToken);
        });
    }
}