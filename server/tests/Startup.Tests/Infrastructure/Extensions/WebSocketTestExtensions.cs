using Application;
using Application.Services;
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
    
    public static WebSocketTestClient CreateAuthenticatedWebSocketClient(
        this CustomWebApplicationFactory factory,
        ITestOutputHelper output,
        string? clientId = null,
        string userId = "test-user", 
        string email = "test@example.com", 
        string role = "user")
    {
        var jwtService = factory.Services.GetRequiredService<IJwtService>();
        var token = jwtService.Generate(userId, email, role);
        
        var wsClient = factory.CreateWebSocketClient(output, clientId);
        wsClient.WithAuth(token);
        
        return wsClient;
    }
}