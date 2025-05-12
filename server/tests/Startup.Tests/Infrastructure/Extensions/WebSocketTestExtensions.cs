using Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Startup.Tests.Infrastructure.Factories;
using Startup.Tests.Infrastructure.TestClients;
using Xunit.Abstractions;

namespace Startup.Tests.Infrastructure.Extensions;

public static class WebSocketTestExtensions
{
    public static WebSocketTestClient CreateWebSocketClient(this CustomWebApplicationFactory factory, ITestOutputHelper output, string? clientId = null)
    {
        var appOptions = factory.Services.GetRequiredService<IOptions<AppOptions>>().Value;
        
        var wsUri = new Uri($"ws://localhost:{appOptions.PublicPort}");
        
        return new WebSocketTestClient(wsUri, output, clientId);
    }
}