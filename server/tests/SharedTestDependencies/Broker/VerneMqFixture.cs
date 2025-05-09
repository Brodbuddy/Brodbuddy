using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Testcontainers.Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace SharedTestDependencies.Broker;

public class VerneMqFixture : ContainerFixture<ContainerBuilder, IContainer>
{
    private readonly IMessageSink _messageSink;
    private const int MqttPort = 1883;
    private const int WebSocketPort = 8080;
    private const string DefaultUsername = "user";
    private const string DefaultPassword = "pass";
    
    public string MqttConnectionString => $"mqtt://{DefaultUsername}:{DefaultPassword}@{Container.Hostname}:{Container.GetMappedPublicPort(MqttPort)}";
    public string WebSocketConnectionString => $"ws://{DefaultUsername}:{DefaultPassword}@{Container.Hostname}:{Container.GetMappedPublicPort(WebSocketPort)}";
    
    public int MappedMqttPort => Container.GetMappedPublicPort(MqttPort);
    public int MappedWebSocketPort => Container.GetMappedPublicPort(WebSocketPort);
    public string Host => Container.Hostname;
    
    public VerneMqFixture(IMessageSink messageSink) : base(messageSink)
    {
        _messageSink = messageSink;
    }
    
    protected override ContainerBuilder Configure(ContainerBuilder builder)
    {
        return builder
            .WithImage("vernemq/vernemq:latest")
            .WithPortBinding(MqttPort, true)
            .WithPortBinding(WebSocketPort, true)
            .WithEnvironment("DOCKER_VERNEMQ_ACCEPT_EULA", "yes")
            .WithEnvironment("DOCKER_VERNEMQ_ALLOW_ANONYMOUS", "off")
            .WithEnvironment($"DOCKER_VERNEMQ_USER_{DefaultUsername}", DefaultPassword)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(MqttPort));
    }
    
    public Task ResetAsync()
    {
        // Finder lige ud af hvordan det her skal håndteres senere, hm...
        Log("VerneMQ has no built-in reset functionality - topics persist until container restart");
        return Task.CompletedTask;
    }
    
    private void Log(string message)
    {
        _messageSink.OnMessage(new DiagnosticMessage($"[{DateTime.UtcNow:HH:mm:ss.fff} VerneMqFixture] {message}"));
    }
}