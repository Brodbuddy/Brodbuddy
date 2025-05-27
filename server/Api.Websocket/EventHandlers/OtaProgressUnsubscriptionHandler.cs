using System.Text.Json.Serialization;
using Api.Websocket.Extensions;
using Brodbuddy.WebSocket.Auth;
using Brodbuddy.WebSocket.Core;
using Brodbuddy.WebSocket.State;
using Core.Messaging;
using Fleck;
using FluentValidation;

namespace Api.Websocket.EventHandlers;

public record UnsubscribeFromOtaProgress([property: JsonPropertyName("analyzerId")] string AnalyzerId);
public record OtaProgressUnsubscribed([property: JsonPropertyName("topic")] string Topic);

public class UnsubscribeFromOtaProgressValidator : AbstractValidator<UnsubscribeFromOtaProgress>
{
    public UnsubscribeFromOtaProgressValidator()
    {
        RuleFor(x => x.AnalyzerId)
            .NotEmpty().WithMessage("Analyzer ID is required")
            .MustBeValidGuid();
    }
}

public class OtaProgressUnsubscriptionHandler(ISocketManager manager) : IWebSocketHandler<UnsubscribeFromOtaProgress, OtaProgressUnsubscribed>
{
    public async Task<OtaProgressUnsubscribed> HandleAsync(UnsubscribeFromOtaProgress incoming, string clientId, IWebSocketConnection socket)
    {
        var topic = WebSocketTopics.User.OtaProgress(Guid.Parse(incoming.AnalyzerId));
        await manager.UnsubscribeAsync(clientId, topic);
        return new OtaProgressUnsubscribed(topic);
    }
}