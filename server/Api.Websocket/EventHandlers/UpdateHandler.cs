using Application.Interfaces.Websocket;
using Brodbuddy.WebSocket.Core;
using Fleck;
using FluentValidation;

namespace Api.Websocket.EventHandlers;

public record IncomingUpdate(string Data, int Id );
public record OutgoingUpdate(string Data );

public class IncommingUpdateValidator :AbstractValidator<IncomingUpdate>
{
    public IncommingUpdateValidator()
    {
        RuleFor(x => x.Data).NotEmpty();
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Data).Matches(@"^[a-zA-Z0-9]+$").WithMessage("Data must be alphanumeric");
    }
}

public class UpdateHandler(IConnectionManager connectionManager) : IWebSocketHandler<IncomingUpdate, OutgoingUpdate> 
{
    
    public string MessageType => "updateDummy";
    
    
    public Task<OutgoingUpdate> HandleAsync(IncomingUpdate incoming, IWebSocketConnection socket)
    {
        var clientId = connectionManager.GetClientIdFromSocket(socket);
        
        return Task.FromResult(new OutgoingUpdate($"Received: {incoming.Data} from {clientId}"));
    }

   
}