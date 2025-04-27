using System.Reflection;
using System.Text.Json;
using FluentValidation;
using HiveMQtt.Client.Events;
using HiveMQtt.MQTT5.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Api.Mqtt;

public class MqttDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MqttDispatcher> _logger;
    private readonly Dictionary<string, (Type HandlerType, Type? MessageType, IValidator? Validator)> _handlerRegistry = new();

    private readonly JsonSerializerOptions _jsonOptions = new() 
    { 
        PropertyNameCaseInsensitive = true 
    };
    
    public MqttDispatcher(IServiceProvider serviceProvider, ILogger<MqttDispatcher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public void RegisterHandlers(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var handlerTypes = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && t.GetInterfaces().Any(i => i == typeof(IMqttMessageHandler) || (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IMqttMessageHandler<>))))
            .ToList();

        using var scope = _serviceProvider.CreateScope();

        foreach (var handlerType in handlerTypes)
        {
            try
            {
                var handler = (IMqttMessageHandler) ActivatorUtilities.CreateInstance(scope.ServiceProvider, handlerType);
                var topicFilter = handler.TopicFilter;

                var genericInterface = handlerType.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IMqttMessageHandler<>));

                if (genericInterface != null)
                {
                    var messageType = genericInterface.GetGenericArguments()[0];


                    var validatorType = typeof(IValidator<>).MakeGenericType(messageType);
                    var validator = scope.ServiceProvider.GetService(validatorType) as IValidator;

                    _handlerRegistry[topicFilter] = (handlerType, messageType, validator);

                    if (validator != null)
                    {
                        _logger.LogDebug(
                            "Registered handler {HandlerType} for topic filter {TopicFilter} with message type {MessageType} and validator",
                            handlerType.Name, topicFilter, messageType.Name);
                    }
                    else
                    {
                        _logger.LogDebug(
                            "Registered handler {HandlerType} for topic filter {TopicFilter} with message type {MessageType} (no validator)",
                            handlerType.Name, topicFilter, messageType.Name);
                    }
                }
                else
                {
                    _handlerRegistry[topicFilter] = (handlerType, null, null);
                    _logger.LogDebug("Registered handler {HandlerType} for topic filter {TopicFilter} (non-generic)",
                        handlerType.Name, topicFilter);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register handler {HandlerType}", handlerType.Name);
            }
        }

        _logger.LogInformation("Registered {Count} MQTT message handlers", _handlerRegistry.Count);
    }
    
    public IEnumerable<(string TopicFilter, QualityOfService QoS)> GetSubscriptions()
    {
        using var scope = _serviceProvider.CreateScope();
        var result = new List<(string, QualityOfService)>();

        foreach (var (topicFilter, handlerInfo) in _handlerRegistry)
        {
            try
            {
                var handler = (IMqttMessageHandler)ActivatorUtilities.CreateInstance(scope.ServiceProvider, handlerInfo.HandlerType);
                result.Add((topicFilter, handler.QoS));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get QoS for handler {HandlerType}", handlerInfo.HandlerType.Name);
            }
        }

        return result;
    }
    
    public async Task DispatchAsync(OnMessageReceivedEventArgs args)
    {
        var incomingTopic = args.PublishMessage.Topic;
        _logger.LogDebug("Received MQTT message on topic: {Topic}", incomingTopic);
        
        var matchingHandlers = _handlerRegistry.Where(kvp => MqttTopicMatcher.Matches(kvp.Key, incomingTopic!)).ToList();

        if (matchingHandlers.Count == 0)
        {
            _logger.LogWarning("No handlers found for topic: {Topic}", incomingTopic);
            return;
        }
        
        foreach (var (_, handlerInfo) in matchingHandlers)
        {
            using var scope = _serviceProvider.CreateScope();
            try
            {
                var handler = (IMqttMessageHandler)ActivatorUtilities.CreateInstance(
                    scope.ServiceProvider, handlerInfo.HandlerType);

                if (handlerInfo.MessageType != null)
                {
                    var payloadString = args.PublishMessage.PayloadAsString;
                    
                    try
                    {
                        var message = JsonSerializer.Deserialize(payloadString, handlerInfo.MessageType, _jsonOptions);

                        if (message == null)
                        {
                            _logger.LogWarning("Failed to deserialize message for topic {Topic} - null result", incomingTopic);
                            continue;
                        }

                        if (handlerInfo.Validator != null)
                        {
                            var validationContext = Activator.CreateInstance(typeof(ValidationContext<>).MakeGenericType(handlerInfo.MessageType), message);
                            
                            var validationResult = await handlerInfo.Validator.ValidateAsync((IValidationContext) validationContext!);

                            if (!validationResult.IsValid)
                            {
                                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                                _logger.LogWarning("Validation failed for message on topic {Topic}: {Errors}", incomingTopic, errors);
                                continue;
                            }
                        }

                        await ((dynamic)handler).HandleAsync((dynamic)message, incomingTopic);
                        _logger.LogDebug("Successfully handled message for topic {Topic} with handler {HandlerType}", incomingTopic, handlerInfo.HandlerType.Name);
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogError(ex, "Failed to deserialize message for topic {Topic}", incomingTopic);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error calling generic handler for topic {Topic}", incomingTopic);
                    }
                }
                else
                {
                    await handler.HandleAsync(args);
                    _logger.LogDebug("Successfully handled message for topic {Topic} with non-generic handler {HandlerType}",
                        incomingTopic, handlerInfo.HandlerType.Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing MQTT message for topic {Topic} with handler {HandlerType}",
                    incomingTopic, handlerInfo.HandlerType.Name);
            }
        }
    }
}