using Api.Mqtt.Core;
using Application.Services.Sourdough;
using Core.ValueObjects;
using FluentValidation;
using HiveMQtt.Client.Events;
using HiveMQtt.MQTT5.Types;
using Microsoft.Extensions.Logging;

namespace Api.Mqtt.MessageHandlers;

public class DiagnosticsResponseValidator : AbstractValidator<DiagnosticsResponse>
{
    public DiagnosticsResponseValidator()
    {
        
    }
}

public class DiagnosticsResponseHandler(ISourdoughTelemetryService service, ILogger<DiagnosticsResponseHandler> logger) : IMqttMessageHandler<DiagnosticsResponse>
{
    public string TopicFilter => "analyzer/+/diagnostics/response";
    public QualityOfService QoS => QualityOfService.AtLeastOnceDelivery;

    public async Task HandleAsync(DiagnosticsResponse message, OnMessageReceivedEventArgs args)
    {
        var topicParts = args.PublishMessage.Topic!.Split('/');
        var analyzerId = Guid.Parse(topicParts[1]);
        
        logger.LogInformation("Processing diagnostics from analyzer {AnalyzerId} on topic {Topic}", analyzerId, args.PublishMessage.Topic);
        logger.LogInformation("Data: {Data}", message);
        
        await service.ProcessDiagnosticsResponseAsync(analyzerId, message);
    }
}