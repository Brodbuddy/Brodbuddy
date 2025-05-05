using Brodbuddy.WebSocket.Auth;
using Brodbuddy.WebSocket.Core;
using Brodbuddy.WebSocket.State;
using Fleck;
using FluentValidation;

namespace Api.Websocket.EventHandlers;

public record JoinRoom(string RoomId, string Username);
public record UserJoined(string RoomId, string Username, Guid ConnectionId); 

public class JoinRoomValidator : AbstractValidator<JoinRoom> 
{
    public JoinRoomValidator()
    {
        RuleFor(x => x.RoomId).NotEmpty();
        RuleFor(x => x.Username).NotEmpty()
            .MinimumLength(2)
            .MaximumLength(50)
            .Must(username => !string.IsNullOrWhiteSpace(username))
            .WithMessage("Username cannot be whitespace only");
    }
}

[Authorize(Roles = "user")]
public class JoinRoomHandler(ISocketManager manager) : IWebSocketHandler<JoinRoom, UserJoined>
{
    public async Task<UserJoined> HandleAsync(JoinRoom incoming, string clientId, IWebSocketConnection socket)
    {
        var roomTopic = $"room:{incoming.RoomId}";
        await manager.SubscribeAsync(clientId, roomTopic);
        
        var notification = new UserJoined(incoming.RoomId, incoming.Username, socket.ConnectionInfo.Id);
        await manager.BroadcastAsync(roomTopic, notification);

        return notification;
    }
}