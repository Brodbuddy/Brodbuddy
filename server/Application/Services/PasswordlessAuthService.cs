namespace Application.Services;

public interface IPasswordlessAuthService
{
    Task<bool> InitiateLoginAsync(string email);
    Task<(string accessToken, string refreshToken)> CompleteLoginAsync(string email, int code);
    Task<(string accessToken, string refreshToken)> RefreshTokenAsync(string currentToken);
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

    public async Task<(string accessToken, string refreshToken)> CompleteLoginAsync(string email, int code)
    {
        var (verified, userId) = await _identityVerificationService.TryVerifyCodeAsync(email, code);

        if (!verified)
        {
            throw new UnauthorizedAccessException("Ugyldig verifikationskode");
        }

        return await _multiDeviceIdentityService.EstablishIdentityAsync(userId, "browser", "os");
    }


    public async Task<(string accessToken, string refreshToken)> RefreshTokenAsync(string currentToken)
    {
        return await _multiDeviceIdentityService.RefreshIdentityAsync(currentToken);
    }
}