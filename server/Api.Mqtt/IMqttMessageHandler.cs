using HiveMQtt.Client.Events;
using HiveMQtt.MQTT5.Types;

namespace Api.Mqtt;

public interface IMqttMessageHandler
{
    string TopicFilter { get; }
    QualityOfService QoS { get; }
    Task HandleAsync(OnMessageReceivedEventArgs args);
}

public interface IMqttMessageHandler<in TMessage> : IMqttMessageHandler where TMessage : class
{
    Task HandleAsync(TMessage message, string topic);
}