using Core.Entities;

namespace Application.Interfaces.Data.Repositories;

public interface IIdentityVerificationRepository
{
    Task<Guid> CreateAsync(Guid userId, Guid otpId);
    Task<VerificationContext?> GetLatestAsync(Guid userId);
}