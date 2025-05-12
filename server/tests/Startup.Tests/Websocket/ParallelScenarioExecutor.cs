using Startup.Tests.WebSocket;

namespace Startup.Tests.Websocket;

public class ParallelScenarioExecutor
{
    private readonly WebSocketTestScenario[] _scenarios;
    
    public ParallelScenarioExecutor(WebSocketTestScenario[] scenarios)
    {
        _scenarios = scenarios;
    }
    
    public async Task ExecuteAsync()
    {
        var tasks = _scenarios.Select(s => s.ExecuteAsync()).ToArray();
        await Task.WhenAll(tasks);
    }
}