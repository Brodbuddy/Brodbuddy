using Api.Websocket.EventHandlers;
using Brodbuddy.WebSocket.State;
using Core.Messaging;
using Fleck;
using Moq;
using Shouldly;

namespace Api.Websocket.Tests.EventHandlers;

public class SourdoughDataHandlerTests
{
    private readonly Mock<ISocketManager> _managerMock;
    private readonly Mock<IWebSocketConnection> _socketMock;
    private readonly SourdoughDataHandler _handler;
    private readonly string _clientId;
    private readonly Guid _userId;
    private readonly Guid _connectionId;

    public SourdoughDataHandlerTests()
    {
        _managerMock = new Mock<ISocketManager>();
        _socketMock = new Mock<IWebSocketConnection>();
        _handler = new SourdoughDataHandler(_managerMock.Object);
        _clientId = "kakaoId";
        _userId = Guid.NewGuid();
        _connectionId = Guid.NewGuid();

        var mockConnectionInfo = new Mock<IWebSocketConnectionInfo>();
        mockConnectionInfo.Setup(ci => ci.Id).Returns(_connectionId);
        _socketMock.Setup(s => s.ConnectionInfo).Returns(mockConnectionInfo.Object);
    }

    public class HandleAsync : SourdoughDataHandlerTests
    {
        [Fact]
        public async Task HandleAsync_SubscribesToCorrectTopic()
        {
            // Arrange
            var request = new SubscribeToSourdoughData(_userId);
            var expectedTopic = WebSocketTopics.User.SourdoughData(_userId);

            // Act
            await _handler.HandleAsync(request, _clientId, _socketMock.Object);

            // Assert
            _managerMock.Verify(m => m.SubscribeAsync(_clientId, expectedTopic), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_ReturnsCorrectResponse()
        {
            // Arrange
            var request = new SubscribeToSourdoughData(_userId);

            // Act
            var response = await _handler.HandleAsync(request, _clientId, _socketMock.Object);

            // Assert
            response.ShouldNotBeNull();
            response.UserId.ShouldBe(_userId);
            response.ConnectionId.ShouldBe(_connectionId);
        }
    }

    public class GetTopicKey : SourdoughDataHandlerTests
    {
        [Fact]
        public void GetTopicKey_ReturnsCorrectKey()
        {
            // Arrange
            var request = new SubscribeToSourdoughData(_userId);
            var expectedTopic = WebSocketTopics.User.SourdoughData(_userId);

            // Act
            var topic = _handler.GetTopicKey(request, _clientId);

            // Assert
            topic.ShouldBe(expectedTopic);
        }
    }
} 