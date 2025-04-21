using Infrastructure.Data.Persistence;
using Microsoft.EntityFrameworkCore;
using SharedTestDependencies.Database;

namespace Infrastructure.Data.Tests.Bases;

public abstract class RepositoryTestBase : IAsyncLifetime, IAsyncDisposable
{
    private readonly PostgresFixture _fixture;

    protected PgDbContext DbContext { get; }

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
    
    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        GC.SuppressFinalize(this);
    }

    private async ValueTask DisposeAsyncCore()
    {
        await DbContext.DisposeAsync();
    }
    
    Task IAsyncLifetime.DisposeAsync()
    {
        return DisposeAsync().AsTask();
    }
}