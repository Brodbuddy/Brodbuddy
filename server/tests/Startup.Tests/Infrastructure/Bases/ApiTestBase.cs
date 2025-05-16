using System.Net.Http.Headers;
using Application.Interfaces.Communication.Mail;
using Application.Services;
using Infrastructure.Data.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Startup.Tests.Infrastructure.Extensions;
using Startup.Tests.Infrastructure.Factories;
using Startup.Tests.Infrastructure.Fakes;
using Startup.Tests.Infrastructure.Fixtures;
using Startup.Tests.Infrastructure.Lifecycle;
using Startup.Tests.Infrastructure.TestClients;
using Xunit.Abstractions;

namespace Startup.Tests.Infrastructure.Bases;

public abstract class ApiTestBase : IAsyncLifetime, IDisposable
{
    protected StartupTestFixture Fixture { get; }
    protected ITestOutputHelper Output { get; }
    protected CustomWebApplicationFactory Factory { get; }
    private bool _disposed;

    static ApiTestBase()
    {
        // Starter watchdog med 60s timeout.
        // Dette er en fallback mekanisme i så fald at vores test tracking (se TestTracker) fejler
        ProcessWatchdog.StartWatchdog(60); 
    }

    protected ApiTestBase(StartupTestFixture fixture, ITestOutputHelper output)
    {
        Fixture = fixture;
        Output = output;
        Factory = new CustomWebApplicationFactory(fixture, output);
        
        // Alle tests der extender ApiTestBase bliver registreret her
        TestTracker.RegisterActiveTest();
    }
    
    protected T GetService<T>() where T : notnull
    {
        return Factory.Services.GetRequiredService<T>();
    }
    
    protected PgDbContext GetDbContext()
    {
        using var scope = Factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<PgDbContext>();
    }
    
    protected FakeEmailSender GetEmailSender()
    {
        return Factory.Services.GetRequiredService<IEmailSender>() as FakeEmailSender 
               ?? throw new InvalidOperationException("FakeEmailSender is not registered properly");
    }
    
    protected async Task<T> WithDbContextAsync<T>(Func<PgDbContext, Task<T>> action)
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PgDbContext>();
        return await action(dbContext);
    }
    
    protected async Task WithDbContextAsync(Func<PgDbContext, Task> action)
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PgDbContext>();
        await action(dbContext);
    }

    public virtual Task InitializeAsync() => Task.CompletedTask;

    public virtual async Task DisposeAsync()
    {
        await Fixture.ResetAsync();
    }
    
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            try
            {
                Factory.Dispose();
            }
            catch (Exception ex)
            {
                Output.WriteLine($"Non-fatal error during disposal: {ex.Message}");
            }
            
            TestTracker.UnregisterActiveTest();
        }

        _disposed = true;
    }
}