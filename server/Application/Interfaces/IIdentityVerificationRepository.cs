using Core.Entities;

namespace Application.Interfaces;

public interface IIdentityVerificationRepository
{
    Task<Guid> CreateAsync(Guid userId, Guid otpId);
    Task<VerificationContext?> GetLatestByUserIdAsync(Guid userId);
    
}