using System.Reflection;
using Api.Websocket.Auth;
using Brodbuddy.WebSocket.Core;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Websocket.Spec;

public static class SpecGenerator
{
    public static WebSocketSpec GenerateSpec(Assembly assembly, IServiceProvider serviceProvider)
    {
        var requestTypes = new Dictionary<string, string>();
        var responseTypes = new Dictionary<string, string>();
        var broadcastTypes = new Dictionary<string, string>();
        var errorCodes = new Dictionary<string, string>();
        var subscriptionMethods = new Dictionary<string, string>();
        var unsubscriptionMethods = new Dictionary<string, string>();
        var types = new Dictionary<string, TypeDefinition>();
        var requestResponses = new Dictionary<string, RequestResponseMapping>();
        
        var handlers = assembly.GetTypes().Where(t => t is { IsAbstract: false, IsInterface: false })
                                                          .Where(t => t.GetInterfaces()
                                                                            .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IWebSocketHandler<,>)));
        
        foreach (var handlerType in handlers)
        {
            if (ActivatorUtilities.CreateInstance(serviceProvider, handlerType) is not IWebSocketHandler handler) continue;
            var handlerInterface = handlerType.GetInterfaces().First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IWebSocketHandler<,>));
            
            var requestType = handlerInterface.GetGenericArguments()[0];
            var responseType = handlerInterface.GetGenericArguments()[1];
            
            requestTypes[ToCamelCase(handler.MessageType)] = handler.MessageType;
            responseTypes[ToCamelCase(responseType.Name)] = responseType.Name;
            
            types[requestType.Name] = GenerateTypeDefinition(requestType);
            types[responseType.Name] = GenerateTypeDefinition(responseType);
            
            requestResponses[handler.MessageType] = new RequestResponseMapping(
                RequestType: requestType.Name,
                ResponseType: responseType.Name,
                Validation: GenerateValidationDefinition(requestType, assembly)
            );
            
            var subscriptionInterface = handlerType.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISubscriptionHandler<,>));
            if (subscriptionInterface != null) subscriptionMethods[ToCamelCase(handler.MessageType)] = handler.MessageType;
            
            var unsubscriptionInterface = handlerType.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IUnsubscriptionHandler<,>));
            if (unsubscriptionInterface != null) unsubscriptionMethods[ToCamelCase(handler.MessageType)] = handler.MessageType;
        }

        var broadcastMessages = assembly.GetTypes().Where(t => typeof(IBroadcastMessage).IsAssignableFrom(t) && !t.IsInterface); 
        foreach (var broadcastMessage in broadcastMessages) 
        {
            broadcastTypes[ToCamelCase(broadcastMessage.Name)] = broadcastMessage.Name;
            types[broadcastMessage.Name] = GenerateTypeDefinition(broadcastMessage);
        }
        
        var errorCodesType = typeof(WebSocketErrorCodes);
        var constantFields = errorCodesType.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(fi => fi.IsLiteral && !fi.IsInitOnly && fi.FieldType == typeof(string));
    
        foreach (var field in constantFields)
        {
            var value = (string)field.GetValue(null)!;
            var name = ToCamelCase(field.Name);
            errorCodes[name] = value;
        }
        
        return new WebSocketSpec(
            Version: "1.0",
            RequestTypes: requestTypes,
            ResponseTypes: responseTypes,
            BroadcastTypes: broadcastTypes,
            ErrorCodes: errorCodes,
            SubscriptionMethods: subscriptionMethods, 
            UnsubscriptionMethods: unsubscriptionMethods,
            Types: types,
            RequestResponses: requestResponses
        );
    } 
    
    private static TypeDefinition GenerateTypeDefinition(Type type)
    {
        var properties = type.GetProperties().ToDictionary(
            p => p.Name,
            p => new PropertyDefinition(
                Type: GetTypeScriptType(p.PropertyType),
                IsRequired: !IsNullableType(p.PropertyType)
            ));
        
        return new TypeDefinition(
            Kind: "record",
            Properties: properties
        );
    }
    
    private static ValidationDefinition? GenerateValidationDefinition(Type requestType, Assembly assembly)
    {
        var validatorType = assembly.GetTypes().FirstOrDefault(t => t.IsClass && t is { IsAbstract: false, BaseType.IsGenericType: true } &&
                                                                    t.BaseType.GetGenericTypeDefinition() == typeof(AbstractValidator<>) &&
                                                                    t.BaseType.GetGenericArguments()[0] == requestType);
        if (validatorType == null) return null;

        // to do: Extract FluentValidation rules
        return new ValidationDefinition(
            Rules: new Dictionary<string, List<ValidationRule>>()
        );
    }
    
    private static string GetTypeScriptType(Type type) => type switch
    {
        not null when type == typeof(string) => "string",
        not null when type == typeof(int) || type == typeof(double) || type == typeof(float) => "number",
        not null when type == typeof(bool) => "boolean",
        not null when type == typeof(Guid) => "string",
        _ => "any"
    };
    
    private static bool IsNullableType(Type type)
    {
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
    }
    
    private static string ToCamelCase(string str) => char.ToLowerInvariant(str[0]) + str[1..];
}