using System.Text;
using System.Text.Json;
using HiveMQtt.Client;
using HiveMQtt.MQTT5.Types;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Communication.Mqtt;

public interface IMqttPublisher
{
    Task PublishAsync(string topic, string payload, QualityOfService qos = QualityOfService.AtLeastOnceDelivery, bool retain = false);
    Task PublishAsync<T>(string topic, T payload, QualityOfService qos = QualityOfService.AtLeastOnceDelivery, bool retain = false) where T : class;
}

public class HiveMqttPublisher : IMqttPublisher
{
    private readonly IHiveMQClient _mqttClient;
    private readonly ILogger<HiveMqttPublisher> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new() 
    { 
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
    };
    
    public HiveMqttPublisher(IHiveMQClient mqttClient, ILogger<HiveMqttPublisher> logger)
    {
        _mqttClient = mqttClient;
        _logger = logger;
    }

    public async Task PublishAsync(string topic, string payload, QualityOfService qos = QualityOfService.AtLeastOnceDelivery, bool retain = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentNullException.ThrowIfNull(payload);

        if (!_mqttClient.IsConnected())
        {
            _logger.LogWarning("MQTT client is not connected. Cannot publish message to topic {Topic}", topic);
            throw new InvalidOperationException("MQTT client is not connected");
        }

        try
        {
            var message = new MQTT5PublishMessage
            {
                Topic = topic,
                Payload = Encoding.UTF8.GetBytes(payload),
                QoS = qos,
                Retain = retain
            };
            
            await _mqttClient.PublishAsync(message);
            
            _logger.LogDebug("Successfully published message to topic {Topic} with QoS {QoS}", topic, qos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish MQTT message to topic {Topic}", topic);
            throw new InvalidOperationException($"Failed to publish MQTT message to topic {topic}");
        }
    }

    public async Task PublishAsync<T>(string topic, T payload, QualityOfService qos = QualityOfService.AtLeastOnceDelivery, bool retain = false) where T : class
    {
        ArgumentNullException.ThrowIfNull(payload);

        try
        {
            string jsonPayload = JsonSerializer.Serialize(payload, _jsonOptions);
            await PublishAsync(topic, jsonPayload, qos, retain);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to serialize payload of type {PayloadType} for MQTT topic {Topic}", typeof(T).Name, topic);
            throw new JsonException($"Failed to serialize payload of type {typeof(T).Name} for MQTT topic {topic}");
        }
    }
}