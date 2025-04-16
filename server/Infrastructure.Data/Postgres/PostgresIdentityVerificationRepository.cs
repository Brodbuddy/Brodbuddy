using Application.Interfaces;
using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Data.Postgres;

public class PostgresIdentityVerificationRepository : IIdentityVerificationRepository
{
    private readonly PostgresDbContext _dbContext;
    private readonly TimeProvider _timeProvider;


    public PostgresIdentityVerificationRepository(PostgresDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<Guid> CreateAsync(Guid userId, Guid otpId)
    {
        var verificationContext = new VerificationContext
        {
            UserId = userId,
            OtpId = otpId,
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime
        };

        await _dbContext.VerificationContexts.AddAsync(verificationContext);
        await _dbContext.SaveChangesAsync();

        return verificationContext.Id;
    }

    public async Task<VerificationContext?> GetLatestAsync(Guid userId)
    {
        var context = await _dbContext.VerificationContexts
            .Include(vc => vc.Otp)
            .Include(vc => vc.User)
            .Where(vc => vc.UserId == userId)
            .OrderByDescending(vc => vc.CreatedAt)
            .FirstOrDefaultAsync();

        return context;
    }
}