using System.Text.Json;
using HiveMQtt.Client;
using HiveMQtt.MQTT5.Types;
using Infrastructure.Communication.Mqtt;
using Shouldly;
using SharedTestDependencies.Constants;
using Microsoft.Extensions.Logging.Abstractions;
using SharedTestDependencies.Fixtures;
using SharedTestDependencies.Lifecycle;

namespace Infrastructure.Communication.Tests.Mqtt;

[Collection(TestCollections.Mqtt)]
public class HiveMqttPublisherTests : IAsyncLifetime, IDisposable, IClassFixture<VerneMqFixture>
{
    private readonly VerneMqFixture _fixture;
    private HiveMqttPublisher _publisher = null!;
    private HiveMQClient _mqttClient = null!;
    private HiveMQClient _testSubscriber = null!;
    private bool _disposed;

    static HiveMqttPublisherTests()
    {
        // Starter watchdog med 60s timeout som fallback hvis TestTracker fejler
        ProcessWatchdog.StartWatchdog(60);
    }

    private HiveMqttPublisherTests(VerneMqFixture fixture)
    {
        _fixture = fixture;
        TestTracker.RegisterActiveTest();
    }

    public async Task InitializeAsync()
    {
        // Opret MQTT klient til publisher
        var clientOptions = new HiveMQClientOptionsBuilder()
            .WithWebSocketServer($"ws://{_fixture.Host}:{_fixture.MappedWebSocketPort}/mqtt")
            .WithClientId($"publisher-{Guid.NewGuid()}")
            .WithUserName("user")
            .WithPassword("pass")
            .WithCleanStart(true)
            .Build();

        _mqttClient = new HiveMQClient(clientOptions);
        await _mqttClient.ConnectAsync();

        // Opret klient til at verificere publishing
        var subscriberOptions = new HiveMQClientOptionsBuilder()
            .WithWebSocketServer($"ws://{_fixture.Host}:{_fixture.MappedWebSocketPort}/mqtt")
            .WithClientId($"subscriber-{Guid.NewGuid()}")
            .WithUserName("user")
            .WithPassword("pass")
            .WithCleanStart(true)
            .Build();

        _testSubscriber = new HiveMQClient(subscriberOptions);
        await _testSubscriber.ConnectAsync();

        _publisher = new HiveMqttPublisher(_mqttClient, new NullLogger<HiveMqttPublisher>());
    }

    public async Task DisposeAsync()
    {
        if (_mqttClient.IsConnected())
        {
            await _mqttClient.DisconnectAsync();
        }
        _mqttClient.Dispose();

        if (_testSubscriber.IsConnected())
        {
            await _testSubscriber.DisconnectAsync();
        }
        _testSubscriber.Dispose();
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
            TestTracker.UnregisterActiveTest();
        }

