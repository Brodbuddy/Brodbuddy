using System.Net.WebSockets;
using System.Text.Json;
using Startup.Tests.WebSocket;
using Websocket.Client;
using Xunit.Abstractions;
using WebSocketError = Brodbuddy.WebSocket.Core.WebSocketError;

namespace Startup.Tests.Websocket;

public class WebSocketTestClient : IAsyncDisposable
{
    private readonly WebsocketClient _client;
    private readonly Dictionary<string, List<TaskCompletionSource<JsonElement>>> _messageWaiters = new();
    private readonly Dictionary<string, List<TaskCompletionSource<JsonElement>>> _requestWaiters = new();
    private readonly object _lockObj = new();
    private readonly ITestOutputHelper _output;
    private bool _isDisposed;
    private bool _isConnected;
    private string? _authToken;

    public ITestOutputHelper OutputHelper { get; }
    public string ClientId { get; }
    
    public WebSocketTestClient(Uri serverUri, ITestOutputHelper output, string? clientId = null)
    {
        ClientId = clientId ?? Guid.NewGuid().ToString();
        _output = output;
        OutputHelper = _output;
        var uriWithClientId = new Uri($"{serverUri}?id={ClientId}");
        _output.WriteLine($"Creating client for {uriWithClientId}");
        
        _client = new WebsocketClient(uriWithClientId);
        _client.ReconnectTimeout = null;
        ConfigureClient();
    }

    private void ConfigureClient()
    {
        _client.MessageReceived.Subscribe(msg =>
        {
            _output.WriteLine($"Client {ClientId} received: {msg.Text}");
            if (string.IsNullOrEmpty(msg.Text)) return;

            try
            {
                var jsonDoc = JsonDocument.Parse(msg.Text);
                var root = jsonDoc.RootElement;

                var type = GetProperty(root, "Type")?.GetString();
                var payload = GetProperty(root, "Payload");
                var requestId = GetProperty(root, "RequestId")?.GetString();

                _output.WriteLine($"Parsed - Type: {type}, RequestId: {requestId}");

                if (type == null) return;
                lock (_lockObj)
                {
                    // Håndtere request-response (med RequestID)
                    if (!string.IsNullOrEmpty(requestId) &&
                        _requestWaiters.TryGetValue(requestId, out var requestWaiters))
                    {
                        _output.WriteLine($"Found {requestWaiters.Count} request waiters for RequestId: {requestId}");
                        foreach (var waiter in requestWaiters.ToList())
                        {
                            waiter.TrySetResult(payload ?? new JsonElement());
                            requestWaiters.Remove(waiter);
                        }

                        if (requestWaiters.Count == 0)
                            _requestWaiters.Remove(requestId);
                    }

                    // Håndtere broadcasts/notifikationer (ud fra message type)
                    if (_messageWaiters.TryGetValue(type, out var messageWaiters))
                    {
                        _output.WriteLine($"Found {messageWaiters.Count} message waiters for type: {type}");
                        foreach (var waiter in messageWaiters.ToList())
                        {
                            waiter.TrySetResult(payload ?? new JsonElement());
                            messageWaiters.Remove(waiter);
                        }

                        if (messageWaiters.Count == 0) _messageWaiters.Remove(type);
                    }
                }
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Error parsing message: {ex.Message}");
            }
        });
        
        _client.DisconnectionHappened.Subscribe(info => _output.WriteLine($"Client {ClientId} disconnected: {info.Type}"));
        _client.ReconnectionHappened.Subscribe(info => _output.WriteLine($"Client {ClientId} reconnected: {info.Type}"));
    }

    private async Task EnsureConnectedAsync()
    {
        if (_isConnected) return;
        
        await _client.Start();
        _isConnected = true;
        
        // Lidt tid for at etablere forbindelse
        await Task.Delay(100);
    }
    
    public WebSocketTestClient WithAuth(string token)
    {
        _authToken = token;
        return this;
    }
    
    public WebSocketTestScenario CreateScenario(string description) => new(this, description);
    
