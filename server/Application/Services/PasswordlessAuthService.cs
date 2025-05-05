namespace Application.Services;

public interface IPasswordlessAuthService
{
    Task<bool> InitiateLoginAsync(string email);
    Task<(string accessToken, string refreshToken)> CompleteLoginAsync(string email, int code, string browser, string os);
    Task<(string accessToken, string refreshToken)> RefreshTokenAsync(string? refreshToken);
    Task<(string email, string role)> UserInfoAsync(Guid userId);
}

public class PasswordlessAuthService : IPasswordlessAuthService
{
    private readonly IIdentityVerificationService _identityVerificationService;
    private readonly IMultiDeviceIdentityService _multiDeviceIdentityService;
    private readonly IUserIdentityService _userIdentityService;

    public PasswordlessAuthService(IIdentityVerificationService identityVerificationService, IMultiDeviceIdentityService multiDeviceIdentityService, IUserIdentityService userIdentityService)
    {
        _identityVerificationService = identityVerificationService;
        _multiDeviceIdentityService = multiDeviceIdentityService;
        _userIdentityService = userIdentityService;
    }

    public async Task<bool> InitiateLoginAsync(string email)
    {
        return await _identityVerificationService.SendCodeAsync(email);
    }

    public async Task<(string accessToken, string refreshToken)> CompleteLoginAsync(string email, int code, string browser, string os)
    {
        var (verified, userId) = await _identityVerificationService.TryVerifyCodeAsync(email, code);

        if (!verified)
        {
            throw new ArgumentException("Invalid verification code");
        }

        return await _multiDeviceIdentityService.EstablishIdentityAsync(userId, browser, os);
    }


    public async Task<(string accessToken, string refreshToken)> RefreshTokenAsync(string? refreshToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);
        return await _multiDeviceIdentityService.RefreshIdentityAsync(refreshToken);
    }

    public async Task<(string email, string role)> UserInfoAsync(Guid userId)
    {
        var user = await _userIdentityService.GetAsync(userId);
        return (user.Email, "user");
    }
}