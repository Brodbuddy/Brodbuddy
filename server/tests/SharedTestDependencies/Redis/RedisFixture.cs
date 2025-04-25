using DotNet.Testcontainers.Builders;
using Microsoft.Extensions.Logging;
using SharedTestDependencies.Logging;
using StackExchange.Redis;
using Testcontainers.Redis;
using Testcontainers.Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace SharedTestDependencies.Redis;

public class RedisFixture : ContainerFixture<RedisBuilder, RedisContainer>
{
    private readonly IMessageSink _messageSink;
    private readonly ILogger _tcLogger;
    private IConnectionMultiplexer? _redis;

    public IConnectionMultiplexer Redis => _redis ?? throw new InvalidOperationException("Redis connection not initialized");

    public RedisFixture(IMessageSink messageSink) : base(messageSink)
    {
        _messageSink = messageSink;
        _tcLogger = _messageSink.CreateLogger("Testcontainers", LogLevel.Debug);
    }
    
    protected override RedisBuilder Configure(RedisBuilder builder)
    {
        return builder
            .WithImage("docker.dragonflydb.io/dragonflydb/dragonfly:v1.27.1")
            .WithPortBinding(6379, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(6379))
            .WithLogger(_tcLogger);
    }
    
    protected override async Task InitializeAsync()
    {
        Log("Starting Dragonfly (Redis compatible) container...");
        await base.InitializeAsync();
        Log($"Dragonfly container started. Connection string: {Container.GetConnectionString()}");

        try
        {
            _redis = await ConnectionMultiplexer.ConnectAsync(Container.GetConnectionString());
            await _redis.GetDatabase().PingAsync();
            Log("Redis connection established and ping successful.");
        }
        catch (Exception ex)
        {
            Log($"ERROR connecting to Redis after container start: {ex}");

            try {
                var logs = await Container.GetLogsAsync();
                Log($"Container StdOut:\n{logs.Stdout}");
                Log($"Container StdErr:\n{logs.Stderr}");
            } catch (Exception logEx) {
                Log($"ERROR retrieving container logs: {logEx.Message}");
            }
            throw; 
        }
    }
    
    public async Task ResetAsync()
    {
        if (_redis == null || !_redis.IsConnected)
        {
            Log("ResetAsync skipped: Redis connection not available.");
            return;
        }

        Log("Cleaning Redis database (deleting keys)...");
        var db = Redis.GetDatabase();
        var server = Redis.GetServer(Container.GetConnectionString());

        var keys = server.Keys(database: db.Database).ToArray();

        if (keys.Length != 0)
        {
            long deletedCount = await db.KeyDeleteAsync(keys);
            Log($"Removed {deletedCount} keys from Redis database {db.Database}");
        }
        else
        {
            Log($"Redis database {db.Database} already empty or no keys found.");
        }
    }

    protected override async Task DisposeAsyncCore()
    {
        if (_redis != null)
        {
            await _redis.CloseAsync();
            _redis.Dispose();
            _redis = null;
            Log("Redis connection disposed.");
        }
        Log("Stopping Dragonfly container...");
        await base.DisposeAsyncCore();
        Log("Dragonfly container stopped and disposed.");
    }
    
    private void Log(string message)
    {
        _messageSink.OnMessage(new DiagnosticMessage($"[{DateTime.UtcNow:HH:mm:ss.fff} RedisFixture] {message}"));
    }
}