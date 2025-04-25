using Brodbuddy.WebSocket.Core;
using Fleck;
using FluentValidation;

namespace Api.Websocket.EventHandlers;

public record IncomingUpdate(string Data, int Id);

public record OutgoingUpdate(string Data);

public class IncomingUpdateValidator : AbstractValidator<IncomingUpdate>
{
    public IncomingUpdateValidator()
    {
        RuleFor(x => x.Data).NotEmpty();
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Data).Matches(@"^[a-zA-Z0-9]+$").WithMessage("Data must be alphanumeric");
    }
}

public class UpdateHandler : IWebSocketHandler<IncomingUpdate, OutgoingUpdate>
{
    public string MessageType => "updateDummy";
    
    public Task<OutgoingUpdate> HandleAsync(IncomingUpdate incoming, string clientId, IWebSocketConnection socket)
    {
        return Task.FromResult(new OutgoingUpdate($"Received: {incoming.Data}"));
    }
}