using Application.Interfaces;

namespace Infrastructure.Data.Postgres;

public class PostgresDeviceRegistryRepository : IDeviceRegistryRepository
{
    private readonly PostgresDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public PostgresDeviceRegistryRepository(PostgresDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public Task<Guid> SaveAsync(Guid userId, string browser, string os)
    {
        throw new NotImplementedException();
    }
}