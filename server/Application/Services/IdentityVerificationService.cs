using Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.Services;

public interface IIdentityVerificationService
{
    Task<bool> SendCodeAsync(string email);
    Task<(bool verified, Guid userId)> TryVerifyCodeAsync(string email, int code);
}

public class IdentityVerificationService : IIdentityVerificationService
{
    private readonly IOtpService _otpService;
    private readonly IUserIdentityService _userIdentityService;
    private readonly IEmailSender _emailService;
    private readonly IIdentityVerificationRepository _identityVerificationRepository;
    private readonly ILogger<IdentityVerificationService> _logger;

    public IdentityVerificationService(
        IOtpService otpService,
        IUserIdentityService userIdentityService,
        IEmailSender emailService,
        IIdentityVerificationRepository identityVerificationRepository,
        ILogger<IdentityVerificationService> logger)
    {
        _otpService = otpService;
        _userIdentityService = userIdentityService;
        _identityVerificationRepository = identityVerificationRepository;
        _emailService = emailService;
        _logger = logger;
    }


    public async Task<bool> SendCodeAsync(string email)
    {
        try
        {
            var userId = await _userIdentityService.CreateAsync(email);

            var code = await _otpService.GenerateAsync();

            var generatedOtp = await _otpService.GetLatestAsync();
            if (generatedOtp == null)
            {
                _logger.LogError("Failed to get the generated OTP for user {UserId}", userId);
                return false;
            }

            await _identityVerificationRepository.CreateAsync(userId, generatedOtp.Id);

            var emailContent = $"Your verification code is: {code}";
            return await _emailService.SendEmailAsync(email, "Verification Code", emailContent);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error occurred while sending verification code to {Email}", email);
            return false;
        }
    }

    public async Task<(bool verified, Guid userId)> TryVerifyCodeAsync(string email, int code)
    {
        try
        {
            var user = await _userIdentityService.GetAsync(email);

            var verificationContext = await _identityVerificationRepository.GetLatestByUserIdAsync(user.Id);
            if (verificationContext == null)
            {
                return (false, Guid.Empty);
            }

            if (!await _otpService.IsValidAsync(verificationContext.OtpId, code))
            {
                return (false, Guid.Empty);
            }

            await _otpService.MarkAsUsedAsync(verificationContext.OtpId);

            return (true, user.Id);
        }
        catch (Exception)
        {
            _logger.LogError("Error occurred while verifying code for {Email}", email);
            return (false, Guid.Empty);
        }
    }
}