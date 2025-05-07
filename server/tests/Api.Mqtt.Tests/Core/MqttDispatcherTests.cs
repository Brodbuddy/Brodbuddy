using System.Text;
using System.Text.Json;
using Api.Mqtt.Core;
using Api.Mqtt.MessageHandlers;
using Api.Mqtt.Tests.MockHandlers;
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
    private readonly MqttDispatcher _dispatcher;

    public MqttDispatcherTests(ITestOutputHelper output)
    {
        _output = output;
        _mqttTestServiceMock = new Mock<IMqttTestService>();
        Mock<IDevicePublisher> devicePublisherMock = new();
        var loggerMock = new Mock<ILogger<MqttDispatcher>>();

        var services = new ServiceCollection();
        services.AddSingleton(_mqttTestServiceMock.Object);
        services.AddSingleton(devicePublisherMock.Object);

        services.AddScoped<DeviceTelemetryHandler>();
        services.AddScoped<SensorsMessageHandler>();
        services.AddScoped<DeviceTestMessageHandler>();

        IServiceProvider serviceProvider = services.BuildServiceProvider();

        _dispatcher = new MqttDispatcher(serviceProvider, loggerMock.Object);

        _dispatcher.RegisterHandlers(GetType().Assembly);
    }

    public class DispatchAsync : MqttDispatcherTests
    {
        public DispatchAsync(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task DispatchAsync_ShouldSerializeMessageCorrectly()
        {
            // Arrange
            var telemetry = new DeviceTelemetry(
                DeviceId: "dev1",
                Temperature: 25.5,
                Humidity: 60.0,
                Timestamp: 1704067200000 // Long værdien repræsentere Jan 1, 2024 UTC i millisekunder
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
            Should.NotThrow(() => _mqttTestServiceMock.Verify(
                s => s.ProcessTelemetryAsync(
                    "dev1",
                    25.5,
                    60.0,
                    new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                ),
                Times.Once
            ));
        }

        [Fact]
        public async Task DispatchAsync_ShouldHandleNoMatchingHandlers()
        {
            // Arrange 
            var telemetry = new DeviceTelemetry(
                DeviceId: "dev1",
                Temperature: 25.5,
                Humidity: 60.0,
                Timestamp: 1704067200000 // Long værdien repræsentere Jan 1, 2024 UTC i millisekunder
            );

            var payload = JsonSerializer.Serialize(telemetry);

            var publishMessage = new MQTT5PublishMessage
            {
                Topic = "unmatched/topic",
                Payload = Encoding.UTF8.GetBytes(payload)
            };

            var publishEventArgs = new OnMessageReceivedEventArgs(publishMessage);

            // Act
            await _dispatcher.DispatchAsync(publishEventArgs);

            //Assert
            Should.NotThrow(() => _mqttTestServiceMock.Verify(
                s => s.ProcessTelemetryAsync(
                    It.IsAny<string>(),
                    It.IsAny<double>(),
                    It.IsAny<double>(),
                    It.IsAny<DateTime>()),
                Times.Never
            ));
        }

        [Fact]
        public async Task DispatchAsync_ShouldHandleNullTopic()
        {
            // Arrange 
            var telemetry = new DeviceTelemetry(
                DeviceId: "dev1",
                Temperature: 25.5,
                Humidity: 60.0,
                Timestamp: 1704067200000 // Long værdien repræsentere Jan 1, 2024 UTC i millisekunder
            );

            var payload = JsonSerializer.Serialize(telemetry);

            var publishMessage = new MQTT5PublishMessage
            {
                Topic = null,
                Payload = Encoding.UTF8.GetBytes(payload)
            };

            var publishEventArgs = new OnMessageReceivedEventArgs(publishMessage);

            // Act
            await _dispatcher.DispatchAsync(publishEventArgs);

            // Assert
            Should.NotThrow(() => _mqttTestServiceMock.Verify(
                s => s.ProcessTelemetryAsync(
                    It.IsAny<string>(),
                    It.IsAny<double>(),
                    It.IsAny<double>(),
                    It.IsAny<DateTime>()),
                Times.Never
            ));
        }

        [Fact]
        public async Task DispatchAsync_ShouldHandleInvalidPayload()
        {
            // Arrange
            var invalidPayload = "Not JSON";

            var publishMessage = new MQTT5PublishMessage
            {
                Topic = "sensors/dev1/telemetry",
                Payload = Encoding.UTF8.GetBytes(invalidPayload)
            };

            var publishedEventArgs = new OnMessageReceivedEventArgs(publishMessage);

            // Act
            await _dispatcher.DispatchAsync(publishedEventArgs);

            // Assert
            Should.NotThrow(() => _mqttTestServiceMock.Verify(
                s => s.ProcessTelemetryAsync(
                    It.IsAny<string>(),
                    It.IsAny<double>(),
                    It.IsAny<double>(),
                    It.IsAny<DateTime>()),
                Times.Never
            ));
        }

        [Fact]
        public async Task DispatchAsync_ShouldHandleEmptyPayload()
        {
            // Arrange
            var emptyPayload = string.Empty;

            var publishMessage = new MQTT5PublishMessage
            {
                Topic = "sensors/dev1/telemetry",
                Payload = Encoding.UTF8.GetBytes(emptyPayload)
            };

            var publishedEventArgs = new OnMessageReceivedEventArgs(publishMessage);

            // Act
            await _dispatcher.DispatchAsync(publishedEventArgs);

            // Assert
            Should.NotThrow(() => _mqttTestServiceMock.Verify(
                s => s.ProcessTelemetryAsync(
                    It.IsAny<string>(),
                    It.IsAny<double>(),
                    It.IsAny<double>(),
                    It.IsAny<DateTime>()),
                Times.Never
            ));
        }


        [Fact]
        public void DispatchAsync_ShouldHandleNullArgs()
        {
            // Act & Assert
            Should.Throw<ArgumentNullException>(() => _dispatcher.DispatchAsync(null!));
        }
    }

    public class Subscriptions : MqttDispatcherTests
    {
        public Subscriptions(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void GetSubscriptions_ShouldReturnCorrectQoS()
        {
            // Act
            var subscriptions = _dispatcher.GetSubscriptions();

            // Assert
            subscriptions.ShouldContain(s =>
                s.TopicFilter == "devices/+/telemetry" && s.QoS == QualityOfService.AtLeastOnceDelivery);
            subscriptions.ShouldContain(s =>
                s.TopicFilter == "sensors/+/telemetry" && s.QoS == QualityOfService.AtMostOnceDelivery);
        }
    }

    public class RegisterHandlers : MqttDispatcherTests
    {
        public RegisterHandlers(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void RegisterHandlers_ShouldRegisterAllHandlersInAssembly()
        {
            // Act
            var subscriptions = _dispatcher.GetSubscriptions();

            // Assert
            subscriptions.ShouldNotBeEmpty();
            subscriptions.ShouldContain(s => s.TopicFilter == "sensors/+/telemetry");
            subscriptions.ShouldContain(s => s.TopicFilter == "devices/+/telemetry");
        }


        [Fact]
        public void RegisterHandlers_ShouldHandleNullAssembly()
        {
            // Act & Assert 
            Should.Throw<ArgumentNullException>(() => _dispatcher.RegisterHandlers(null!));
        }
    }
}