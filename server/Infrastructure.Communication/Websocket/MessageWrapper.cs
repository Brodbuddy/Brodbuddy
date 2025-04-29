namespace Infrastructure.Communication.Websocket;

public record MessageWrapper<T>(string Type, T Payload)
{
    public MessageWrapper(T payload) : this(payload?.GetType().Name ?? "Unknown", payload) { }
}