    public async Task<TResponse> SendAndWaitAsync<TRequest, TResponse>(string type, TRequest request, TimeSpan? timeout = null)
        where TRequest : class
        where TResponse : class
    {
        var requestId = Guid.NewGuid().ToString();
        var waitTask = WaitForRequestResponseAsync<TResponse>(requestId, timeout);
        
        await SendMessageAsync(type, request, requestId);
        return await waitTask;
    }

    public async Task<WebSocketError> SendAndWaitForErrorAsync<TRequest>(string type, TRequest request, TimeSpan? timeout = null)
        where TRequest : class
    {
        var requestId = Guid.NewGuid().ToString();
        var waitTask = WaitForRequestResponseAsync<WebSocketError>(requestId, timeout);
        
        await SendMessageAsync(type, request, requestId);
        return await waitTask;
    }
    
    public async Task SendMessageAsync<TRequest>(string type, TRequest request, string? requestId = null)
        where TRequest : class
    {
        await EnsureConnectedAsync();
        
        var message = new Dictionary<string, object>
        {
            ["Type"] = type,
            ["Payload"] = request
        };
        
        if (!string.IsNullOrEmpty(requestId)) message["RequestId"] = requestId;
        if (!string.IsNullOrEmpty(_authToken)) message["Token"] = _authToken;
        
        var json = JsonSerializer.Serialize(message);
        _output.WriteLine($"Client {ClientId} sending: {json}");
        await _client.SendInstant(json);
    }
    
    private async Task<TResponse> WaitForRequestResponseAsync<TResponse>(string requestId, TimeSpan? timeout = null)
        where TResponse : class
    {
        await EnsureConnectedAsync();
        var tcs = new TaskCompletionSource<JsonElement>();
        
        lock (_lockObj)
        {
            if (!_requestWaiters.ContainsKey(requestId)) _requestWaiters[requestId] = [];
            
            _requestWaiters[requestId].Add(tcs);
        }
        
        using var cts = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(5));
        
        try
        {
            var result = await tcs.Task.WaitAsync(cts.Token);
            return DeserializeResponse<TResponse>(result);
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException($"Timeout waiting for response to request {requestId}");
        }
        finally
        {
            lock (_lockObj)
            {
                if (_requestWaiters.TryGetValue(requestId, out var waiters))
                {
                    waiters.Remove(tcs);
                    if (waiters.Count == 0) _requestWaiters.Remove(requestId);
                }
            }
        }
    }
    
    public async Task<TResponse> WaitForMessageAsync<TResponse>(string type, TimeSpan? timeout = null)
        where TResponse : class
    {
        await EnsureConnectedAsync();
        var tcs = new TaskCompletionSource<JsonElement>();
        
        lock (_lockObj)
        {
            if (!_messageWaiters.ContainsKey(type)) _messageWaiters[type] = [];
            
            _messageWaiters[type].Add(tcs);
        }
        
        using var cts = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(5));
        
        try
        {
            var result = await tcs.Task.WaitAsync(cts.Token);
            return DeserializeResponse<TResponse>(result);
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException($"Timeout waiting for message of type {type}");
        }
        finally
        {
            lock (_lockObj)
            {
                if (_messageWaiters.TryGetValue(type, out var waiters))
                {
                    waiters.Remove(tcs);
                    if (waiters.Count == 0) _messageWaiters.Remove(type);
                }
            }
        }
    }
    
    private static TResponse DeserializeResponse<TResponse>(JsonElement element) where TResponse : class
    {
        // Håndter tom respons
        if (element.ValueKind == JsonValueKind.Undefined) throw new InvalidOperationException("Received empty response");
            
        var response = element.Deserialize<TResponse>();
        return response ?? throw new InvalidOperationException($"Failed to deserialize message to {typeof(TResponse).Name}");
    }
    
    private static JsonElement? GetProperty(JsonElement element, string propertyName)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (property.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase)) return property.Value;
        }
        return null;
    }
    
    public async ValueTask DisposeAsync()
    {
        if (_isDisposed) return;
        
        if (_isConnected)
        {
            await _client.Stop(WebSocketCloseStatus.NormalClosure, "Test completed");
        }
        
        _client.Dispose();
        _isDisposed = true;
        GC.SuppressFinalize(this);
    }
}