        _disposed = true;
    }

    public class PublishAsync(VerneMqFixture fixture) : HiveMqttPublisherTests(fixture)
    {
        [Fact]
        public async Task PublishAsync_WithValidTopicAndPayload_SendsMessage()
        {
            // Arrange
            var topic = $"test/topic/{Guid.NewGuid():N}";
            const string payload = "test message";
            var received = false;
            string? receivedPayload = null;

            await _testSubscriber.SubscribeAsync(topic);

            _testSubscriber.OnMessageReceived += (_, e) =>
            {
                if (e.PublishMessage.Topic != topic) return;

                received = true;
                receivedPayload = e.PublishMessage.PayloadAsString;
            };

            // Act
            await _publisher.PublishAsync(topic, payload);

            // Vent på besked
            await Task.Delay(200); 

            // Assert
            received.ShouldBeTrue();
            receivedPayload.ShouldBe(payload);
        }

        [Fact]
        public async Task PublishAsync_WithObjectPayload_SerializesToJson()
        {
            // Arrange
            var topic = $"test/topic/json/{Guid.NewGuid():N}"; 
            var testObject = new TestMessage { Id = 123, Name = "Test" };
            var received = false;
            TestMessage? receivedObject = null;

            await _testSubscriber.SubscribeAsync(topic);

            _testSubscriber.OnMessageReceived += (_, e) =>
            {
                if (e.PublishMessage.Topic != topic) return;
                received = true;
                var json = e.PublishMessage.PayloadAsString;
                var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                receivedObject = JsonSerializer.Deserialize<TestMessage>(json, options);
            };

            // Act
            await _publisher.PublishAsync(topic, testObject);

            // Vent på besked
            await Task.Delay(200); 

            // Assert
            received.ShouldBeTrue();
            receivedObject.ShouldNotBeNull();
            receivedObject.Id.ShouldBe(testObject.Id);
            receivedObject.Name.ShouldBe(testObject.Name);
        }

        [Fact]
        public async Task PublishAsync_WithDifferentQosLevels_HandlesAllLevels()
        {
            // Arrange
            var topic = $"test/topic/qos/{Guid.NewGuid():N}"; 
            const string payload = "qos test";
            var qosLevels = new[] { QualityOfService.AtMostOnceDelivery, QualityOfService.AtLeastOnceDelivery, QualityOfService.ExactlyOnceDelivery };

            foreach (var qos in qosLevels)
            {
                var received = false;
                var targetTopic = $"{topic}/{qos}";

                // Opsæt handler før subscribe
                var tcs = new TaskCompletionSource<bool>();

                _testSubscriber.OnMessageReceived += MessageHandler;

                await _testSubscriber.SubscribeAsync(targetTopic);

                // Act
                await _publisher.PublishAsync(targetTopic, payload, qos);

                // Vent på besked med timeout
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                try
                {
                    await tcs.Task.WaitAsync(cts.Token);
                }
                catch (OperationCanceledException)
                {
                    // Timeout er ok for nogle QoS niveauer
                }

                // Assert
                received.ShouldBeTrue($"Failed to receive message with QoS {qos}");

                _testSubscriber.OnMessageReceived -= MessageHandler;

                void MessageHandler(object? sender, HiveMQtt.Client.Events.OnMessageReceivedEventArgs e)
                {
                    if (e.PublishMessage.Topic != targetTopic) return;
                    received = true;
                    tcs.TrySetResult(true);
                }
            }
        }

        [Fact]
        public async Task PublishAsync_WithRetainFlag_HandlesRetainedMessage()
        {
            // Arrange
            var topic = $"test/topic/retain/{Guid.NewGuid():N}"; 
            const string payload = "retained message";

            // Act - Publish med retain
            await _publisher.PublishAsync(topic, payload, retain: true);

            await Task.Delay(100);

            // Subscribe nu - skulle modtage retained besked
            var received = false;
            string? receivedPayload = null;

            _testSubscriber.OnMessageReceived += (_, e) =>
            {
                if (e.PublishMessage.Topic != topic) return;

                received = true;
                receivedPayload = e.PublishMessage.PayloadAsString;
            };

            await _testSubscriber.SubscribeAsync(topic);

            // Vent på besked 
            await Task.Delay(200); 

            // Assert
            received.ShouldBeTrue();
            receivedPayload.ShouldBe(payload);
        }

        [Fact]
        public async Task PublishAsync_WithNullOrEmptyTopic_ThrowsArgumentException()
        {
            // Arrange
            const string payload = "test";

            // Act & Assert
            await Should.ThrowAsync<ArgumentException>(() => _publisher.PublishAsync(null!, payload));
            await Should.ThrowAsync<ArgumentException>(() => _publisher.PublishAsync("", payload));
            await Should.ThrowAsync<ArgumentException>(() => _publisher.PublishAsync(" ", payload));
        }

        [Fact]
        public async Task PublishAsync_WithNullPayload_ThrowsArgumentNullException()
        {
            // Arrange
            var topic = $"test/topic/{Guid.NewGuid():N}";

            // Act & Assert
            await Should.ThrowAsync<ArgumentNullException>(() => _publisher.PublishAsync(topic, null!));
        }

        [Fact]
        public async Task PublishAsync_GenericWithNullPayload_ThrowsArgumentNullException()
        {
            // Arrange
            var topic = $"test/topic/{Guid.NewGuid():N}";
            TestMessage? payload = null;

            // Act & Assert
            await Should.ThrowAsync<ArgumentNullException>(() => _publisher.PublishAsync(topic, payload!));
        }

        [Fact]
        public async Task PublishAsync_WhenNotConnected_ThrowsInvalidOperationException()
        {
            // Arrange
            var disconnectedOptions = new HiveMQClientOptionsBuilder()
                .WithWebSocketServer($"ws://{_fixture.Host}:{_fixture.MappedWebSocketPort}/mqtt")
                .WithClientId($"disconnected-{Guid.NewGuid()}")
                .WithUserName("user")
                .WithPassword("pass")
                .Build();

            var disconnectedClient = new HiveMQClient(disconnectedOptions);
            var disconnectedPublisher = new HiveMqttPublisher(disconnectedClient, new NullLogger<HiveMqttPublisher>());

            var topic = $"test/topic/{Guid.NewGuid():N}"; 
            const string payload = "test";

            // Act & Assert
            await Should.ThrowAsync<InvalidOperationException>(() => disconnectedPublisher.PublishAsync(topic, payload));

            disconnectedClient.Dispose();
        }

        private sealed class TestMessage
        {
            public int Id { get; set; }
            public string? Name { get; set; }
        }
    }
}