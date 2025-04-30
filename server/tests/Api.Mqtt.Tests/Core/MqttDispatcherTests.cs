using Api.Mqtt.Core;
using Api.Mqtt.MessageHandlers;
using Api.Mqtt.Tests.MockHandlers;
using Api.Mqtt.Tests.TestUtils;
using Application.Interfaces.Communication.Publishers;
using Application.Services;
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
        subscriptions.ShouldContain(s => s.TopicFilter == "test/message");
        
        _output.WriteLine($"Found {subscriptions.Count()} subscriptions:");
        foreach (var sub in subscriptions)
        {
            _output.WriteLine($" - {sub.TopicFilter} (QoS: {sub.QoS})");
        }
    }
}