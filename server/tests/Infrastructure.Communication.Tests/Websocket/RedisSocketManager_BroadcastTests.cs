using System.Text.Json;
using Infrastructure.Communication.Websocket;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SharedTestDependencies.Fakes;
using Shouldly;
using StackExchange.Redis;

namespace Infrastructure.Communication.Tests.Websocket;

// Tester kun BroadcastAsync-metoden
// Bruger mocks for dets afhængigheder (ISubscriber)
public class RedisSocketManager_BroadcastTests
{
    private readonly Mock<IConnectionMultiplexer> _redisMultiplexerMock;
    private readonly Mock<ISubscriber> _subscriberMock;
    private readonly Mock<IDatabase> _dbMock;
    private readonly RedisSocketManager _manager;
    private readonly ILogger<RedisSocketManager> _logger;
    private readonly FakeTimeProvider _timeProvider;

    public RedisSocketManager_BroadcastTests()
    {
        _redisMultiplexerMock = new Mock<IConnectionMultiplexer>();
        _subscriberMock = new Mock<ISubscriber>();
        _dbMock = new Mock<IDatabase>();
        _logger = NullLogger<RedisSocketManager>.Instance;
        _timeProvider = new FakeTimeProvider(DateTimeOffset.Now);

        _redisMultiplexerMock.Setup(x => x.GetSubscriber(It.IsAny<object>()))
            .Returns(_subscriberMock.Object);
        _redisMultiplexerMock.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_dbMock.Object);

        _manager = new RedisSocketManager(
            _redisMultiplexerMock.Object,
            _timeProvider,
            _logger);
    }

    public sealed record TestMessage
    {
        public string? Content { get; init; }
    }

    private static string SerializeTestMessage<T>(T message) where T : class
    {
        return JsonSerializer.Serialize(new MessageWrapper<T>(message));
    }

    [Fact]
    public async Task BroadcastAsync_PublishesToCorrectRedisChannel_WhenSubscribersExist()
    {
        // Arrange
        var topic = "topic-pub";
        var message = new TestMessage { Content = "Publish Me" };
        var expectedChannel = RedisSocketKeys.TopicChannel(topic);
        var expectedJsonPayload = SerializeTestMessage(message);

        // Tjek at BroadcastAsync-metoden tjekker GetSubscribersAsync først, før den publisher.
        _dbMock.Setup(db => db.SetMembersAsync(RedisSocketKeys.TopicSubscribersSet(topic), CommandFlags.None))
            .ReturnsAsync([new RedisValue("client1")]);

        // Setup mock for at fange PublishAsync resultatet
        _subscriberMock.Setup(s => s.PublishAsync(It.IsAny<RedisChannel>(), It.IsAny<RedisValue>(), CommandFlags.None))
            .ReturnsAsync(1);

        // Act
        await _manager.BroadcastAsync(topic, message);

        // Assert
        // Verificer PublishAsync var kaldet præcis 1 gang med den forventede kanal og besked
        _subscriberMock.Verify(sub => sub.PublishAsync(
                It.Is<RedisChannel>(rc => rc == expectedChannel),
                It.Is<RedisValue>(rv => rv == expectedJsonPayload), CommandFlags.None),
            Times.Once);
    }

    [Fact]
    public async Task BroadcastAsync_DoesNotPublish_WhenNoSubscribersExistInState()
    {
        // Arrange
        var topic = "topic-empty";
        var message = new TestMessage { Content = "Anyone there?" };
        var expectedChannel = RedisSocketKeys.TopicChannel(topic);
        var expectedJsonPayload = SerializeTestMessage(message);

        _dbMock.Setup(db => db.SetMembersAsync(RedisSocketKeys.TopicSubscribersSet(topic), CommandFlags.None))
            .ReturnsAsync([]);
        
        // Act
        await _manager.BroadcastAsync(topic, message);

        // Assert
        // Verificer PublishAsync IKKE er kaldet
        _subscriberMock.Verify(sub => sub.PublishAsync(
                It.Is<RedisChannel>(rc => rc == expectedChannel),
                It.Is<RedisValue>(rv => rv == expectedJsonPayload),
                CommandFlags.None),
            Times.Never);
    }
    
    [Fact]
    public async Task BroadcastAsync_ThrowsArgumentNullException_ForNullMessage()
    {
        // Arrange
        var topic = "broadcast-null-msg";
        TestMessage message = null!;

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() =>
            _manager.BroadcastAsync(topic, message)
        );

        _subscriberMock.Verify(sub => sub.PublishAsync(
                It.IsAny<RedisChannel>(), It.IsAny<RedisValue>(), CommandFlags.None),
            Times.Never);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task BroadcastAsync_ThrowsArgumentException_ForInvalidTopic(string? invalidTopic)
    {
        // Arrange
        var message = new TestMessage { Content = "Invalid Topic Test" };

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(() =>
            _manager.BroadcastAsync(invalidTopic!, message)
        );

        _subscriberMock.Verify(sub => sub.PublishAsync(
                It.IsAny<RedisChannel>(), It.IsAny<RedisValue>(), CommandFlags.None),
            Times.Never);
    }
}