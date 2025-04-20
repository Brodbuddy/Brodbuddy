namespace Application.Services;

public interface IPasswordlessAuthService
{
    Task<bool> InitiateLoginAsync(string email);
    Task<(string accessToken, string refreshToken)> CompleteLoginAsync(string email, int code, string browser, string os);
    Task<(string accessToken, string refreshToken)> RefreshTokenAsync(string refreshToken);
}

public class PasswordlessAuthService : IPasswordlessAuthService
{
    private readonly IIdentityVerificationService _identityVerificationService;
    private readonly IMultiDeviceIdentityService _multiDeviceIdentityService;


    public PasswordlessAuthService(
        IIdentityVerificationService identityVerificationService,
        IMultiDeviceIdentityService multiDeviceIdentityService)
    {
        _identityVerificationService = identityVerificationService;
        _multiDeviceIdentityService = multiDeviceIdentityService;
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
            throw new UnauthorizedAccessException("Invalid verification code");
        }

        return await _multiDeviceIdentityService.EstablishIdentityAsync(userId, browser, os);
    }


    public async Task<(string accessToken, string refreshToken)> RefreshTokenAsync(string refreshToken)
    {
        return await _multiDeviceIdentityService.RefreshIdentityAsync(refreshToken);
    }
}