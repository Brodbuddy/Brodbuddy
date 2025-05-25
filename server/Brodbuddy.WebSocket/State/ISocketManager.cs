using System.Diagnostics.CodeAnalysis;
using Fleck;

namespace Brodbuddy.WebSocket.State;

/// <summary>
/// Managed WebSocket connections, handling opening and closing of connections
/// and maintaining the relationship between client IDs and socket connections.
/// </summary>
public interface ISocketManager
{
    /// <summary>
    /// Registers a new WebSocket connection with a client ID.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown if socket is null.</exception>
    /// <exception cref="ArgumentException">Thrown if clientId is null, empty, or whitespace.</exception>
    Task OnOpenAsync(IWebSocketConnection socket, string clientId);

    /// <summary>
    /// Handles the closing of a WebSocket connection
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown if socket is null.</exception>
    /// <exception cref="ArgumentException">Thrown if clientId is null, empty, or whitespace.</exception>
    Task OnCloseAsync(IWebSocketConnection socket, string clientId);

    /// <summary>
    /// Attempts to retrieve a WebSocket connection managed by this specific server instance, based on its unique socket ID.
    /// </summary>
    bool TryGetLocalSocket(Guid socketId, [MaybeNullWhen(false)] out IWebSocketConnection socket);

    /// <summary>
    /// Gets the client ID associated with the specified WebSocket connection
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown if socket is null.</exception>
    /// <exception cref="KeyNotFoundException">Thrown if the socket is not registered or has no associated client ID.</exception>
    Task<string> GetClientIdAsync(IWebSocketConnection socket);

    /// <summary>
    /// Tries to get the client ID associated with the specified WebSocket connection.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown if socket is null.</exception>
    Task<(bool Found, string? ClientId)> TryGetClientIdAsync(IWebSocketConnection socket);

    /// <summary>
    /// Subscribes a client to a specified topic.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown if clientId or topic is null or whitespace.</exception>
    /// <remarks>Operation is idempotent; subscribing multiple times has no additional effect.</remarks>
    Task SubscribeAsync(string clientId, string topic);

    /// <summary>
    /// Unsubscribes a client from a specified topic.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown if clientId or topic is null or whitespace.</exception>
    /// <remarks>Operation is idempotent; unsubscribing when not subscribed has no effect.</remarks>
    Task UnsubscribeAsync(string clientId, string topic);

    /// <summary>
    /// Gets a list of topics the specified client is currently subscribed to.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown if clientId is null or whitespace.</exception>
    Task<IReadOnlyList<string>> GetTopicsAsync(string clientId);

    /// <summary>
    /// Gets a list of client IDs currently subscribed to the specified topic.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown if topic is null or whitespace.</exception>
    Task<IReadOnlyList<string>> GetSubscribersAsync(string topic);

    /// <summary>
    /// Broadcasts a message to all clients currently subscribed to the specified topic.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message being sent.</typeparam>
    /// <exception cref="ArgumentException">Thrown if topic is null or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown if message is null.</exception>
    /// <remarks>
    /// This method attempts to send to all active connections for subscribed clients.
    /// Delivery is not guaranteed if a connection becomes unavailable during the process
    /// </remarks>
    Task BroadcastAsync<TMessage>(string topic, TMessage message) where TMessage : class;

    /// <summary>
    /// Sends a message directly to all active local connections associated with the specified client ID.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message being sent.</typeparam>
    /// <exception cref="ArgumentException">Thrown if clientId is null or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown if message is null.</exception>
    /// <remarks>
    /// This only sends to sockets currently connected to *this* server instance.
    /// It does not guarantee delivery across all potential client connections if running multiple server instances
    /// without a shared bus for direct messages.
    /// </remarks>
    Task SendToClientAsync<TMessage>(string clientId, TMessage message) where TMessage : class;
}