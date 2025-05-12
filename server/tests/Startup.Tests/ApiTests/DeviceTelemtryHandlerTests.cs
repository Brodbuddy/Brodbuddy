using Api.Mqtt.MessageHandlers;
using SharedTestDependencies.Constants;
using Shouldly;
using Startup.Tests.Mqtt;
using Xunit.Abstractions;

namespace Startup.Tests.ApiTests;

[Collection(TestCollections.Startup)]
public class DeviceTelemetryHandlerTests(StartupTestFixture fixture, ITestOutputHelper output) : ApiTestBase(fixture, output)
{
    [Fact]
    public async Task DeviceTelemetryHandler_ShouldRepublishToKakao_WhenReceivingTelemetry()
    {
        // Arrange
        await using var mqttClient = await Factory.CreateMqttClientAsync(Output);
        
        await mqttClient.SubscribeAsync("kakao");
        
        var deviceTelemetry = new DeviceTelemetry(
            DeviceId: "test-device-123",
            Temperature: 23.5,
            Humidity: 60.0,
            Timestamp: DateTime.UtcNow
        );
        
        // Act 
        var waitForRepublishTask = mqttClient.WaitForMessageAsync("kakao");
        await mqttClient.PublishAsync("devices/test-device-123/telemetry", deviceTelemetry);
        
        // Assert 
        var republishedMessage = await waitForRepublishTask;
        
        republishedMessage.ShouldNotBeNull();
        republishedMessage.ShouldBe("test-device-123");
    }
}