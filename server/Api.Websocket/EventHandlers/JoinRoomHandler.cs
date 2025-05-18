using Api.Websocket.Auth;
using Api.Websocket.Spec;
using Brodbuddy.WebSocket.Auth;
using Brodbuddy.WebSocket.Core;
using Brodbuddy.WebSocket.State;
using Core.Entities;
using Fleck;
using FluentValidation;

namespace Api.Websocket.EventHandlers;

public record BroadcastTest(string RoomId, string Status) : IBroadcastMessage;
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

// [Authorize(Roles = Role.Member)]
[AllowAnonymous]
public class JoinRoomHandler(ISocketManager manager) : ISubscriptionHandler<JoinRoom, UserJoined>
{
    public string GetTopicKey(JoinRoom request, string clientId) => $"room:{request.RoomId}";
    
    public async Task<UserJoined> HandleAsync(JoinRoom incoming, string clientId, IWebSocketConnection socket)
    {
        var topic = GetTopicKey(incoming, clientId); 
        
        var existingTopics = await manager.GetTopicsAsync(clientId);
        var alreadySubscribed = existingTopics.Contains(topic);
        
        await manager.SubscribeAsync(clientId, topic);
        
        if (!alreadySubscribed) await manager.BroadcastAsync(topic, new BroadcastTest(incoming.RoomId, "User joined"));

        return new UserJoined(incoming.RoomId, incoming.Username, socket.ConnectionInfo.Id);
    }
}