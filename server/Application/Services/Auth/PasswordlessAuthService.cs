using Application.Interfaces;
using Application.Interfaces.Data;
using Application.Models.DTOs;
using Core.Entities;

namespace Application.Services.Auth;

public interface IPasswordlessAuthService
{
    Task<bool> InitiateLoginAsync(string email);
    Task<(string accessToken, string refreshToken)> CompleteLoginAsync(string email, int code, DeviceDetails deviceDetails);
    Task<(string accessToken, string refreshToken)> RefreshTokenAsync(string? refreshToken);
    Task<(string email, string role)> UserInfoAsync(Guid userId);
}

public class PasswordlessAuthService : IPasswordlessAuthService
{
    private readonly IIdentityVerificationService _identityVerificationService;
    private readonly IMultiDeviceIdentityService _multiDeviceIdentityService;
    private readonly IUserIdentityService _userIdentityService;
    private readonly IUserRoleService _userRoleService;
    private readonly ITransactionManager _transactionManager;

    public PasswordlessAuthService(
        IIdentityVerificationService identityVerificationService,
        IMultiDeviceIdentityService multiDeviceIdentityService, 
        IUserIdentityService userIdentityService,
        IUserRoleService userRoleService,
        ITransactionManager transactionManager)
    {
        _identityVerificationService = identityVerificationService;
        _multiDeviceIdentityService = multiDeviceIdentityService;
        _userIdentityService = userIdentityService;
        _userRoleService = userRoleService;
        _transactionManager = transactionManager;
    }

    public async Task<bool> InitiateLoginAsync(string email)
    {
        return await _identityVerificationService.SendCodeAsync(email);
    }

    public async Task<(string accessToken, string refreshToken)> CompleteLoginAsync(string email, int code, DeviceDetails deviceDetails)
    {
        return await _transactionManager.ExecuteInTransactionAsync(async () =>
        {
            var (verified, userId) = await _identityVerificationService.TryVerifyCodeAsync(email, code);
            if (!verified) throw new ArgumentException("Invalid verification code");

            return await _multiDeviceIdentityService.EstablishIdentityAsync(userId, deviceDetails);
        });
    }
    
    public async Task<(string accessToken, string refreshToken)> RefreshTokenAsync(string? refreshToken)
    {
        return await _transactionManager.ExecuteInTransactionAsync(async () =>
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);
            return await _multiDeviceIdentityService.RefreshIdentityAsync(refreshToken);
        });
    }

    public async Task<(string email, string role)> UserInfoAsync(Guid userId)
    {
        return await _transactionManager.ExecuteInTransactionAsync(async () =>
        {
            var user = await _userIdentityService.GetAsync(userId);
            var roles = await _userRoleService.GetUserRolesAsync(userId);
            var firstRole = roles.FirstOrDefault();
            return (email: user.Email, role: firstRole?.Name ?? Role.Member);
        });
    }
}