using Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Startup.Tests.Infrastructure.Factories;
using Startup.Tests.Infrastructure.TestClients;
using Xunit.Abstractions;

namespace Startup.Tests.Infrastructure.Extensions;

public static class MqttTestExtensions
{
    public static async Task<MqttTestClient> CreateMqttClientAsync(this CustomWebApplicationFactory factory, ITestOutputHelper output)
    {
        var appOptions = factory.Services.GetRequiredService<IOptions<AppOptions>>().Value;
        var client = new MqttTestClient(appOptions.Mqtt, output);
        await client.ConnectAsync();
        return client;
    }
}