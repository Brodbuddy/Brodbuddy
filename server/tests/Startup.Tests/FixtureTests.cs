using SharedTestDependencies.Constants;
using Shouldly;
using Startup.Tests.Infrastructure.Fixtures;
using Xunit.Abstractions;

namespace Startup.Tests;

[Collection(TestCollections.Startup)]
public class FixtureTests : IAsyncLifetime
{
    private readonly StartupTestFixture _fixture;
    private readonly ITestOutputHelper _output;

    public FixtureTests(StartupTestFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }
    
    public Task InitializeAsync() => Task.CompletedTask;
    
    public Task DisposeAsync() => _fixture.ResetAsync();
    
    [Fact]
    public void PostgresFixture_ShouldHaveConnectionString()
    {
        var connectionString = _fixture.Postgres.ConnectionString;
        _output.WriteLine($"Postgres connection string: {connectionString}");
        connectionString.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task RedisFixture_ShouldConnect()
    {
        var redis = _fixture.Redis.Redis;
        var ping = await redis.GetDatabase().PingAsync();
        _output.WriteLine($"Redis ping response time: {ping.TotalMilliseconds}ms");
        ping.TotalMilliseconds.ShouldBeGreaterThan(0);
    }
    
    [Fact]
    public void VerneMqFixture_ShouldHaveConnectionDetails()
    {
        var mqttConnection = _fixture.VerneMq.MqttConnectionString;
        var wsConnection = _fixture.VerneMq.WebSocketConnectionString;
    
        _output.WriteLine($"MQTT connection string: {mqttConnection}");
        _output.WriteLine($"WebSocket connection string: {wsConnection}");
    
        mqttConnection.ShouldNotBeNullOrEmpty();
        wsConnection.ShouldNotBeNullOrEmpty();
    
        _fixture.VerneMq.MappedMqttPort.ShouldBeGreaterThan(0);
        _fixture.VerneMq.MappedWebSocketPort.ShouldBeGreaterThan(0);
    }
}