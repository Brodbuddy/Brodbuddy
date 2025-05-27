using System.Text.Json.Serialization;
using Api.Websocket.Extensions;
using Brodbuddy.WebSocket.Auth;
using Brodbuddy.WebSocket.Core;
using Brodbuddy.WebSocket.State;
using Core.Messaging;
using Fleck;
using FluentValidation;

namespace Api.Websocket.EventHandlers;

public record SubscribeToOtaProgress([property: JsonPropertyName("analyzerId")] string AnalyzerId);
public record OtaProgressSubscribed([property: JsonPropertyName("topic")] string Topic);

public class SubscribeToOtaProgressValidator : AbstractValidator<SubscribeToOtaProgress>
{
    public SubscribeToOtaProgressValidator()
    {
        RuleFor(x => x.AnalyzerId)
            .NotEmpty().WithMessage("Analyzer ID is required")
            .MustBeValidGuid();
    }
}

public class OtaProgressSubscriptionHandler(ISocketManager manager) : IWebSocketHandler<SubscribeToOtaProgress, OtaProgressSubscribed>
{
    public async Task<OtaProgressSubscribed> HandleAsync(SubscribeToOtaProgress incoming, string clientId, IWebSocketConnection socket)
    {
        var topic = WebSocketTopics.User.OtaProgress(Guid.Parse(incoming.AnalyzerId));
        await manager.SubscribeAsync(clientId, topic);
        return new OtaProgressSubscribed(topic);
    }
}