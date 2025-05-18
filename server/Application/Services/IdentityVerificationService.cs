using Application.Interfaces.Communication.Mail;
using Application.Interfaces.Data.Repositories;

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
    private readonly IUserRoleService _userRoleService;
    private readonly IEmailSender _emailService;
    private readonly IIdentityVerificationRepository _identityVerificationRepository;
    
    public IdentityVerificationService(
        IOtpService otpService,
        IUserIdentityService userIdentityService,
        IUserRoleService userRoleService,
        IEmailSender emailService,
        IIdentityVerificationRepository identityVerificationRepository)
    {
        _otpService = otpService;
        _userIdentityService = userIdentityService;
        _userRoleService = userRoleService;
        _identityVerificationRepository = identityVerificationRepository;
        _emailService = emailService;
    }
    
    public async Task<bool> SendCodeAsync(string email)
    {
        var userId = await _userIdentityService.CreateAsync(email);
        
        var userRoles = await _userRoleService.GetUserRolesAsync(userId);
        if (!userRoles.Any()) await _userRoleService.AssignRoleAsync(userId, Core.Entities.Role.Member);

        var (otpId, code) = await _otpService.GenerateAsync();

        await _identityVerificationRepository.CreateAsync(userId, otpId);

        var emailContent = "Your verification code is: " + code;
        return await _emailService.SendEmailAsync(email, "Verification Code", emailContent);
    }

    public async Task<(bool verified, Guid userId)> TryVerifyCodeAsync(string email, int code)
    {
        var user = await _userIdentityService.GetAsync(email);

        var verificationContext = await _identityVerificationRepository.GetLatestAsync(user.Id);
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
}