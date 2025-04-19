using Infrastructure.Data.Persistence;
using Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using SharedTestDependencies.Database;

namespace Infrastructure.Data.Tests.Bases;

public abstract class RepositoryTestBase : IAsyncLifetime
{
    private readonly PostgresFixture _fixture;
    protected readonly PgDbContext DbContext;
    
    protected RepositoryTestBase(PostgresFixture fixture)
    {
        _fixture = fixture;
        
        var options = new DbContextOptionsBuilder<PgDbContext>().UseNpgsql(_fixture.ConnectionString).Options;
        DbContext = new PgDbContext(options);
    }
    
    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
    }

    public Task DisposeAsync()
    {
        DbContext.Dispose();
        return Task.CompletedTask;
    }
}