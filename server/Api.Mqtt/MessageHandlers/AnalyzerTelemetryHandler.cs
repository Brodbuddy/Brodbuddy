using Api.Mqtt.Core;
using Application.Services.Sourdough;
using Core.Messaging;
using Core.ValueObjects;
using FluentValidation;
using HiveMQtt.Client.Events;
using HiveMQtt.MQTT5.Types;
using Microsoft.Extensions.Logging;

namespace Api.Mqtt.MessageHandlers;

public class SourdoughReadingValidator : AbstractValidator<SourdoughReading>
{
    public SourdoughReadingValidator()
    {

    }
}

public class AnalyzerTelemetryHandler(ISourdoughTelemetryService service, ILogger<AnalyzerTelemetryHandler> logger) : IMqttMessageHandler<SourdoughReading>
{
    public string TopicFilter => MqttTopics.Patterns.AllAnalyzersTelemetry;
    public QualityOfService QoS => QualityOfService.AtLeastOnceDelivery;

    public async Task HandleAsync(SourdoughReading message, OnMessageReceivedEventArgs args)
    {
        var analyzerId = MqttTopics.ExtractAnalyzerId(args.PublishMessage.Topic);
        
        logger.LogInformation("Processing telemetry from analyzer {AnalyzerId} on topic {Topic}", analyzerId, args.PublishMessage.Topic);
        logger.LogInformation("Data: {Data}", message);
        
        await service.ProcessSourdoughReadingAsync(analyzerId, message);
    }
}