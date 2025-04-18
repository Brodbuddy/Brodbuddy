using Infrastructure.Data.Postgres;
using Microsoft.EntityFrameworkCore;
using SharedTestDependencies;

namespace Infrastructure.Data.Tests;

public abstract class RepositoryTestBase : IAsyncLifetime
{
    protected readonly PostgresFixture _fixture;
    protected PostgresDbContext _dbContext;
    
    protected RepositoryTestBase(PostgresFixture fixture)
    {
        _fixture = fixture;
        
        var options = new DbContextOptionsBuilder<PostgresDbContext>().UseNpgsql(_fixture.ConnectionString).Options;
        _dbContext = new PostgresDbContext(options);
    }
    
    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
    }

    public Task DisposeAsync()
    {
        _dbContext.Dispose();
        return Task.CompletedTask;
    }
}