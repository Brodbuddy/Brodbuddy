using System.Reflection;
using System.Text.Json;
using Api.Mqtt.Routing;
using HiveMQtt.Client.Events;
using HiveMQtt.MQTT5.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Api.Mqtt.Core;

public class MqttDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MqttDispatcher> _logger;
    private readonly Dictionary<string, (Type HandlerType, Type MessageType)> _handlerRegistry = new();
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
    
    public MqttDispatcher(IServiceProvider serviceProvider, ILogger<MqttDispatcher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public void RegisterHandlers(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        
        using var scope = _serviceProvider.CreateScope();

        foreach (var handlerType in HandlerTypeHelpers.GetMqttMessageHandlers(assembly))
        {
            try
            {
                var handlerInterface = handlerType.GetInterfaces()
                    .First(i => i.IsGenericType && 
                                i.GetGenericTypeDefinition() == typeof(IMqttMessageHandler<>));
                
                var messageType = handlerInterface.GetGenericArguments()[0];
                
                var handler = ActivatorUtilities.CreateInstance(scope.ServiceProvider, handlerType);
                var topicFilter = ((dynamic)handler).TopicFilter as string;
                
                if (string.IsNullOrEmpty(topicFilter))
                {
                    _logger.LogWarning("Handler {HandlerType} has null or empty TopicFilter", handlerType.Name);
                    continue;
                }
                
                _handlerRegistry[topicFilter] = (handlerType, messageType);
                
                _logger.LogDebug("Registered handler {HandlerType} for topic {TopicFilter}", handlerType.Name, topicFilter);
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
        
        _logger.LogInformation("Raw MQTT message received on topic: {Topic}, payload: {Payload}", 
            args.PublishMessage.Topic, args.PublishMessage.PayloadAsString);
        
        ArgumentNullException.ThrowIfNull(args);
        
        var topic = args.PublishMessage.Topic;
        
        if (string.IsNullOrEmpty(topic))
        {
            _logger.LogWarning("Received MQTT message with null or empty topic");
            return;
        }
        
        _logger.LogDebug("Received MQTT message on topic: {Topic}", topic);
        
        var matchingHandlers = _handlerRegistry.Where(kvp => MqttTopicMatcher.Matches(kvp.Key, topic))
                                               .ToList();
            
        if (matchingHandlers.Count == 0)
        {
            _logger.LogWarning("No handlers found for topic: {Topic}", topic);
            return;
        }
        
        foreach (var (_, handlerInfo) in matchingHandlers)
        {
            using var scope = _serviceProvider.CreateScope();
            try
            {
                
                var message = JsonSerializer.Deserialize(args.PublishMessage.PayloadAsString, handlerInfo.MessageType, _jsonOptions);
                
                if (message == null)
                {
                    _logger.LogWarning("Failed to deserialize message for topic {Topic}", topic);
                    continue;
                }
                
                var handler = ActivatorUtilities.CreateInstance(scope.ServiceProvider, handlerInfo.HandlerType);
                await ((dynamic) handler).HandleAsync((dynamic) message, args);
                _logger.LogDebug("Successfully handled message for topic {Topic} with handler {HandlerType}", topic, handlerInfo.HandlerType.Name);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize message for topic {Topic}", topic);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message for topic {Topic} with handler {HandlerType}", topic, handlerInfo.HandlerType.Name);
            }
        }
    }
}