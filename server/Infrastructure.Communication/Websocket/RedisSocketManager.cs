using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Brodbuddy.WebSocket.State;
using Fleck;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Infrastructure.Communication.Websocket;

public class RedisSocketManager : ISocketManager
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _db;
    private readonly ConcurrentDictionary<Guid, IWebSocketConnection> _localSockets;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<RedisSocketManager> _logger;

    public RedisSocketManager(IConnectionMultiplexer redis, TimeProvider timeProvider,
        ILogger<RedisSocketManager> logger)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _db = _redis.GetDatabase();
        _localSockets = new ConcurrentDictionary<Guid, IWebSocketConnection>();
    }

    public async Task OnOpenAsync(IWebSocketConnection socket, string clientId)
    {
        ArgumentNullException.ThrowIfNull(socket);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);

        var socketId = socket.ConnectionInfo.Id;
        _localSockets[socketId] = socket;

        var socketHashKey = RedisSocketKeys.SocketHash(socketId);
        await _db.HashSetAsync(socketHashKey, RedisSocketKeys.ClientIdField, clientId);
        await _db.HashSetAsync(socketHashKey, RedisSocketKeys.ConnectedAtField,
            _timeProvider.GetUtcNow().ToUnixTimeSeconds(), When.NotExists);

        await _db.StringSetAsync(RedisSocketKeys.SocketToClientMap(socketId), clientId);
        await _db.SetAddAsync(RedisSocketKeys.ActiveSocketsSetKey, socketId.ToString());
        await _db.SetAddAsync(RedisSocketKeys.ClientSocketsSet(clientId), socketId.ToString());

        _logger.LogInformation("Socket {SocketId} opened for client {ClientId}", socketId, clientId);
    }

    public async Task OnCloseAsync(IWebSocketConnection socket, string clientId)
    {
        ArgumentNullException.ThrowIfNull(socket);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);

        var socketId = socket.ConnectionInfo.Id;
        var socketToClientKey = RedisSocketKeys.SocketToClientMap(socketId);

        string? actualStoredClientId = await _db.StringGetAsync(socketToClientKey);

        if (!string.IsNullOrWhiteSpace(actualStoredClientId) && actualStoredClientId != clientId)
        {
            _logger.LogWarning(
                "Client ID mismatch during close for Socket {SocketId}: received {ProvidedClientId} but stored {StoredClientId}",
                socketId, clientId, actualStoredClientId);
        }
        else if (string.IsNullOrWhiteSpace(actualStoredClientId))
        {
            _logger.LogWarning("Socket {SocketId} closed but no associated client ID found in Redis (key: {Key})",
                socketId, socketToClientKey);
        }

        _localSockets.TryRemove(socketId, out _);

        await _db.KeyDeleteAsync(RedisSocketKeys.SocketHash(socketId));
        await _db.KeyDeleteAsync(socketToClientKey);
        await _db.SetRemoveAsync(RedisSocketKeys.ActiveSocketsSetKey, socketId.ToString());

        if (!string.IsNullOrWhiteSpace(actualStoredClientId))
        {
            await HandleClientSetCleanupAsync(actualStoredClientId, socketId);
        }

        _logger.LogInformation("Socket {SocketId} closed (initiated with provided client ID {ProvidedClientId})",
            socketId, clientId);
    }

    public bool TryGetLocalSocket(Guid socketId, [MaybeNullWhen(false)] out IWebSocketConnection socket)
    {
        return _localSockets.TryGetValue(socketId, out socket);
    }

    public async Task<string> GetClientIdAsync(IWebSocketConnection socket)
    {
        ArgumentNullException.ThrowIfNull(socket);

        var socketId = socket.ConnectionInfo.Id;
        var socketToClientKey = RedisSocketKeys.SocketToClientMap(socketId);
        RedisValue storedClientIdValue = await _db.StringGetAsync(socketToClientKey);

        if (storedClientIdValue.IsNullOrEmpty)
            throw new KeyNotFoundException($"No client ID found for socket {socketId}.");

        return storedClientIdValue!;
    }

    public async Task<(bool Found, string? ClientId)> TryGetClientIdAsync(IWebSocketConnection socket)
    {
        ArgumentNullException.ThrowIfNull(socket);

        var socketId = socket.ConnectionInfo.Id;
        var socketToClientKey = RedisSocketKeys.SocketToClientMap(socketId);

        RedisValue storedClientIdValue = await _db.StringGetAsync(socketToClientKey);

        if (storedClientIdValue.IsNullOrEmpty)
        {
            return (Found: false, ClientId: null);
        }

        return (Found: true, ClientId: storedClientIdValue);
    }

    public async Task SubscribeAsync(string clientId, string topic)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);

        var clientTopicsKey = RedisSocketKeys.ClientTopicsSet(clientId);
        var topicSubscribersKey = RedisSocketKeys.TopicSubscribersSet(topic);

        var transaction = _db.CreateTransaction();

        // Sæt kommandoer i kø uden at 'await' Task-objekterne.
        // Vi ignorerer dem med '_' da vi kun har brug for transaktionens samlede resultat.
        _ = transaction.SetAddAsync(RedisSocketKeys.AllTopicsSetKey, topic);
        _ = transaction.SetAddAsync(clientTopicsKey, topic);
        _ = transaction.SetAddAsync(topicSubscribersKey, clientId);

        bool committed = await transaction.ExecuteAsync();
        if (!committed)
        {
            _logger.LogError("Failed to execute subscription transaction for Client {ClientId} to Topic {Topic}",
                clientId, topic);
        }
        else
        {
            _logger.LogInformation("Client {ClientId} subscribed to Topic {Topic}", clientId, topic);
        }
    }

    public async Task UnsubscribeAsync(string clientId, string topic)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);

        var clientTopicsKey = RedisSocketKeys.ClientTopicsSet(clientId);
        var topicSubscribersKey = RedisSocketKeys.TopicSubscribersSet(topic);

        var transaction = _db.CreateTransaction();

        // Sæt kommandoer i kø uden 'await' ligesom i 'SubscribeAsync'-metoden.
        _ = transaction.SetRemoveAsync(clientTopicsKey, topic);
        _ = transaction.SetRemoveAsync(topicSubscribersKey, clientId);

        bool committed = await transaction.ExecuteAsync();
        if (!committed)
        {
            _logger.LogError("Failed to execute unsubscription transaction for Client {ClientId} from Topic {Topic}",
                clientId, topic);
        }
        else
        {
            _logger.LogInformation("Client {ClientId} unsubscribed from Topic {Topic}", clientId, topic);
        }
    }

    public async Task<IReadOnlyList<string>> GetTopicsAsync(string clientId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);

        var clientTopicsKey = RedisSocketKeys.ClientTopicsSet(clientId);
        var topics = await _db.SetMembersAsync(clientTopicsKey);

        return topics.Select(topic => topic.ToString()).ToList();
    }

    public async Task<IReadOnlyList<string>> GetSubscribersAsync(string topic)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);

        var topicSubscribersKey = RedisSocketKeys.TopicSubscribersSet(topic);
        var subscribers = await _db.SetMembersAsync(topicSubscribersKey);

        return subscribers.Select(subscriber => subscriber.ToString()).ToList();
    }

    public async Task BroadcastAsync<TMessage>(string topic, TMessage message) where TMessage : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentNullException.ThrowIfNull(message);

        var subscribers = await GetSubscribersAsync(topic);
        if (subscribers.Count == 0)
        {
            _logger.LogDebug("No subscribers found in Redis state for topic {Topic}. Skipping publish.", topic);
            return;
        }

        _logger.LogInformation(
            "Publishing message of type {MessageType} to {SubscriberCount} potential clients via Redis channel for topic {Topic}",
            typeof(TMessage).Name, subscribers.Count, topic);

        string serializedMessage;
        try
        {
            serializedMessage = JsonSerializer.Serialize(new MessageWrapper<TMessage>(message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to serialize message of type {MessageType} for Redis publish to topic {Topic}",
                typeof(TMessage).Name, topic);
            return;
        }

        try
        {
            var subscriber = _redis.GetSubscriber();
            var channel = RedisChannel.Pattern(RedisSocketKeys.TopicChannel(topic));
            long receivers = await subscriber.PublishAsync(channel, serializedMessage);

            if (receivers == 0)
            {
                _logger.LogDebug(
                    "Published message to topic {Topic} channel {Channel}, but no Redis subscribers received it.",
                    topic, channel);
            }
            else
            {
                _logger.LogDebug(
                    "Published message to topic {Topic} channel {Channel}, received by {ReceiverCount} Redis subscribers.",
                    topic, channel, receivers);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message to Redis channel for topic {Topic}", topic);
        }
    }

    public async Task SendToClientAsync<TMessage>(string clientId, TMessage message) where TMessage : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        ArgumentNullException.ThrowIfNull(message);

        var clientSocketsKey = RedisSocketKeys.ClientSocketsSet(clientId);
        var socketIdStrings = await _db.SetMembersAsync(clientSocketsKey);
        if (socketIdStrings.Length == 0)
        {
            _logger.LogWarning(
                "Attempted to send message to client {ClientId}, but no sockets found in Redis set {Key}", clientId,
                clientSocketsKey);
            return;
        }

        _logger.LogDebug(
            "Attempting to send direct message of type {MessageType} to client {ClientId} ({SocketCount} potential sockets)",
            typeof(TMessage).Name, clientId, socketIdStrings.Length);
        
        string serializedMessage;
        try
        {
            serializedMessage = JsonSerializer.Serialize(new MessageWrapper<TMessage>(message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to serialize direct message of type {MessageType} for client {ClientId}", typeof(TMessage).Name, clientId);
            return;
        }

        var sendTasks = new List<Task>();
        int actualSends = 0;
        foreach (var socketIdStr in socketIdStrings)
        {
            if (!Guid.TryParse(socketIdStr.ToString(), out var socketId))
            {
                _logger.LogWarning("Invalid Guid format '{SocketIdString}' found in socket set for client {ClientId}", socketIdStr, clientId);
                continue;
            }
            
            if (!TryGetLocalSocket(socketId, out var socket)) continue; 
            if (socket.IsAvailable)
            {
                try
                {
                    sendTasks.Add(socket.Send(serializedMessage));
                    actualSends++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error initiating direct send to socket {SocketId} for client {ClientId}", socketId, clientId);
                }
            }
            else
            {
                _logger.LogDebug("Local socket {SocketId} for client {ClientId} found but is not available.", socketId, clientId);
            }
        }

        if (actualSends > 0)
        {
            _logger.LogDebug("Initiated direct message send to {ActualSends} local sockets for client {ClientId}", actualSends, clientId);
        }
        else
        {
            _logger.LogInformation("No locally managed, available sockets found for client {ClientId} to send direct message.", clientId);
        }

        try
        {
            await Task.WhenAll(sendTasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "One or more errors occurred during direct message send operations for client {ClientId}.", clientId);
        }
    }

    private async Task HandleClientSetCleanupAsync(string actualStoredClientId, Guid socketId)
    {
        var actualClientSocketsSetKey = RedisSocketKeys.ClientSocketsSet(actualStoredClientId);
        bool removed = await _db.SetRemoveAsync(actualClientSocketsSetKey, socketId.ToString());

        if (removed)
        {
            _logger.LogDebug("Removed Socket {SocketId} from actual client set {ClientSetKey}", socketId,
                actualClientSocketsSetKey);

            long remainingSockets = await _db.SetLengthAsync(actualClientSocketsSetKey);
            if (remainingSockets == 0)
            {
                _logger.LogInformation(
                    "Actual client {ClientId} has no more active connections after closing Socket {SocketId}",
                    actualStoredClientId, socketId);
            }
        }
        else
        {
            _logger.LogWarning(
                "Attempted to remove Socket {SocketId} from actual client set {ClientSetKey}, but it was not found.",
                socketId, actualClientSocketsSetKey);
        }
    }
}