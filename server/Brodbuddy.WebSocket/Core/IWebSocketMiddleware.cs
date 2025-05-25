using Brodbuddy.WebSocket.Auth;
using Fleck;
using FluentValidation;

namespace Brodbuddy.WebSocket.Core;

public interface IWebSocketMiddleware
{
    Task<bool> InvokeAsync(IWebSocketConnection socket, string message, Func<Task> next);
}

public class MiddlewareContext
{
    public IWebSocketConnection Socket { get; set; }
    public string Message { get; set; }
    public string MessageType { get; set; }
    public object Request { get; set; }
    public string ClientId { get; set; }
    public string RequestId { get; set; }
    public (Type HandlerType, IValidator? Validator, AuthorizeAttribute? AuthAttribute, bool HasAllowAnonymous) Registration { get; set; }
    public object? Response { get; set; }
    public string? TopicKey { get; set; } 
}