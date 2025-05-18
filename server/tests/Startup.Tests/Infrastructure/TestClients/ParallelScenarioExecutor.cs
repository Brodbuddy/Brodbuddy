namespace Startup.Tests.Infrastructure.TestClients;

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