using System.Text.Json;
using Brodbuddy.WebSocket.State;
using Fleck;
using Infrastructure.Communication.Websocket;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StackExchange.Redis;

namespace Infrastructure.Communication.Tests.Websocket;

public class RedisSubscriptionListenerTests : IAsyncDisposable
{
    private readonly Mock<IConnectionMultiplexer> _redisMultiplexerMock;
    private readonly Mock<ISubscriber> _subscriberMock;
    private readonly Mock<IDatabase> _dbMock;
    private readonly Mock<ISocketManager> _socketManagerMock;
    private readonly ILogger<RedisSubscriptionListener> _logger;
    private readonly RedisSubscriptionListener _listener;

    public RedisSubscriptionListenerTests()
    {
        _redisMultiplexerMock = new Mock<IConnectionMultiplexer>();
        _subscriberMock = new Mock<ISubscriber>();
        _dbMock = new Mock<IDatabase>();
        _socketManagerMock = new Mock<ISocketManager>();
        _logger = NullLogger<RedisSubscriptionListener>.Instance;

        _redisMultiplexerMock.Setup(x => x.GetSubscriber(It.IsAny<object>())).Returns(_subscriberMock.Object);
        _redisMultiplexerMock.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_dbMock.Object);

        _listener = new RedisSubscriptionListener(
            _logger,
            _redisMultiplexerMock.Object,
            _socketManagerMock.Object);
    }

    // Hjælper for test beskeder
    private sealed record TestMessage
    {
        public string? Data { get; init; }
    }

    // Hjælper til at simulere vi har modtaget en besked
    private Task TriggerHandleMessageAsync(string topic, object payload)
    {
        var channel = RedisChannel.Pattern(RedisSocketKeys.TopicChannel(topic));
        var messageJson = JsonSerializer.Serialize(new MessageWrapper<object>(payload));
        var redisValue = (RedisValue)messageJson;
        return _listener.ProcessMessageAsync(channel, redisValue);
    }

    [Fact]
    public async Task ProcessMessageAsync_SendsToCorrectLocalSubscribedClient()
    {
        // Arrange
        var topic = "news";
        var localClientId = "client-1";
        var remoteClientId = "client-2";
        var localSocketId = Guid.NewGuid();
        var mockLocalSocket = new Mock<IWebSocketConnection>();
        mockLocalSocket.SetupGet(s => s.IsAvailable).Returns(true);

        var message = new TestMessage { Data = "Local Test" };
        var expectedJson = JsonSerializer.Serialize(new MessageWrapper<TestMessage>(message));

        // Mock ISocketManager 
        _socketManagerMock.Setup(m => m.GetSubscribersAsync(topic))
            .ReturnsAsync(new List<string> { localClientId, remoteClientId }.AsReadOnly());

        _socketManagerMock.Setup(m => m.TryGetLocalSocket(localSocketId, out It.Ref<IWebSocketConnection?>.IsAny))
            .Returns((Guid id, out IWebSocketConnection? socket) =>
            {
                socket = mockLocalSocket.Object;
                return true;
            });

        _socketManagerMock.Setup(m =>
                m.TryGetLocalSocket(It.IsNotIn(localSocketId), out It.Ref<IWebSocketConnection?>.IsAny))
            .Returns((Guid id, out IWebSocketConnection? socket) =>
            {
                socket = null;
                return false;
            }); // Et andet socket ID

        // Mock IDatabase (skal bruges af listener for at få socket IDer for klienter) 
        _dbMock.Setup(db => db.SetMembersAsync(RedisSocketKeys.ClientSocketsSet(localClientId), CommandFlags.None))
            .ReturnsAsync([(RedisValue)localSocketId.ToString()]);
        _dbMock.Setup(db => db.SetMembersAsync(RedisSocketKeys.ClientSocketsSet(remoteClientId), CommandFlags.None))
            .ReturnsAsync([(RedisValue)Guid.NewGuid().ToString()]); // Et andet socket ID

        // Act
        await TriggerHandleMessageAsync(topic, message);

        // Assert
        // Verificer Send kun var kaldet EN GANG på den lokale socket
        mockLocalSocket.Verify(s => s.Send(expectedJson), Times.Once);
    }

    [Fact]
    public async Task ProcessMessageAsync_SendsToMultipleLocalSocketsForClient()
    {
        // Arrange
        var topic = "updates";
        var clientId = "client-multi-local";
        var localSocketId1 = Guid.NewGuid();
        var localSocketId2 = Guid.NewGuid();
        var mockLocalSocket1 = new Mock<IWebSocketConnection>();
        var mockLocalSocket2 = new Mock<IWebSocketConnection>();
        mockLocalSocket1.SetupGet(s => s.IsAvailable).Returns(true);
        mockLocalSocket2.SetupGet(s => s.IsAvailable).Returns(true);

        var message = new TestMessage { Data = "Multi Local Test" };
        var expectedJson = JsonSerializer.Serialize(new MessageWrapper<TestMessage>(message));

        _socketManagerMock.Setup(m => m.GetSubscribersAsync(topic))
            .ReturnsAsync(new List<string> { clientId }.AsReadOnly());

        _socketManagerMock.Setup(m => m.TryGetLocalSocket(localSocketId1, out It.Ref<IWebSocketConnection?>.IsAny))
            .Returns((Guid id, out IWebSocketConnection? socket) =>
            {
                socket = mockLocalSocket1.Object;
                return true;
            });

        _socketManagerMock.Setup(m => m.TryGetLocalSocket(localSocketId2, out It.Ref<IWebSocketConnection?>.IsAny))
            .Returns((Guid id, out IWebSocketConnection? socket) =>
            {
                socket = mockLocalSocket2.Object;
                return true;
            });

        _dbMock.Setup(db => db.SetMembersAsync(RedisSocketKeys.ClientSocketsSet(clientId), CommandFlags.None))
            .ReturnsAsync([(RedisValue)localSocketId1.ToString(), (RedisValue)localSocketId2.ToString()]);

        // Act
        await TriggerHandleMessageAsync(topic, message);

        // Assert
        mockLocalSocket1.Verify(s => s.Send(expectedJson), Times.Once);
        mockLocalSocket2.Verify(s => s.Send(expectedJson), Times.Once);
    }

    [Fact]
    public async Task ProcessMessageAsync_SkipsUnavailableLocalSocket()
    {
        // Arrange
        var topic = "availability";
        var clientId = "client-avail-test";
        var socketId = Guid.NewGuid();
        var mockSocket = new Mock<IWebSocketConnection>();
        mockSocket.SetupGet(s => s.IsAvailable).Returns(false); // Marker som utilgængelig

        var message = new TestMessage { Data = "Unavailable Test" };

        _socketManagerMock.Setup(m => m.GetSubscribersAsync(topic))
            .ReturnsAsync(new List<string> { clientId }.AsReadOnly());

        _socketManagerMock.Setup(m => m.TryGetLocalSocket(socketId, out It.Ref<IWebSocketConnection?>.IsAny)).Returns(
            (Guid id, out IWebSocketConnection? socket) =>
            {
                socket = mockSocket.Object;
                return true;
            });

        _dbMock.Setup(db => db.SetMembersAsync(RedisSocketKeys.ClientSocketsSet(clientId), CommandFlags.None))
            .ReturnsAsync([(RedisValue)socketId.ToString()]);

        // Act
        await TriggerHandleMessageAsync(topic, message);

        // Assert
        mockSocket.Verify(s => s.Send(It.IsAny<string>()), Times.Never); // Skal ikke kaldes
    }

    [Fact]
    public async Task ProcessMessageAsync_HandlesTopicWithNoLocalSubscribers()
    {
        // Arrange
        var topic = "remote-only";
        var remoteClientId = "client-remote-only";
        var remoteSocketId = Guid.NewGuid();

        var message = new TestMessage { Data = "Remote Only Test" };

        // Mock at subscribers eksister, men ingen er lokale
        _socketManagerMock.Setup(m => m.GetSubscribersAsync(topic))
            .ReturnsAsync(new List<string> { remoteClientId }.AsReadOnly());

        _socketManagerMock.Setup(m => m.TryGetLocalSocket(It.IsAny<Guid>(), out It.Ref<IWebSocketConnection?>.IsAny))
            .Returns((Guid id, out IWebSocketConnection? socket) =>
            {
                socket = null;
                return false;
            }); // Ingen lokale sockets fundet

        _dbMock.Setup(db => db.SetMembersAsync(RedisSocketKeys.ClientSocketsSet(remoteClientId), CommandFlags.None))
            .ReturnsAsync([(RedisValue)remoteSocketId.ToString()]);

        // Act
        await TriggerHandleMessageAsync(topic, message);

        // Assert
        // Ingen mocks at verificere Send for, vi sikre bare at ingen exceptions er kastet implicit ved at lad testen gå gennem
        _socketManagerMock.Verify(m => m.TryGetLocalSocket(remoteSocketId, out It.Ref<IWebSocketConnection?>.IsAny),
            Times.Once); // Verificer den tjekkede for remote socket lokalt
    }

    [Fact]
    public async Task ProcessMessage_HandlesTopicWithNoSubscribersInState()
    {
        // Arrange
        var topic = "no-one-home";
        var message = new TestMessage { Data = "Empty Topic Test" };

        // Mock at GetSubscribers returner en tom liste
        _socketManagerMock.Setup(m => m.GetSubscribersAsync(topic)).ReturnsAsync(new List<string>().AsReadOnly());

        // Act
        await TriggerHandleMessageAsync(topic, message);

        // Assert
        // Verificer TryGetLocalSocket og SetMembers aldrig bliver kaldt da der blev returneret tidligt
        _socketManagerMock.Verify(m => m.TryGetLocalSocket(It.IsAny<Guid>(), out It.Ref<IWebSocketConnection?>.IsAny),
            Times.Never);
        _dbMock.Verify(db => db.SetMembersAsync(It.IsAny<RedisKey>(), CommandFlags.None), Times.Never);
    }

    [Theory]
    [InlineData("invalidchannel")]
    [InlineData("pubsub:wrongprefix:topic")]
    [InlineData("pubsub:topic")] 
    public async Task ProcessMessageAsync_IgnoresInvalidChannelFormat(string invalidChannelStr)
    {
        // Arrange
        var message = new TestMessage { Data = "Bad Channel" };
        var messageJson = JsonSerializer.Serialize(new MessageWrapper<TestMessage>(message));
        var redisChannel = RedisChannel.Pattern(invalidChannelStr);
        var redisValue = (RedisValue)messageJson;

        // Act
        await _listener.ProcessMessageAsync(redisChannel, redisValue);

        // Assert
        // Verificer at der blev forsøgt at få subscribers fordi det var en ugyldig kanal (channel)
        _socketManagerMock.Verify(m => m.GetSubscribersAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ProcessMessageAsync_ContinuesSending_WhenOneSocketSendThrows()
    {
        // Arrange
        var topic = "error-handling";
        var clientOk = "client-ok";
        var clientFail = "client-fail";
        var socketIdOk = Guid.NewGuid();
        var socketIdFail = Guid.NewGuid();
        var mockSocketOk = new Mock<IWebSocketConnection>();
        var mockSocketFail = new Mock<IWebSocketConnection>();

        mockSocketOk.SetupGet(s => s.IsAvailable).Returns(true);
        mockSocketFail.SetupGet(s => s.IsAvailable).Returns(true);
        
        // Opsæt den fejlende socket til at kaste når Send er kaldet
        mockSocketFail.Setup(s => s.Send(It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Send failed!"));

        var message = new TestMessage { Data = "Error Test" };
        var expectedJson = JsonSerializer.Serialize(new MessageWrapper<TestMessage>(message));

        _socketManagerMock.Setup(m => m.GetSubscribersAsync(topic))
            .ReturnsAsync(new List<string> { clientOk, clientFail }.AsReadOnly());
        
        _socketManagerMock.Setup(m => m.TryGetLocalSocket(socketIdOk, out It.Ref<IWebSocketConnection?>.IsAny)).Returns(
            (Guid id, out IWebSocketConnection? socket) =>
            {
                socket = mockSocketOk.Object;
                return true;
            });
        
        _socketManagerMock.Setup(m => m.TryGetLocalSocket(socketIdFail, out It.Ref<IWebSocketConnection?>.IsAny))
            .Returns((Guid _, out IWebSocketConnection? socket) =>
            {
                socket = mockSocketFail.Object;
                return true;
            });

        _dbMock.Setup(db => db.SetMembersAsync(RedisSocketKeys.ClientSocketsSet(clientOk), CommandFlags.None))
            .ReturnsAsync([(RedisValue)socketIdOk.ToString()]);
        
        _dbMock.Setup(db => db.SetMembersAsync(RedisSocketKeys.ClientSocketsSet(clientFail), CommandFlags.None))
            .ReturnsAsync([(RedisValue)socketIdFail.ToString()]);

        // Act
        await TriggerHandleMessageAsync(topic, message);

        // Assert
        // Verificer at OK socket blev kaldet
        mockSocketOk.Verify(s => s.Send(expectedJson), Times.Once);
        // Verificer at den fejlende socket også blev kaldet, men kastede en exception
        mockSocketFail.Verify(s => s.Send(expectedJson), Times.Once);
    }
    
    public async ValueTask DisposeAsync()
    {
        await _listener.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}