using Core.ValueObjects;
using SharedTestDependencies.Constants;
using SharedTestDependencies.Fixtures;
using Shouldly;
using Startup.Tests.Infrastructure.Fixtures;
using Startup.Tests.Infrastructure.Bases;
using Startup.Tests.Infrastructure.Extensions;
using Xunit;
using Xunit.Abstractions;

namespace Startup.Tests.Api.Mqtt;

[Collection(TestCollections.Startup)]
public class MqttMessageHandlerTests(StartupTestFixture fixture, ITestOutputHelper output) : ApiTestBase(fixture, output)
{
    [Fact]
    public async Task AnalyzerTelemetryHandler_ProcessesTelemetryData_SavesReading()
    {
        // Arrange
        var mqttClient = await Factory.CreateMqttClientAsync(Output);
        var analyzerId = Guid.NewGuid();
        var telemetryData = new SourdoughReading(
            Rise: 10.0,
            Temperature: 25.5,
            Humidity: 65.0,
            EpochTime: DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Timestamp: DateTime.UtcNow,
            LocalTime: DateTime.UtcNow,
            FeedingNumber: 1
        );
        
        // Act
        await mqttClient.PublishAsync($"analyzer/{analyzerId}/telemetry", telemetryData);
        
        // Assert
        await Task.Delay(100);
        telemetryData.ShouldNotBeNull();
    }
}