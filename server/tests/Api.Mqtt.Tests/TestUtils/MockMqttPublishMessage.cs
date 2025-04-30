using Moq;
using System.Text;
using HiveMQtt.Client.Events;
using HiveMQtt.MQTT5.Types;

namespace Api.Mqtt.Tests.TestUtils;

public class MockMqttPublishMessage 
{
    public string Topic { get; set; } = string.Empty;
    public byte[] Payload { get; set; } = Array.Empty<byte>();
    public string PayloadAsString => Encoding.UTF8.GetString(Payload);
}