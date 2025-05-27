using System.Text.Json.Serialization;
using Brodbuddy.WebSocket.Auth;
using Brodbuddy.WebSocket.Core;
using Brodbuddy.WebSocket.State;
using Core.Messaging;
using Fleck;

namespace Api.Websocket.EventHandlers;

public record SubscribeToFirmwareNotifications([property: JsonPropertyName("clientType")] string ClientType = "WebClient");
public record FirmwareNotificationsSubscribed([property: JsonPropertyName("topic")] string Topic);

public class FirmwareNotificationSubscriptionHandler(ISocketManager manager) : IWebSocketHandler<SubscribeToFirmwareNotifications, FirmwareNotificationsSubscribed>
{
    public async Task<FirmwareNotificationsSubscribed> HandleAsync(SubscribeToFirmwareNotifications incoming, string clientId, IWebSocketConnection socket)
    {
        await manager.SubscribeAsync(clientId, WebSocketTopics.Everyone.FirmwareAvailable);
        return new FirmwareNotificationsSubscribed(WebSocketTopics.Everyone.FirmwareAvailable);
    }
}