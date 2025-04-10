using Application.Interfaces;
using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Data.Postgres;

public class IdentityVerificationRepository : IIdentityVerificationRepository
{
    private readonly PostgresDbContext _dbContext;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<IdentityVerificationRepository> _logger;

    public IdentityVerificationRepository(
        PostgresDbContext dbContext,
        TimeProvider timeProvider,
        ILogger<IdentityVerificationRepository> logger)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<Guid> CreateAsync(Guid userId, Guid otpId)
    {
        try
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
                CreatedAt = _timeProvider.GetUtcNow().UtcDateTime,
                User = user,
                Otp = otp
            };

            await _dbContext.VerificationContexts.AddAsync(verificationContext);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Created verification context {ContextId} for user {UserId}", verificationContext.Id,
                userId);
            return verificationContext.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating verification context for user {UserId} with OTP {OtpId}", userId,
                otpId);
            throw;
        }
        
       
    }

    public async Task<VerificationContext?> GetLatestByUserIdAsync(Guid userId)
    {
        try
        {
            var context = await _dbContext.VerificationContexts
                .Include(vc => vc.Otp)
                .Include(vc => vc.User)
                .Where(vc => vc.UserId == userId)
                .OrderByDescending(vc => vc.CreatedAt)
                .FirstOrDefaultAsync();

            _logger.LogInformation("Retrieved {ContextFound} latest verification context for user {UserId}",
                context != null ? "a" : "no", userId);

            return context;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving latest verification context for user {UserId}", userId);
            throw;
        }
        
        
    }
}