using Api.Mqtt.Core;
using Application.Services;
using Core.Messaging;
using Core.ValueObjects;
using HiveMQtt.Client.Events;
using HiveMQtt.MQTT5.Types;
using Microsoft.Extensions.Logging;

namespace Api.Mqtt.MessageHandlers;

public class OtaStatusHandler(IOtaService otaService, ILogger<OtaStatusHandler> logger) : IMqttMessageHandler<OtaStatusMessage>
{
    public string TopicFilter => MqttTopics.Patterns.AllAnalyzersOtaStatus;
    public QualityOfService QoS => QualityOfService.AtLeastOnceDelivery;

    public async Task HandleAsync(OtaStatusMessage message, OnMessageReceivedEventArgs args)
    {
        var analyzerId = MqttTopics.ExtractAnalyzerId(args.PublishMessage.Topic, "analyzer/{analyzerId}/ota/status");
        
        logger.LogInformation("OTA status from analyzer {AnalyzerId}: Status={Status}, Progress={Progress}%", 
            analyzerId, message.Status, message.Progress);
        
        await otaService.ProcessOtaStatusAsync(analyzerId, message);
    }
}