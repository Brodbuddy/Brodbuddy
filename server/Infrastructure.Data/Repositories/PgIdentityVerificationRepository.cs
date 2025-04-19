using Application.Interfaces;
using Application.Interfaces.Data.Repositories;
using Core.Entities;
using Core.Extensions;
using Infrastructure.Data.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data.Repositories;

public class PgIdentityVerificationRepository : IIdentityVerificationRepository
{
    private readonly PgDbContext _dbContext;
    private readonly TimeProvider _timeProvider;


    public PgIdentityVerificationRepository(PgDbContext dbContext, TimeProvider timeProvider)
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
            CreatedAt = _timeProvider.Now()
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