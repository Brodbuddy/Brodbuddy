using Application.Interfaces;
using Core.Entities;
using Core.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data.Postgres;

public class PostgresOtpRepository : IOtpRepository
{
    private readonly PostgresDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public PostgresOtpRepository(PostgresDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<Guid> SaveAsync(int code)
    {
        var now = _timeProvider.Now();
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
        var now = _timeProvider.Now();

        var otp = await _dbContext.OneTimePasswords.FirstOrDefaultAsync(otp => otp.Id == id && otp.Code == code);

        if (otp == null)
        {
            return false; 
        }

        if (otp.IsUsed)
        {
            return false;
        }

        if (otp.ExpiresAt < now)
        {
            return false; 
        }

        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> MarkAsUsedAsync(Guid id)
    {
        var otp = await _dbContext.OneTimePasswords.FirstOrDefaultAsync(otp => otp.Id == id);

        if (otp == null)
        {
            return false;
        }

        otp.IsUsed = true;
        
        await _dbContext.SaveChangesAsync();
        return true;
    }
}