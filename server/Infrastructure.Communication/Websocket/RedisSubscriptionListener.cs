using System.Diagnostics.CodeAnalysis;
using Brodbuddy.WebSocket.State;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Infrastructure.Communication.Websocket;

public class RedisSubscriptionListener : IHostedService, IAsyncDisposable
{
    private readonly ILogger<RedisSubscriptionListener> _logger;
    private readonly IConnectionMultiplexer _redis;
    private readonly ISocketManager _socketManager;
    private readonly IDatabase _db;
    private ISubscriber? _subscriber;
    private readonly CancellationTokenSource _stoppingCts = new();
    private bool _disposed;

    public RedisSubscriptionListener(ILogger<RedisSubscriptionListener> logger, IConnectionMultiplexer redis,
        ISocketManager socketManager)
    {
        _logger = logger;
        _redis = redis;
        _socketManager = socketManager;
        _db = _redis.GetDatabase();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Redis Subscription Listener...");
        _subscriber = _redis.GetSubscriber();

        try
        {
            var channelPattern = RedisChannel.Pattern(RedisSocketKeys.AllTopicChannelsPattern());
            await _subscriber.SubscribeAsync(channelPattern, HandleMessageAsync);

            _logger.LogInformation("Subscribed to Redis channel pattern: {ChannelPattern}", channelPattern);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Failed to subscribe to Redis channels. Broadcasting may not function correctly.");
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Redis Subscription Listener...");
        if (_subscriber != null)
        {
            await _subscriber.UnsubscribeAllAsync();
            _logger.LogInformation("Unsubscribed from all Redis channels.");
        }

        await _stoppingCts.CancelAsync();
    }
    
    // Bruges som wrapper og er `async void` for at kunne bruges i SubscribeAsync
    private async void HandleMessageAsync(RedisChannel channel, RedisValue value)
    {
        await ProcessMessageAsync(channel, value);
    }

    // Kerne logik; er public så vi kan teste den
    public async Task ProcessMessageAsync(RedisChannel channel, RedisValue value)
    {
        try
        {
            string channelStr = channel.ToString();
            string messageJson = value.ToString();
            _logger.LogDebug("Received message on Redis channel {Channel}", channelStr);

            string? topic;
            if (!TryExtractTopic(channelStr, out topic))
            {
                _logger.LogWarning("Received message on unexpected channel format: {Channel}", channel);
                return;
            }

            var subscribers = await _socketManager.GetSubscribersAsync(topic);
            if (subscribers.Count == 0)
            {
                _logger.LogTrace("No subscribers found in state for topic {Topic} from channel {Channel}", topic,
                    channelStr);
                return;
            }

            var sendTasks = new List<Task>();
            int localSends = 0;
            foreach (var clientId in subscribers)
            {
                var clientSocketsKey = RedisSocketKeys.ClientSocketsSet(clientId);
                var socketIdStrings = await _db.SetMembersAsync(clientSocketsKey);

                foreach (var socketIdStr in socketIdStrings)
                {
                    if (!Guid.TryParse(socketIdStr.ToString(), out var socketId) ||
                        !_socketManager.TryGetLocalSocket(socketId, out var socket)) continue;

                    if (!socket.IsAvailable) continue;

                    try
                    {
                        sendTasks.Add(socket.Send(messageJson));
                        localSends++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Error initiating send to local socket {SocketId} for client {ClientId} from pub/sub channel {Channel}",
                            socketId, clientId, channelStr);
                    }
                }
            }

            if (localSends <= 0) return;

            _logger.LogDebug(
                "Forwarded message from channel {Channel} to {LocalSendCount} local sockets for topic {Topic}",
                channelStr, localSends, topic);
            await Task.WhenAll(sendTasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message received on Redis channel {Channel}", channel.ToString());
        }
    }

    private static bool TryExtractTopic(string channel, [NotNullWhen(true)] out string? topic)
    {
        var expectedPrefix = RedisSocketKeys.TopicChannel(string.Empty);
        if (channel.StartsWith(expectedPrefix, StringComparison.Ordinal))
        {
            topic = channel[expectedPrefix.Length..];
            if (!string.IsNullOrEmpty(topic)) return true;
        }

        topic = null;
        return false;
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        GC.SuppressFinalize(this);
    }
    
    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (_disposed)
        {
            return;
        }

        if (_subscriber != null)
        {
            await _subscriber.UnsubscribeAllAsync();
        }
        
        await _stoppingCts.CancelAsync();
        _stoppingCts.Dispose();

        _disposed = true;
    }
}