using System.Text.Json;
using Fleck;
using Infrastructure.Communication.Websocket;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SharedTestDependencies.Fakes;
using SharedTestDependencies.Fixtures;
using Shouldly;
using StackExchange.Redis;

namespace Infrastructure.Communication.Tests.Websocket;

[Collection("Redis Tests")]
public class RedisSocketManagerTests : IClassFixture<RedisFixture>, IAsyncLifetime
{
    private readonly RedisFixture _fixture;
    private readonly FakeTimeProvider _timeProvider;
    private readonly ILogger<RedisSocketManager> _logger;
    private readonly RedisSocketManager _manager;
    private IDatabase _db = null!;

    public RedisSocketManagerTests(RedisFixture fixture)
    {
        _fixture = fixture;
        _timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        _logger = new NullLogger<RedisSocketManager>();
        _manager = new RedisSocketManager(_fixture.Redis, _timeProvider, _logger);
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        _db = _fixture.Redis.GetDatabase();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    public class OnOpenAsync(RedisFixture fixture) : RedisSocketManagerTests(fixture)
    {
        [Fact]
        public async Task OnOpenAsync_StoresSocketAndClientId()
        {
            // Arrange
            var socket = CreateMockSocket(Guid.NewGuid());
            var socketId = socket.ConnectionInfo.Id;
            const string clientId = "test-client";

            // Act
            await _manager.OnOpenAsync(socket, clientId);

            // Assert
            string storedClientId = (await _db.StringGetAsync(RedisSocketKeys.SocketToClientMap(socketId)))!;
            storedClientId.ShouldNotBeNull();
            storedClientId.ShouldBe(clientId);

            (await _db.SetContainsAsync(RedisSocketKeys.ClientSocketsSet(clientId), socketId.ToString()))
                .ShouldBeTrue();
            (await _db.KeyExistsAsync(RedisSocketKeys.SocketHash(socketId))).ShouldBeTrue();
            (await _db.SetContainsAsync(RedisSocketKeys.ActiveSocketsSetKey, socketId.ToString())).ShouldBeTrue();
            var storedHashClientId =
                (string)(await _db.HashGetAsync(RedisSocketKeys.SocketHash(socketId), RedisSocketKeys.ClientIdField))!;
            storedHashClientId.ShouldNotBeNull();
            storedHashClientId.ShouldBe(clientId);
        }

        [Fact]
        public async Task OnOpenAsync_ThrowsArgumentNullException_WhenSocketIsNull()
        {
            // Arrange
            IWebSocketConnection? socket = null;
            var clientId = "any-client";

            // Act & Assert
            await Should.ThrowAsync<ArgumentNullException>(() => _manager.OnOpenAsync(socket!, clientId));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("\t")]
        public async Task OnOpenAsync_ThrowsArgumentException_WhenClientIdIsNullOrEmptyOrWhitespace(
            string? invalidClientId)
        {
            // Arrange
            var socket = CreateMockSocket(Guid.NewGuid());

            // Act & Assert
            await Should.ThrowAsync<ArgumentException>(() => _manager.OnOpenAsync(socket, invalidClientId!));
        }

        [Fact]
        public async Task OnOpenAsync_AddsSecondSocket_WhenClientAlreadyConnected()
        {
            // Arrange
            var clientId = "multi-socket-client";
            var socket1 = CreateMockSocket(Guid.NewGuid());
            var socket2 = CreateMockSocket(Guid.NewGuid());
            var socketId1 = socket1.ConnectionInfo.Id;
            var socketId2 = socket2.ConnectionInfo.Id;

            await _manager.OnOpenAsync(socket1, clientId);

            // Act
            await _manager.OnOpenAsync(socket2, clientId);

            // Assert
            var clientSocketsKey = RedisSocketKeys.ClientSocketsSet(clientId);

            var members = await _db.SetMembersAsync(clientSocketsKey);
            members.Length.ShouldBe(2);

            var memberStrings = members.Select(m => m.ToString()).ToArray();
            memberStrings.ShouldContain(socketId1.ToString());
            memberStrings.ShouldContain(socketId2.ToString());

            // Verificer at keys for begge sockets eksister
            (await _db.KeyExistsAsync(RedisSocketKeys.SocketHash(socketId1))).ShouldBeTrue();
            (await _db.KeyExistsAsync(RedisSocketKeys.SocketHash(socketId2))).ShouldBeTrue();

            string storedClientId1 = (await _db.StringGetAsync(RedisSocketKeys.SocketToClientMap(socketId1)))!;
            string storedClientId2 = (await _db.StringGetAsync(RedisSocketKeys.SocketToClientMap(socketId2)))!;
            storedClientId1.ShouldBe(clientId);
            storedClientId2.ShouldBe(clientId);

            // Verificer at begge er i det globale aktive set
            (await _db.SetContainsAsync(RedisSocketKeys.ActiveSocketsSetKey, socketId1.ToString())).ShouldBeTrue();
            (await _db.SetContainsAsync(RedisSocketKeys.ActiveSocketsSetKey, socketId2.ToString())).ShouldBeTrue();
        }


        [Fact]
        public async Task OnOpenAsync_IsIdempotent_WhenCalledTwiceWithSameSocket()
        {
            // Arrange
            var socket = CreateMockSocket(Guid.NewGuid());
            var socketId = socket.ConnectionInfo.Id;
            const string clientId = "idempotent-client";
            _timeProvider.SetUtcNow(new DateTimeOffset(2025, 1, 1, 12, 0, 0, TimeSpan.Zero));
            var expectedTimestamp = _timeProvider.GetUtcNow().ToUnixTimeSeconds();

            // Act
            await _manager.OnOpenAsync(socket, clientId);
            _timeProvider.Advance(TimeSpan.FromSeconds(10));
            await _manager.OnOpenAsync(socket, clientId);

            // Assert
            string storedClientId = (await _db.StringGetAsync(RedisSocketKeys.SocketToClientMap(socketId)))!;
            storedClientId.ShouldBe(clientId);

            (await _db.SetContainsAsync(RedisSocketKeys.ClientSocketsSet(clientId), socketId.ToString()))
                .ShouldBeTrue();
            (await _db.KeyExistsAsync(RedisSocketKeys.SocketHash(socketId))).ShouldBeTrue();
            (await _db.SetContainsAsync(RedisSocketKeys.ActiveSocketsSetKey, socketId.ToString())).ShouldBeTrue();

            (await _db.SetLengthAsync(RedisSocketKeys.ClientSocketsSet(clientId))).ShouldBe(1);
            (await _db.SetLengthAsync(RedisSocketKeys.ActiveSocketsSetKey)).ShouldNotBe(0);

            var storedTimestamp =
                (long)(await _db.HashGetAsync(RedisSocketKeys.SocketHash(socketId), RedisSocketKeys.ConnectedAtField))!;
            storedTimestamp.ShouldBe(expectedTimestamp);
        }
    }

    public class OnCloseAsync(RedisFixture fixture) : RedisSocketManagerTests(fixture)
    {
        [Fact]
        public async Task OnCloseAsync_RemovesSocketAndUpdatesClientSockets()
        {
            // Arrange
            var socket = CreateMockSocket(Guid.NewGuid());
            var socketId = socket.ConnectionInfo.Id;
            const string clientId = "test-client";

            // Pre-condition
            await _manager.OnOpenAsync(socket, clientId);

            // Act
            await _manager.OnCloseAsync(socket, clientId);

            // Assert
            (await _db.KeyExistsAsync(RedisSocketKeys.SocketHash(socketId))).ShouldBeFalse();
            (await _db.KeyExistsAsync(RedisSocketKeys.SocketToClientMap(socketId))).ShouldBeFalse();
            (await _db.SetContainsAsync(RedisSocketKeys.ClientSocketsSet(clientId), socketId.ToString()))
                .ShouldBeFalse();
            (await _db.SetContainsAsync(RedisSocketKeys.ActiveSocketsSetKey, socketId.ToString())).ShouldBeFalse();
        }

        [Fact]
        public async Task OnCloseAsync_ThrowsArgumentNullException_WhenSocketIsNull()
        {
            // Arrange
            IWebSocketConnection? socket = null;
            var clientId = "any-client";

            // Act & Assert
            await Should.ThrowAsync<ArgumentNullException>(() => _manager.OnCloseAsync(socket!, clientId));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("\t")]
        public async Task OnCloseAsync_ThrowsArgumentException_WhenClientIdIsNullOrEmptyOrWhitespace(
            string? invalidClientId)
        {
            // Arrange
            var socket = CreateMockSocket(Guid.NewGuid());
            await _manager.OnOpenAsync(socket, "validClientDuringOpen");

            // Act & Assert
            await Should.ThrowAsync<ArgumentException>(() => _manager.OnCloseAsync(socket, invalidClientId!));
        }

        [Fact]
        public async Task OnCloseAsync_RemovesOnlyTargetSocket_WhenClientHasMultipleSockets()
        {
            // Arrange
            var clientId = "multi-socket-client-close";
            var socket1 = CreateMockSocket(Guid.NewGuid());
            var socket2 = CreateMockSocket(Guid.NewGuid());
            var socketId1 = socket1.ConnectionInfo.Id;
            var socketId2 = socket2.ConnectionInfo.Id;

            await _manager.OnOpenAsync(socket1, clientId);
            await _manager.OnOpenAsync(socket2, clientId);

            // Act
            await _manager.OnCloseAsync(socket1, clientId);

            // Assert
            var clientSocketsKey = RedisSocketKeys.ClientSocketsSet(clientId);

            // Keys for socket 1 burde ikke være der
            (await _db.KeyExistsAsync(RedisSocketKeys.SocketHash(socketId1))).ShouldBeFalse();
            (await _db.KeyExistsAsync(RedisSocketKeys.SocketToClientMap(socketId1))).ShouldBeFalse();
            (await _db.SetContainsAsync(clientSocketsKey, socketId1.ToString())).ShouldBeFalse();
            (await _db.SetContainsAsync(RedisSocketKeys.ActiveSocketsSetKey, socketId1.ToString())).ShouldBeFalse();

            // Keys for socket 2 burde stadig være der
            (await _db.KeyExistsAsync(RedisSocketKeys.SocketHash(socketId2))).ShouldBeTrue();
            (await _db.KeyExistsAsync(RedisSocketKeys.SocketToClientMap(socketId2))).ShouldBeTrue();
            string storedClientId2 = (await _db.StringGetAsync(RedisSocketKeys.SocketToClientMap(socketId2)))!;
            storedClientId2.ShouldBe(clientId);
            (await _db.SetContainsAsync(clientSocketsKey, socketId2.ToString())).ShouldBeTrue();
            (await _db.SetContainsAsync(RedisSocketKeys.ActiveSocketsSetKey, socketId2.ToString())).ShouldBeTrue();

            // Client burde kunne indeholde socket 2
            var members = await _db.SetMembersAsync(clientSocketsKey);
            members.Length.ShouldBe(1);
            members[0].ToString().ShouldBe(socketId2.ToString());
        }

        [Fact]
        public async Task OnCloseAsync_HandlesMismatchingClientIdAndRemovesSocket()
        {
            // Arrange
            var socket = CreateMockSocket(Guid.NewGuid());
            var socketId = socket.ConnectionInfo.Id;
            var actualClientId = "client-A";
            var incorrectClientId = "client-B";

            await _manager.OnOpenAsync(socket, actualClientId);

            // Act
            await _manager.OnCloseAsync(socket, incorrectClientId);

            // Assert
            (await _db.KeyExistsAsync(RedisSocketKeys.SocketHash(socketId))).ShouldBeFalse();
            (await _db.KeyExistsAsync(RedisSocketKeys.SocketToClientMap(socketId))).ShouldBeFalse();
            (await _db.SetContainsAsync(RedisSocketKeys.ActiveSocketsSetKey, socketId.ToString())).ShouldBeFalse();

            (await _db.SetContainsAsync(RedisSocketKeys.ClientSocketsSet(incorrectClientId), socketId.ToString()))
                .ShouldBeFalse();
            (await _db.SetContainsAsync(RedisSocketKeys.ClientSocketsSet(actualClientId), socketId.ToString()))
                .ShouldBeFalse();
        }

        [Fact]
        public async Task OnCloseAsync_IsIdempotent_WhenCalledTwiceWithSameSocket()
        {
            // Arrange
            var socket = CreateMockSocket(Guid.NewGuid());
            var socketId = socket.ConnectionInfo.Id;
            const string clientId = "idempotent-close-client";
            await _manager.OnOpenAsync(socket, clientId);

            // Act
            await _manager.OnCloseAsync(socket, clientId);
            await _manager.OnCloseAsync(socket, clientId);

            // Assert
            (await _db.KeyExistsAsync(RedisSocketKeys.SocketHash(socketId))).ShouldBeFalse();
            (await _db.KeyExistsAsync(RedisSocketKeys.SocketToClientMap(socketId))).ShouldBeFalse();
            (await _db.SetContainsAsync(RedisSocketKeys.ClientSocketsSet(clientId), socketId.ToString()))
                .ShouldBeFalse();
            (await _db.SetContainsAsync(RedisSocketKeys.ActiveSocketsSetKey, socketId.ToString())).ShouldBeFalse();
            (await _db.KeyExistsAsync(RedisSocketKeys.ClientSocketsSet(clientId))).ShouldBeFalse();
        }

        [Fact]
        public async Task OnCloseAsync_DoesNothing_WhenSocketNotOpen()
        {
            // Arrange
            var socket = CreateMockSocket(Guid.NewGuid());
            var socketId = socket.ConnectionInfo.Id;
            const string clientId = "never-opened-client";

            // Act
            await _manager.OnCloseAsync(socket, clientId);

            // Assert
            (await _db.KeyExistsAsync(RedisSocketKeys.SocketHash(socketId))).ShouldBeFalse();
            (await _db.KeyExistsAsync(RedisSocketKeys.SocketToClientMap(socketId))).ShouldBeFalse();
            (await _db.KeyExistsAsync(RedisSocketKeys.ClientSocketsSet(clientId))).ShouldBeFalse();
            (await _db.SetContainsAsync(RedisSocketKeys.ActiveSocketsSetKey, socketId.ToString())).ShouldBeFalse();

            var server = _fixture.Redis.GetServer(_fixture.Container.GetConnectionString());
            var keyCount = server.Keys(database: _db.Database).Count();
            keyCount.ShouldBe(0);
        }
    }

    public class GetClientIdAsync(RedisFixture fixture) : RedisSocketManagerTests(fixture)
    {
        [Fact]
        public async Task GetClientIdAsync_ReturnsCorrectClientId_WhenSocketExists()
        {
            // Arrange
            var socket = CreateMockSocket(Guid.NewGuid());
            var expectedClientId = "client-lookup-test";
            await _manager.OnOpenAsync(socket, expectedClientId);

            // Act
            var actualClientId = await _manager.GetClientIdAsync(socket);

            // Assert
            actualClientId.ShouldBe(expectedClientId);
        }

        [Fact]
        public async Task GetClientIdAsync_ThrowsKeyNotFoundException_WhenSocketNotRegistered()
        {
            // Arrange
            var socket = CreateMockSocket(Guid.NewGuid());

            // Act & Assert
            await Should.ThrowAsync<KeyNotFoundException>(() =>
                _manager.GetClientIdAsync(socket)
            );
        }

        [Fact]
        public async Task GetClientIdAsync_ThrowsArgumentNullException_WhenSocketIsNull()
        {
            // Arrange
            IWebSocketConnection? socket = null;

            // Act & Assert
            await Should.ThrowAsync<ArgumentNullException>(() =>
                _manager.GetClientIdAsync(socket!)
            );
        }
    }

    public class TryGetClientIdAsync(RedisFixture fixture) : RedisSocketManagerTests(fixture)
    {
        [Fact]
        public async Task TryGetClientIdAsync_Tuple_ReturnsTrueAndCorrectId_WhenSocketExists()
        {
            // Arrange
            var socket = CreateMockSocket(Guid.NewGuid());
            var expectedClientId = "try-client-lookup-test";
            await _manager.OnOpenAsync(socket, expectedClientId);

            // Act
            var result = await _manager.TryGetClientIdAsync(socket);

            // Assert
            result.Found.ShouldBeTrue();
            result.ClientId.ShouldNotBeNull();
            result.ClientId.ShouldBe(expectedClientId);
        }

        [Fact]
        public async Task TryGetClientIdAsync_Tuple_ReturnsFalse_WhenSocketNotRegistered()
        {
            // Arrange
            var socket = CreateMockSocket(Guid.NewGuid());

            // Act
            var result = await _manager.TryGetClientIdAsync(socket);

            // Assert
            result.Found.ShouldBeFalse();
            result.ClientId.ShouldBeNull();
        }

        [Fact]
        public async Task TTryGetClientIdAsync_Tuple_ThrowsArgumentNullException_WhenSocketIsNull()
        {
            // Arrange
            IWebSocketConnection? socket = null;

            // Act & Assert
            await Should.ThrowAsync<ArgumentNullException>(() =>
                    _manager.TryGetClientIdAsync(socket!)
            );
        }
    }

    public class SubscribeAsync(RedisFixture fixture) : RedisSocketManagerTests(fixture)
    {
        [Fact]
        public async Task SubscribeAsync_AddsClientAndTopicToSets()
        {
            // Arrange
            var clientId = "client-sub-1";
            var topic = "topic-a";

            // Act
            await _manager.SubscribeAsync(clientId, topic);

            // Assert
            var clientTopics = await _db.SetMembersAsync(RedisSocketKeys.ClientTopicsSet(clientId));
            clientTopics.ShouldHaveSingleItem().ToString().ShouldBe(topic); 

            var topicSubscribers = await _db.SetMembersAsync(RedisSocketKeys.TopicSubscribersSet(topic));
            topicSubscribers.ShouldHaveSingleItem().ToString().ShouldBe(clientId);
            
            var allTopics = await _db.SetMembersAsync(RedisSocketKeys.AllTopicsSetKey);
            allTopics.ShouldContain(topic);
        }
        
        [Fact]
        public async Task SubscribeAsync_IsIdempotent()
        {
            // Arrange
            var clientId = "client-sub-idem";
            var topic = "topic-idem";

            // Act
            await _manager.SubscribeAsync(clientId, topic);
            await _manager.SubscribeAsync(clientId, topic); // Subscribe igen

            // Assert
            var clientTopics = await _db.SetMembersAsync(RedisSocketKeys.ClientTopicsSet(clientId));
            clientTopics.Length.ShouldBe(1); 
            clientTopics.ShouldHaveSingleItem().ToString().ShouldBe(topic);
            
            var topicSubscribers = await _db.SetMembersAsync(RedisSocketKeys.TopicSubscribersSet(topic));
            topicSubscribers.Length.ShouldBe(1); 
            topicSubscribers.ShouldHaveSingleItem().ToString().ShouldBe(clientId);
        }
        
        [Theory]
        [InlineData(null, "topic")]
        [InlineData("client", null)]
        [InlineData("", "topic")]
        [InlineData("client", "")]
        [InlineData(" ", "topic")]
        [InlineData("client", " ")]
        public async Task SubscribeAsync_ThrowsArgumentException_ForInvalidArgs(string? clientId, string? topic)
        {
            // Act & Assert
            await Should.ThrowAsync<ArgumentException>(() =>
                    _manager.SubscribeAsync(clientId!, topic!) 
            );
        }
    }

    public class UnsubscribeAsync(RedisFixture fixture) : RedisSocketManagerTests(fixture)
    {
        [Fact]
        public async Task UnsubscribeAsync_RemovesClientAndTopicFromSets()
        {
            // Arrange
            var clientId = "client-unsub-1";
            var topic = "topic-b";
            
            // Pre-condition
            await _manager.SubscribeAsync(clientId, topic);

            // Tjek pre-condition
            (await _db.SetContainsAsync(RedisSocketKeys.ClientTopicsSet(clientId), topic)).ShouldBeTrue();
            (await _db.SetContainsAsync(RedisSocketKeys.TopicSubscribersSet(topic), clientId)).ShouldBeTrue();

            // Act
            await _manager.UnsubscribeAsync(clientId, topic);

            // Assert
            (await _db.SetContainsAsync(RedisSocketKeys.ClientTopicsSet(clientId), topic)).ShouldBeFalse();
            (await _db.SetContainsAsync(RedisSocketKeys.TopicSubscribersSet(topic), clientId)).ShouldBeFalse();

            // Tjek at sets er tomme
            (await _db.SetLengthAsync(RedisSocketKeys.ClientTopicsSet(clientId))).ShouldBe(0);
            (await _db.SetLengthAsync(RedisSocketKeys.TopicSubscribersSet(topic))).ShouldBe(0);
        }
        
        [Fact]
        public async Task UnsubscribeAsync_IsIdempotent()
        {
            // Arrange
            var clientId = "client-unsub-idem";
            var topic = "topic-unsub-idem";

            // Act
            await _manager.UnsubscribeAsync(clientId, topic);
            await _manager.UnsubscribeAsync(clientId, topic); // Kald igen

            // Assert
            (await _db.KeyExistsAsync(RedisSocketKeys.ClientTopicsSet(clientId))).ShouldBeFalse();
            (await _db.KeyExistsAsync(RedisSocketKeys.TopicSubscribersSet(topic))).ShouldBeFalse();
        }
        
        [Theory]
        [InlineData(null, "topic")]
        [InlineData("client", null)]
        [InlineData("", "topic")]
        [InlineData("client", "")]
        [InlineData(" ", "topic")]
        [InlineData("client", " ")]
        public async Task UnsubscribeAsync_ThrowsArgumentException_ForInvalidArgs(string? clientId, string? topic)
        {
            // Act & Assert
            await Should.ThrowAsync<ArgumentException>(() =>
                    _manager.UnsubscribeAsync(clientId!, topic!)
            );
        }
    }

    public class GetTopicsAsync(RedisFixture fixture) : RedisSocketManagerTests(fixture)
    {
        [Fact]
        public async Task GetTopicsAsync_ReturnsSubscribedTopics()
        {
            // Arrange
            var clientId = "client-get-topics";
            var topic1 = "pizza";
            var topic2 = "kakao";
            await _manager.SubscribeAsync(clientId, topic1);
            await _manager.SubscribeAsync(clientId, topic2);

            // Act
            var topics = await _manager.GetTopicsAsync(clientId);

            // Assert
            topics.ShouldNotBeNull();
            topics.Count.ShouldBe(2);
            topics.ShouldBeUnique();
            topics.ShouldContain(topic1);
            topics.ShouldContain(topic2);
        }

        [Fact]
        public async Task GetTopicsAsync_ReturnsEmptyList_WhenNotSubscribed()
        {
            // Arrange
            var clientId = "client-no-topics";

            // Act
            var topics = await _manager.GetTopicsAsync(clientId);

            // Assert
            topics.ShouldNotBeNull();
            topics.ShouldBeEmpty();
        }
    }

    public class GetSubscribersAsync(RedisFixture fixture) : RedisSocketManagerTests(fixture)
    {
        [Fact]
        public async Task GetSubscribersAsync_ReturnsSubscribedClients()
        {
            // Arrange
            var topic = "topic-get-subs";
            var client1 = "fedtmule";
            var client2 = "anders and";
            await _manager.SubscribeAsync(client1, topic);
            await _manager.SubscribeAsync(client2, topic);

            // Act
            var subscribers = await _manager.GetSubscribersAsync(topic);

            // Assert
            subscribers.ShouldNotBeNull();
            subscribers.Count.ShouldBe(2);
            subscribers.ShouldBeUnique();
            subscribers.ShouldContain(client1);
            subscribers.ShouldContain(client2);
        }
        
        [Fact]
        public async Task GetSubscribersAsync_ReturnsEmptyList_ForEmptyOrUnknownTopic()
        {
            // Arrange
            var topic = "topic-no-subs";

            // Act
            var subscribers = await _manager.GetSubscribersAsync(topic);

            // Assert
            subscribers.ShouldNotBeNull();
            subscribers.ShouldBeEmpty();
        }
    }

    public class SendToClientAsync(RedisFixture fixture) : RedisSocketManagerTests(fixture)
    {
        private async Task<(Moq.Mock<IWebSocketConnection> Mock, IWebSocketConnection Socket, Guid SocketId)> AddClientSocketAsync(string clientId)
        {
            var id = Guid.NewGuid();
            var mock = new Mock<IWebSocketConnection>();
            mock.SetupGet(s => s.ConnectionInfo.Id).Returns(id);
            mock.SetupGet(s => s.IsAvailable).Returns(true);
            mock.Setup(s => s.Send(It.IsAny<string>())).Returns(Task.CompletedTask);

            var socket = mock.Object;
            await _manager.OnOpenAsync(socket, clientId);
            return (mock, socket, id);
        }
        
        private static string SerializeTestMessage<T>(T message) where T : class
        {
            return JsonSerializer.Serialize(new MessageWrapper<T>(message));
        }
        
        private sealed record TestMessage { public string? Content { get; init; } }
        
        [Fact]
        public async Task SendToClientAsync_SendsToSingleLocalSocket()
        {
            // Arrange
            var clientId = "client-direct-1";
            var clientConn = await AddClientSocketAsync(clientId);
            var message = new TestMessage { Content = "Direct Message" };
            var expectedJson = SerializeTestMessage(message);

            // Act
            await _manager.SendToClientAsync(clientId, message);

            // Assert
            clientConn.Mock.Verify(s => s.Send(expectedJson), Times.Once);
        }
        
        [Fact]
        public async Task SendToClientAsync_SendsToMultipleLocalSocketsForClient()
        {
            // Arrange
            var clientId = "client-direct-multi";
            var conn1 = await AddClientSocketAsync(clientId);
            var conn2 = await AddClientSocketAsync(clientId);
            var message = new TestMessage { Content = "Direct Multi Message" };
            var expectedJson = SerializeTestMessage(message);

            // Act
            await _manager.SendToClientAsync(clientId, message);

            // Assert
            conn1.Mock.Verify(s => s.Send(expectedJson), Times.Once);
            conn2.Mock.Verify(s => s.Send(expectedJson), Times.Once);
        }
        
        [Fact]
        public async Task SendToClientAsync_DoesNotSendToOtherClients()
        {
            // Arrange
            var targetClientId = "client-direct-target";
            var otherClientId = "client-direct-other";
            var targetConn = await AddClientSocketAsync(targetClientId);
            var otherConn = await AddClientSocketAsync(otherClientId);
            var message = new TestMessage { Content = "Targeted Message" };

            // Act
            await _manager.SendToClientAsync(targetClientId, message);

            // Assert
            targetConn.Mock.Verify(s => s.Send(It.IsAny<string>()), Times.Once);
            otherConn.Mock.Verify(s => s.Send(It.IsAny<string>()), Times.Never);
        }
        
        [Fact]
        public async Task SendToClientAsync_SkipsUnavailableLocalSocket()
        {
            // Arrange
            var clientId = "client-direct-skip";
            var connAvailable = await AddClientSocketAsync(clientId);
            var connUnavailable = await AddClientSocketAsync(clientId);
            
            connUnavailable.Mock.SetupGet(s => s.IsAvailable).Returns(false);

            var message = new TestMessage { Content = "Skip Unavailable" };
            var expectedJson = SerializeTestMessage(message);

            // Act
            await _manager.SendToClientAsync(clientId, message);

            // Assert
            connAvailable.Mock.Verify(s => s.Send(expectedJson), Times.Once);
            connUnavailable.Mock.Verify(s => s.Send(It.IsAny<string>()), Times.Never);
        }
        
        [Fact]
        public async Task SendToClientAsync_DoesNothing_WhenClientSocketsAreNotLocal()
        {
            // Arrange
            var clientId = "client-direct-remote";
            var socketId = Guid.NewGuid();
            var message = new TestMessage { Content = "Remote Socket Test" };
            
            await _db.SetAddAsync(RedisSocketKeys.ClientSocketsSet(clientId), socketId.ToString());

            // Act
            await _manager.SendToClientAsync(clientId, message);

            // Assert
            (await _db.SetLengthAsync(RedisSocketKeys.ClientSocketsSet(clientId))).ShouldBe(1); // Ensure state wasn't wrongly cleaned up
        }

        [Fact]
        public async Task SendToClientAsync_ThrowsArgumentNullException_ForNullMessage()
        {
            // Arrange
            var clientId = "client-direct-null";
            TestMessage message = null!;

            // Act & Assert
            await Should.ThrowAsync<ArgumentNullException>(() =>
                _manager.SendToClientAsync(clientId, message)
            );
        }
        
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public async Task SendToClientAsync_ThrowsArgumentException_ForInvalidClientId(string? invalidClientId)
        {
            // Arrange
            var message = new TestMessage { Content = "Invalid Client Test" };

            // Act & Assert
            await Should.ThrowAsync<ArgumentException>(() =>
                _manager.SendToClientAsync(invalidClientId!, message)
            );
        }
    }
    
    private static IWebSocketConnection CreateMockSocket(Guid id)
    {
        var mock = new Mock<IWebSocketConnection>();
        mock.Setup(s => s.ConnectionInfo.Id).Returns(id);
        mock.Setup(s => s.IsAvailable).Returns(true);
        return mock.Object;
    }
}