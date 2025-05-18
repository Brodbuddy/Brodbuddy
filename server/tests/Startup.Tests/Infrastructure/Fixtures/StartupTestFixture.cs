using SharedTestDependencies.Fixtures;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Startup.Tests.Infrastructure.Fixtures;


public class StartupTestFixture : IAsyncLifetime
{
    private readonly IMessageSink _messageSink;
    
    public PostgresFixture Postgres { get; }
    public RedisFixture Redis { get; }
    public VerneMqFixture VerneMq { get; }
    
    public StartupTestFixture(IMessageSink messageSink)
    {
        _messageSink = messageSink;
        Log("Creating container fixtures");
        Postgres = new PostgresFixture(messageSink);
        Redis = new RedisFixture(messageSink);
        VerneMq = new VerneMqFixture(messageSink);
    }
    
    public async Task InitializeAsync()
    {
        Log("Initializing containers...");
        var postgresInitMethod = typeof(PostgresFixture).GetMethod("InitializeAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var redisInitMethod = typeof(RedisFixture).GetMethod("InitializeAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var verneMqInitMethod = typeof(VerneMqFixture).GetMethod("InitializeAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        
        if (postgresInitMethod != null && redisInitMethod != null && verneMqInitMethod != null)
        {
            await Task.WhenAll(
                (Task)postgresInitMethod.Invoke(Postgres, null)!,
                (Task)redisInitMethod.Invoke(Redis, null)!,
                (Task)verneMqInitMethod.Invoke(VerneMq, null)!
            );
        }
        else
        {
            throw new InvalidOperationException("Could not find InitializeAsync methods using reflection");
        }
        
        Log("All containers initialized");
    }
    
    public async Task DisposeAsync()
    {
        Log("Disposing containers...");
        
        var postgresDisposeMethod = typeof(PostgresFixture).GetMethod("DisposeAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var redisDisposeMethod = typeof(RedisFixture).GetMethod("DisposeAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var verneMqDisposeMethod = typeof(VerneMqFixture).GetMethod("DisposeAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            
        if (postgresDisposeMethod != null && redisDisposeMethod != null && verneMqDisposeMethod != null)
        {
            await Task.WhenAll(
                ((ValueTask)postgresDisposeMethod.Invoke(Postgres, null)!).AsTask(),
                ((ValueTask)redisDisposeMethod.Invoke(Redis, null)!).AsTask(),
                ((ValueTask)verneMqDisposeMethod.Invoke(VerneMq, null)!).AsTask()
            );
        }
        
        Log("All containers disposed");
    }
    
    public async Task ResetAsync()
    {
        Log("Resetting services...");
        await Task.Delay(100);
        await Task.WhenAll(
            Postgres.ResetDatabaseAsync(),
            Redis.ResetAsync(),
            VerneMq.ResetAsync()
        );
        Log("All services reset");
    }
    
    private void Log(string message)
    {
        _messageSink.OnMessage(new DiagnosticMessage($"[{DateTime.UtcNow:HH:mm:ss.fff} StartupTestFixture] {message}"));
    }
}