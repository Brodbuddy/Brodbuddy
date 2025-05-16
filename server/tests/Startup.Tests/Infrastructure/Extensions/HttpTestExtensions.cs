using System.Net.Http.Headers;
using Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Startup.Tests.Infrastructure.Factories;

namespace Startup.Tests.Infrastructure.Extensions;

public static class HttpTestExtensions
{
    public static HttpClient CreateAuthenticatedHttpClient(
        this CustomWebApplicationFactory factory,
        string userId = "test-user", 
        string email = "test@example.com", 
        string role = "user")
    {
        var jwtService = factory.Services.GetRequiredService<IJwtService>();
        var token = jwtService.Generate(userId, email, role);
        
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        
        return client;
    }
}