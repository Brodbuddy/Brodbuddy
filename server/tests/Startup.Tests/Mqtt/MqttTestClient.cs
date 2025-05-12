using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Application;
using HiveMQtt.Client;
using HiveMQtt.MQTT5.Types;
using Xunit.Abstractions;

namespace Startup.Tests.Mqtt;

public class MqttTestClient : IAsyncDisposable
{
    private readonly HiveMQClient _client;
    private readonly ITestOutputHelper _output;
    private readonly ConcurrentDictionary<string, List<TaskCompletionSource<string>>> _waiters = new();
    private readonly object _lockObj = new();
    
    public MqttTestClient(MqttOptions options, ITestOutputHelper output)
    {
        _output = output;
        
        var clientOptions = new HiveMQClientOptionsBuilder()
            .WithWebSocketServer($"ws://{options.Host}:{options.WebSocketPort}/mqtt")
            .WithClientId($"test-client-{Guid.NewGuid()}")
            .WithUserName(options.Username)
            .WithPassword(options.Password)
            .WithCleanStart(true)
            .WithAutomaticReconnect(false) // Skal ikke auto-connect under tests
            .Build();
            
        _client = new HiveMQClient(clientOptions);
        ConfigureClient();
    }
    
    private void ConfigureClient()
    {
        _client.OnMessageReceived += (_, args) =>
        {
            var topic = args.PublishMessage.Topic;
            var payload = args.PublishMessage.PayloadAsString;

            ArgumentException.ThrowIfNullOrWhiteSpace(topic);
            
            _output.WriteLine($"MqttTestClient received: {topic} -> {payload}");
            
            lock (_lockObj)
            {
                if (!_waiters.TryGetValue(topic, out var waiters)) return;
                foreach (var waiter in waiters.ToList())
                {
                    waiter.TrySetResult(payload);
                    waiters.Remove(waiter);
                }
                
                if (waiters.Count == 0) _waiters.TryRemove(topic, out var _);
            }
        };
    }
    
    public async Task ConnectAsync()
    {
        await _client.ConnectAsync();
        // Giv din et øjeblik at etablere forbindelse
        await Task.Delay(100);
    }
    
    public async Task PublishAsync<T>(string topic, T payload, QualityOfService qos = QualityOfService.AtLeastOnceDelivery) 
        where T : class
    {
        var json = JsonSerializer.Serialize(payload);
        await PublishAsync(topic, json, qos);
    }
    
    public async Task PublishAsync(string topic, string payload, QualityOfService qos = QualityOfService.AtLeastOnceDelivery)
    {
        _output.WriteLine($"MqttTestClient publishing: {topic} -> {payload}");
        
        var message = new MQTT5PublishMessage
        {
            Topic = topic,
            Payload = Encoding.UTF8.GetBytes(payload),
            QoS = qos
        };
        
        await _client.PublishAsync(message);
    }
    
    public async Task SubscribeAsync(string topic, QualityOfService qos = QualityOfService.AtLeastOnceDelivery)
    {
        _output.WriteLine($"MqttTestClient subscribing to: {topic}");
        await _client.SubscribeAsync(topic, qos);
    }
    
    public async Task<T> WaitForMessageAsync<T>(string topic, TimeSpan? timeout = null) where T : class
    {
        var payload = await WaitForMessageAsync(topic, timeout);
        return JsonSerializer.Deserialize<T>(payload) ?? throw new InvalidOperationException($"Failed to deserialize message from topic {topic}");
    }
    
    public async Task<string> WaitForMessageAsync(string topic, TimeSpan? timeout = null)
    {
        var tcs = new TaskCompletionSource<string>();
        
        lock (_lockObj)
        {
            if (!_waiters.ContainsKey(topic)) _waiters[topic] = [];
            
            _waiters[topic].Add(tcs);
        }
        
        using var cts = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(5));
        
        try
        {
            return await tcs.Task.WaitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException($"Timeout waiting for MQTT message on topic: {topic}");
        }
        finally
        {
            lock (_lockObj)
            {
                if (_waiters.TryGetValue(topic, out var waiters))
                {
                    waiters.Remove(tcs);
                }
            }
        }
    }
    
    public async ValueTask DisposeAsync()
    {
        if (_client.IsConnected())
        {
            await _client.DisconnectAsync();
        }
        _client.Dispose();
        GC.SuppressFinalize(this);
    }
}