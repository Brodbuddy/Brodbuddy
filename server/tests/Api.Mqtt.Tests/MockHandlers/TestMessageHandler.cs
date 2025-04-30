using Api.Mqtt.Core;
using Api.Mqtt.Tests.TestUtils;
using HiveMQtt.Client.Events;
using HiveMQtt.MQTT5.Types;

namespace Api.Mqtt.Tests.MockHandlers;

public class TestMessageHandler : IMqttMessageHandler<MockMqttPublishMessage>
{
    public string TopicFilter => "test/message";
    public QualityOfService QoS => QualityOfService.AtMostOnceDelivery;

    public Task HandleAsync(MockMqttPublishMessage message, OnMessageReceivedEventArgs args)
    {
        return Task.CompletedTask;
    }
}