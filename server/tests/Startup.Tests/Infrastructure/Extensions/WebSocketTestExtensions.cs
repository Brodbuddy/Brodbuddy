using Application;
using Application.Services;
using Application.Services.Auth;
using Core.Entities;
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
    
    public static WebSocketTestClient CreateMemberWebSocketClient(
        this CustomWebApplicationFactory factory,
        ITestOutputHelper output,
        string? clientId = null,
        string userId = "test-user", 
        string email = "test@example.com", 
        string role = Role.Member)
    {
        var jwtService = factory.Services.GetRequiredService<IJwtService>();
        var token = jwtService.Generate(userId, email, role);

        var wsClient = factory.CreateWebSocketClient(output, clientId).WithAuth(token);
        
        return wsClient;
    }
    
    public static WebSocketTestClient CreateAdminWebSocketClient(
        this CustomWebApplicationFactory factory,
        ITestOutputHelper output,
        string? clientId = null,
        string userId = "test-user", 
        string email = "test@example.com", 
        string role = Role.Admin)
    {
        var jwtService = factory.Services.GetRequiredService<IJwtService>();
        var token = jwtService.Generate(userId, email, role);

        var wsClient = factory.CreateWebSocketClient(output, clientId).WithAuth(token);
        
        return wsClient;
    }
}