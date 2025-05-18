using System.Reflection;
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
        
        var enums = CollectEnums(assembly);
        
        return new WebSocketSpec(
            Version: "1.0",
            RequestTypes: requestTypes,
            ResponseTypes: responseTypes,
            BroadcastTypes: broadcastTypes,
            ErrorCodes: errorCodes,
            SubscriptionMethods: subscriptionMethods, 
            UnsubscriptionMethods: unsubscriptionMethods,
            Types: types,
            RequestResponses: requestResponses,
            enums
        );
    } 
    
    internal static TypeDefinition GenerateTypeDefinition(Type type)
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
    
    private static string GetTypeScriptType(Type type)
    {
        // Håndter nullable typer
        if (IsNullableType(type))
        {
            var underlyingType = Nullable.GetUnderlyingType(type);
            return GetTypeScriptType(underlyingType!) + " | null";
        }
        
        // Håndter enums
        if (type.IsEnum)
        {
            return type.Name;
        }
        
        // Håndter arrays 
        if (type.IsArray)
        {
            var elementType = type.GetElementType()!;
            return GetTypeScriptType(elementType) + "[]";
        }
        
        // Håndter generiske collections 
        if (type.IsGenericType)
        {
            var genericTypeDefinition = type.GetGenericTypeDefinition();
            var genericArguments = type.GetGenericArguments();
            
            // List<T>, IList<T>, ICollection<T>, IEnumerable<T> -> T[]
            if (genericTypeDefinition == typeof(List<>) ||
                genericTypeDefinition == typeof(IList<>) ||
                genericTypeDefinition == typeof(ICollection<>) ||
                genericTypeDefinition == typeof(IEnumerable<>))
            {
                return GetTypeScriptType(genericArguments[0]) + "[]";
            }
            
            // Dictionary<string, T> -> Record<string, T>
            if (genericTypeDefinition == typeof(Dictionary<,>) ||
                genericTypeDefinition == typeof(IDictionary<,>))
            {
                var keyType = genericArguments[0];
                var valueType = genericArguments[1];
                
                // Only support string keys for Record type
                // Understøt kun string keys for Record typer
                if (keyType == typeof(string)) return $"Record<string, {GetTypeScriptType(valueType)}>";
                
                // For ikke-string keys, fald tilbage til objekt
                return "Record<string, any>";
            }
            
            // KeyValuePair<K, V> -> { key: K; value: V }
            if (genericTypeDefinition == typeof(KeyValuePair<,>))
            {
                var keyType = GetTypeScriptType(genericArguments[0]);
                var valueType = GetTypeScriptType(genericArguments[1]);
                return $"{{ key: {keyType}; value: {valueType} }}";
            }
        }
        
        // Primitive typer
        return type switch
        {
            // String typer 
            not null when type == typeof(string) => "string",
            not null when type == typeof(char) => "string",
            
            // Numeriske typer 
            not null when type == typeof(byte) => "number",
            not null when type == typeof(sbyte) => "number",
            not null when type == typeof(short) => "number",
            not null when type == typeof(ushort) => "number",
            not null when type == typeof(int) => "number",
            not null when type == typeof(uint) => "number",
            not null when type == typeof(long) => "number",
            not null when type == typeof(ulong) => "number",
            not null when type == typeof(float) => "number",
            not null when type == typeof(double) => "number",
            not null when type == typeof(decimal) => "number",
            
            // Boolean
            not null when type == typeof(bool) => "boolean",
            
            // Date/Time typer (typisk serialiseret som ISO strings)
            not null when type == typeof(DateTime) => "string",
            not null when type == typeof(DateTimeOffset) => "string",
            not null when type == typeof(DateOnly) => "string",
            not null when type == typeof(TimeOnly) => "string",
            not null when type == typeof(TimeSpan) => "string",
            
            // GUID og andre specielle typer 
            not null when type == typeof(Guid) => "string",
            not null when type == typeof(Uri) => "string",
    
            // Default
            _ => "any"
        };
    }
    
    private static Dictionary<string, EnumDefinition> CollectEnums(Assembly assembly)
    {
        var enums = new Dictionary<string, EnumDefinition>();
    
        var enumTypes = assembly.GetTypes().Where(t => t.IsEnum);
    
        foreach (var enumType in enumTypes)
        {
            var enumValues = new Dictionary<string, object>();
            var names = Enum.GetNames(enumType);
            var values = Enum.GetValues(enumType);
        
            for (int i = 0; i < names.Length; i++)
            {
                var name = names[i];
                var value = values.GetValue(i);
                enumValues[name] = value!;
            }
        
            enums[enumType.Name] = new EnumDefinition(enumValues);
        }
    
        return enums;
    }

    
    private static bool IsNullableType(Type type)
    {
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
    }
    
    private static string ToCamelCase(string str) => char.ToLowerInvariant(str[0]) + str[1..];
}