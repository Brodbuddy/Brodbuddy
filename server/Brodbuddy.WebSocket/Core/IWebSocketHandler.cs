using Fleck;

namespace Brodbuddy.WebSocket.Core;

public interface IWebSocketHandler
{
    string MessageType => GetType().Name.Replace("Handler", "");
}

public interface IWebSocketHandler<in TRequest, TResponse> : IWebSocketHandler
    where TRequest : class
    where TResponse : class
{
    Task<TResponse> HandleAsync(TRequest incoming, string clientId, IWebSocketConnection socket);
}

public interface ISubscriptionHandler<in TRequest, TResponse> : IWebSocketHandler<TRequest, TResponse>
    where TRequest : class 
    where TResponse : class 
{
    string GetTopicKey(TRequest request, string clientId);
}

public interface IUnsubscriptionHandler<in TRequest, TResponse> : IWebSocketHandler<TRequest, TResponse>
    where TRequest : class 
    where TResponse : class 
{
    string GetTopicKey(TRequest request, string clientId);
}