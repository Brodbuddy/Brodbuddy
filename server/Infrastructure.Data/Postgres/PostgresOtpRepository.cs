using Application.Interfaces;
using Core.Entities;

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
}