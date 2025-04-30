using System.Text;
using System.Text.Json;
using Api.Mqtt.Core;
using Api.Mqtt.MessageHandlers;
using Api.Mqtt.Tests.MockHandlers;
using Api.Mqtt.Tests.TestUtils;
using Application.Interfaces.Communication.Publishers;
using Application.Services;
using HiveMQtt.Client.Events;
using HiveMQtt.MQTT5.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit.Abstractions;

namespace Api.Mqtt.Tests.Core;


public class MqttDispatcherTests
{ 
    private readonly ITestOutputHelper _output;
    private readonly Mock<IMqttTestService> _mqttTestServiceMock;
    private readonly Mock<IDevicePublisher> _devicePublisherMock;
    private readonly MqttDispatcher _dispatcher;
    private readonly MockMqttPublishMessage _publishMessage;

    public MqttDispatcherTests(ITestOutputHelper output)
    {
        _output = output;
        _mqttTestServiceMock = new Mock<IMqttTestService>();
        _devicePublisherMock = new Mock<IDevicePublisher>();
        var loggerMock = new Mock<ILogger<MqttDispatcher>>();

        var services = new ServiceCollection();
        services.AddSingleton(_mqttTestServiceMock.Object);
        services.AddSingleton(_devicePublisherMock.Object);

        services.AddScoped<DeviceTelemetryHandler>();
        services.AddScoped<TestMessageHandler>();

        IServiceProvider serviceProvider = services.BuildServiceProvider();

        _dispatcher = new MqttDispatcher(serviceProvider, loggerMock.Object);
        
        _dispatcher.RegisterHandlers(GetType().Assembly);

        _publishMessage = new MockMqttPublishMessage();
    }
    
    [Fact]
    public void RegisterHandlers_ShouldRegisterAllHandlersInAssembly()
    {
        // Act
        var subscriptions = _dispatcher.GetSubscriptions();

        // Assert
        subscriptions.ShouldNotBeEmpty();
        subscriptions.ShouldContain(s => s.TopicFilter == "sensors/+/telemetry");
    }

    [Fact]
    public async Task DispatchAsync_ShouldSerializeMessageCorretly()
    {
        // Arrange
        var telemetry = new DeviceTelemetry(
            DeviceId: "dev1",
            Temperature: 25.5,
            Humidity: 60.0,
            Timestamp: new DateTime(2024, 1, 1,0 ,0, 0, DateTimeKind.Utc)
        );

        var payload = JsonSerializer.Serialize(telemetry);
        
        var publishMessage = new MQTT5PublishMessage
        {
            Topic = "sensors/dev1/telemetry",
            Payload = Encoding.UTF8.GetBytes(payload)
        };

        var publishEventArgs = new OnMessageReceivedEventArgs(publishMessage);
        
        _mqttTestServiceMock
            .Setup(s => s.ProcessTelemetryAsync(
                It.IsAny<string>(),
                It.IsAny<double>(),
                It.IsAny<double>(),
                It.IsAny<DateTime>()))
            .Returns(Task.CompletedTask);
        
        // Act
        await _dispatcher.DispatchAsync(publishEventArgs);
        
        // Assert
        _mqttTestServiceMock.Verify(
            s => s.ProcessTelemetryAsync(
                "dev1",
                25.5,
                60.0,
                new DateTime(2024, 1, 1,0 ,0, 0, DateTimeKind.Utc)
            ),
            Times.Once
        );

    }
    
    
    
}