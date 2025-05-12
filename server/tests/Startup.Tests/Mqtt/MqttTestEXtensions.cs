using Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Startup.Tests.WebApplicationFactories;
using Xunit.Abstractions;

namespace Startup.Tests.Mqtt;

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