using HiveMQtt.Client;
using HiveMQtt.Client.Results;
using HiveMQtt.MQTT5.Types;
using Infrastructure.Communication.Mqtt;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;

namespace Infrastructure.Communication.Tests.Mqtt;

public class HiveMqttPublisherTests
{
    private readonly Mock<ILogger<HiveMqttPublisher>> _mockLogger;
    private readonly Mock<IHiveMQClient> _mockMqttClient;
    
    public HiveMqttPublisherTests()
    {
        _mockLogger = new Mock<ILogger<HiveMqttPublisher>>();
        _mockMqttClient = new Mock<IHiveMQClient>();
    }
    
    [Fact]
    public void Constructor_AssignsProperties_Correctly()
    {
        // Arrange & Act
        var publisher = new HiveMqttPublisher(_mockMqttClient.Object, _mockLogger.Object);
        
        // Assert
        var clientField = typeof(HiveMqttPublisher).GetField("_mqttClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var loggerField = typeof(HiveMqttPublisher).GetField("_logger", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        clientField!.GetValue(publisher).ShouldBe(_mockMqttClient.Object);
        loggerField!.GetValue(publisher).ShouldBe(_mockLogger.Object);
    }
    
    [Fact]
    public async Task PublishAsync_WithNullOrEmptyTopic_ThrowsArgumentException()
    {
        // Arrange
        const string payload = "test";
        var publisher = new HiveMqttPublisher(_mockMqttClient.Object, _mockLogger.Object);

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
        var publisher = new HiveMqttPublisher(_mockMqttClient.Object, _mockLogger.Object);

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

    [Fact]
    public async Task PublishAsync_WhenNotConnected_ThrowsInvalidOperationException()
    {
        // Arrange
        const string topic = "test/topic";
        const string payload = "test-payload";
        
        _mockMqttClient.Setup(m => m.IsConnected()).Returns(false);
        
        var publisher = new HiveMqttPublisher(_mockMqttClient.Object, _mockLogger.Object);

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(() => publisher.PublishAsync(topic, payload));
    }
    
    [Fact]
    public async Task PublishAsync_WithValidParameters_PublishesMessage()
    {
        // Arrange
        const string topic = "test/topic";
        const string payload = "test-payload";
        const QualityOfService qos = QualityOfService.AtLeastOnceDelivery;
        const bool retain = true;
        
        var message = new MQTT5PublishMessage
        {
            Topic = topic,
            Payload = System.Text.Encoding.UTF8.GetBytes(payload),
            QoS = qos,
            Retain = retain
        };
        
        _mockMqttClient.Setup(m => m.IsConnected()).Returns(true);
        _mockMqttClient.Setup(m => m.PublishAsync(It.IsAny<MQTT5PublishMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(new PublishResult(message));
        
        var publisher = new HiveMqttPublisher(_mockMqttClient.Object, _mockLogger.Object);

        // Act
        await publisher.PublishAsync(topic, payload, qos, retain);

        // Assert
        _mockMqttClient.Verify(m => m.PublishAsync(
                It.Is<MQTT5PublishMessage>(msg => 
                    msg.Topic == topic && 
                    msg.QoS == qos && 
                    msg.Retain == retain &&
                    msg.Payload != null &&
                    System.Text.Encoding.UTF8.GetString(msg.Payload) == payload),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
    
    [Fact]
    public async Task PublishAsync_WhenPublishingFails_ThrowsInvalidOperationException()
    {
        // Arrange
        const string topic = "test/topic";
        const string payload = "test-payload";
        
        _mockMqttClient.Setup(m => m.IsConnected()).Returns(true);
        _mockMqttClient.Setup(m => m.PublishAsync(It.IsAny<MQTT5PublishMessage>(), It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("Publishing failed"));
        
        var publisher = new HiveMqttPublisher(_mockMqttClient.Object, _mockLogger.Object);

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(() => publisher.PublishAsync(topic, payload));
    }
    
    private sealed class TestObject
    {
        public string Value { get; set; } = string.Empty;
    }
}