using System.Text.Json.Serialization;
using Brodbuddy.WebSocket.Auth;
using Brodbuddy.WebSocket.Core;
using Brodbuddy.WebSocket.State;
using Core.Messaging;
using Fleck;

namespace Api.Websocket.EventHandlers;

public record SubscribeToFirmwareNotifications([property: JsonPropertyName("clientType")] string ClientType = "WebClient");
public record FirmwareNotificationsSubscribed([property: JsonPropertyName("topic")] string Topic);

public class FirmwareNotificationSubscriptionHandler(ISocketManager manager) : ISubscriptionHandler<SubscribeToFirmwareNotifications, FirmwareNotificationsSubscribed>
{
    public string GetTopicKey(SubscribeToFirmwareNotifications request, string clientId) => WebSocketTopics.Everyone.FirmwareAvailable;
    
    public async Task<FirmwareNotificationsSubscribed> HandleAsync(SubscribeToFirmwareNotifications incoming, string clientId, IWebSocketConnection socket)
    {
        var topic = GetTopicKey(incoming, clientId);
        await manager.SubscribeAsync(clientId, topic);
        return new FirmwareNotificationsSubscribed(topic);
    }
}