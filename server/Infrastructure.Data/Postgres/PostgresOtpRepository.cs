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

    public async Task<Guid> GenerateAsync(int code)
    {
        
        var otp = new Onetimepassword
        {
            Code = code
            /*
             * Databasen sætter flere default værdier:
             * Generer et UUID
             * CreatedAt bruger (CURRENT_TIMESTAMP)
             * ExpiresAt bruger (CURRENT_TIMESTAMP + 15) så vi sikrer at Otp koden udløber efter 15 min.
             * IsUsed er som default sat til (false)
             */
        };
        
        await _dbContext.Onetimepasswords.AddAsync(otp);
        await _dbContext.SaveChangesAsync();

        return otp.Id;
    }

   
}