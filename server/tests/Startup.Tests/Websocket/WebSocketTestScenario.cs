using Brodbuddy.WebSocket.Core;
using Shouldly;
using Startup.Tests.Websocket;
using Xunit.Abstractions;

namespace Startup.Tests.WebSocket;

public class WebSocketTestScenario
{
    private readonly WebSocketTestClient _client;
    private readonly string _description;
    private readonly List<Func<Task>> _steps = [];
    private readonly ITestOutputHelper _output;
    
    public WebSocketTestScenario(WebSocketTestClient client, string description)
    {
        _client = client;
        _description = description;
        _output = client.OutputHelper; 
    }
    
    public WebSocketTestScenario Send<TRequest>(string type, TRequest request) where TRequest : class
    {
        _steps.Add(async () =>
        {
            _output.WriteLine($"Scenario '{_description}': Sending {type}");
            await _client.SendMessageAsync(type, request);
        });
        
        return this;
    }
    
    public WebSocketTestScenario SendWithId<TRequest>(string type, TRequest request, string requestId) where TRequest : class
    {
        _steps.Add(async () =>
        {
            _output.WriteLine($"Scenario '{_description}': Sending {type} with RequestId {requestId}");
            await _client.SendMessageAsync(type, request, requestId);
        });
        
        return this;
    } 
    
    public WebSocketTestScenario ExpectResponse<TResponse>(string type, Action<TResponse> assertion, TimeSpan? timeout = null) where TResponse : class
    {
        _steps.Add(async () =>
        {
            _output.WriteLine($"Scenario '{_description}': Waiting for {type}");
            var response = await _client.WaitForMessageAsync<TResponse>(type, timeout);
            _output.WriteLine($"Scenario '{_description}': Received {type}, running assertions");
            assertion(response);
        });
        
        return this;
    }
    
    public WebSocketTestScenario ExpectError(Action<WebSocketError> assertion, TimeSpan? timeout = null)
    {
        _steps.Add(async () =>
        {
            _output.WriteLine($"Scenario '{_description}': Waiting for Error");
            var error = await _client.WaitForMessageAsync<WebSocketError>("Error", timeout);
            _output.WriteLine($"Scenario '{_description}': Received Error, running assertions");
            assertion(error);
        });
        
        return this;
    }
    
    public WebSocketTestScenario SendAndExpect<TRequest, TResponse>(string type, TRequest request, Action<TResponse> assertion, TimeSpan? timeout = null)
        where TRequest : class
        where TResponse : class
    {
        _steps.Add(async () =>
        {
            _output.WriteLine($"Scenario '{_description}': Sending {type} and expecting response");
            var response = await _client.SendAndWaitAsync<TRequest, TResponse>(type, request, timeout);
            _output.WriteLine($"Scenario '{_description}': Received response, running assertions");
            assertion(response);
        });
        
        return this;
    }
    
    public WebSocketTestScenario SendAndExpectError<TRequest>(string type, TRequest request, Action<WebSocketError> assertion, TimeSpan? timeout = null)
        where TRequest : class
    {
        _steps.Add(async () =>
        {
            _output.WriteLine($"Scenario '{_description}': Sending {type} and expecting error");
            var error = await _client.SendAndWaitForErrorAsync<TRequest>(type, request, timeout);
            _output.WriteLine($"Scenario '{_description}': Received error, running assertions");
            assertion(error);
        });
        
        return this;
    }
    
    public WebSocketTestScenario Delay(TimeSpan duration)
    {
        _steps.Add(async () =>
        {
            _output.WriteLine($"Scenario '{_description}': Waiting {duration.TotalMilliseconds}ms");
            await Task.Delay(duration);
        });
        
        return this;
    }
    
    public async Task ExecuteAsync()
    {
        _output.WriteLine($"Executing scenario: {_description}");
        try
        {
            foreach (var step in _steps)
            {
                await step();
            }
            _output.WriteLine($"Scenario completed successfully: {_description}");
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Scenario failed: {_description} - {ex.Message}");
            throw new Exception($"Scenario '{_description}' failed: {ex.Message}", ex);
        }
    }
    
    public static ParallelScenarioExecutor InParallel(params WebSocketTestScenario[] scenarios)
    {
        return new ParallelScenarioExecutor(scenarios);
    }
}