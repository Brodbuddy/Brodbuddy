using Application.Interfaces;
using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Data.Postgres;

public class PostgresOtpRepository : IOtpRepository
{
    private readonly PostgresDbContext _dbContext;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<PostgresOtpRepository> _logger;

    public PostgresOtpRepository(PostgresDbContext dbContext, TimeProvider timeProvider,
        ILogger<PostgresOtpRepository> logger)
    {
        _logger = logger;
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<Guid> SaveAsync(int code)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var otp = new OneTimePassword
        {
            Code = code,
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(15),
            IsUsed = false
        };

        await _dbContext.OneTimePasswords.AddAsync(otp);
        await _dbContext.SaveChangesAsync();

        return otp.Id;
    }

    public async Task<bool> IsValidAsync(Guid id, int code)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var otp = await _dbContext.OneTimePasswords
            .FirstOrDefaultAsync(otp => otp.Id == id && otp.Code == code);

        if (otp == null)
        {
            return false; // Otp kan ikke findes i db.
        }

        if (otp.IsUsed)
        {
            return false; // Otp er allerede brugt 
        }

        if (otp.ExpiresAt < now)
        {
            return false; // Otp er udløbet
        }

        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> MarkAsUsedAsync(Guid id)
    {
        var otp = await _dbContext.OneTimePasswords
            .FirstOrDefaultAsync(otp => otp.Id == id);

        if (otp == null)
        {
            return false;
        }

        otp.IsUsed = true;
        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<OneTimePassword?> GetLatestOtpAsync()
    {
        try
        {
            var latestOtp = await _dbContext.OneTimePasswords
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            _logger.LogInformation("Retrieved {Result} latest OTP",
                latestOtp != null ? "a" : "no");

            return latestOtp;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving latest OTP");
            throw;
        }
    }
}