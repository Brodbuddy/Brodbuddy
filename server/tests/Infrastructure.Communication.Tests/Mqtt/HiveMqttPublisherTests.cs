using Infrastructure.Communication.Mqtt;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;

namespace Infrastructure.Communication.Tests.Mqtt;

public class HiveMqttPublisherTests
{
    private readonly Mock<ILogger<HiveMqttPublisher>> _mockLogger;

    public HiveMqttPublisherTests()
    {
        _mockLogger = new Mock<ILogger<HiveMqttPublisher>>();
    }

    [Fact]
    public async Task PublishAsync_WithNullOrEmptyTopic_ThrowsArgumentException()
    {
        // Arrange
        const string payload = "test";
        var publisher = new HiveMqttPublisher(null!, _mockLogger.Object);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(() => publisher.PublishAsync(null!, payload));
        await Should.ThrowAsync<ArgumentException>(() => publisher.PublishAsync("", payload));
        await Should.ThrowAsync<ArgumentException>(() => publisher.PublishAsync(" ", payload));
    }

    [Fact]
    public async Task PublishAsync_WithNullPayload_ThrowsArgumentNullException()
    {
        // Arrange
        const string topic = "test/topic";
        var publisher = new HiveMqttPublisher(null!, _mockLogger.Object);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() => publisher.PublishAsync(topic, null!));
    }

    [Fact]
    public async Task PublishAsync_GenericWithNullPayload_ThrowsArgumentNullException()
    {
        // Arrange
        const string topic = "test/topic";
        var publisher = new HiveMqttPublisher(null!, _mockLogger.Object);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() => publisher.PublishAsync<TestObject>(topic, null!));
    }

    private sealed class TestObject
    {
        public string Value { get; set; } = string.Empty;
    }
}