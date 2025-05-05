using HiveMQtt.Client.Events;
using HiveMQtt.MQTT5.Types;

namespace Api.Mqtt.Core;

public interface IMqttMessageHandler
{
    string TopicFilter { get; }
    QualityOfService QoS { get; }
}

public interface IMqttMessageHandler<in TMessage> : IMqttMessageHandler where TMessage : class
{
    Task HandleAsync(TMessage message, OnMessageReceivedEventArgs args);
}