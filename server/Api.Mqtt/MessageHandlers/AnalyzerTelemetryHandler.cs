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
        RuleFor(x => x.TemperatureCelsius)
            .InclusiveBetween(-10, 80)
            .WithMessage("Temperature must be between -10°C and 80°C");

        RuleFor(x => x.HumidityPercent)
            .InclusiveBetween(0, 100)
            .WithMessage("Humidity must be between 0% and 100%");

        RuleFor(x => x.RisePercent)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Rise must be non-negative");

        RuleFor(x => x.Timestamp)
            .Must(BeRecentTimestamp)
            .WithMessage("Timestamp must be within last 24 hours");
    }

    private static bool BeRecentTimestamp(DateTime timestamp)
    {
        var now = DateTime.UtcNow;
        return timestamp >= now.AddHours(-24) && timestamp <= now.AddMinutes(5);
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
        
        await service.ProcessSourdoughReadingAsync(analyzerId, message);
    }
}