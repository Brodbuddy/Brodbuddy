using Application.Interfaces;
using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Data.Postgres;

public class PostgresIdentityVerificationRepository : IIdentityVerificationRepository
{
    private readonly PostgresDbContext _dbContext;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<PostgresIdentityVerificationRepository> _logger;

    public PostgresIdentityVerificationRepository(
        PostgresDbContext dbContext,
        TimeProvider timeProvider,
        ILogger<PostgresIdentityVerificationRepository> logger)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<Guid> CreateAsync(Guid userId, Guid otpId)
    {
       
            var user = await _dbContext.Users.FindAsync(userId);
            if (user == null)
            {
                _logger.LogError("User with ID {UserId} not found", userId);
                throw new InvalidOperationException($"User with ID {userId} not found");
            }

            var otp = await _dbContext.OneTimePasswords.FindAsync(otpId);
            if (otp == null)
            {
                _logger.LogError("OTP with ID {OtpId} not found", otpId);
                throw new InvalidOperationException($"OTP with ID {otpId} not found");
            }

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