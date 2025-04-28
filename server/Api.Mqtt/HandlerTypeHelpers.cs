using System.Reflection;

namespace Api.Mqtt;

public static class HandlerTypeHelpers
{
    private static List<Type>? _cachedHandlerTypes;
    
    public static List<Type> GetMqttMessageHandlers(Assembly assembly)
    {
        if (_cachedHandlerTypes != null) return _cachedHandlerTypes;
            
        _cachedHandlerTypes = assembly.GetTypes()
                                      .Where(IsGenericMqttMessageHandler)
                                      .ToList();
            
        return _cachedHandlerTypes;
    }

    public static bool IsGenericMqttMessageHandler(Type type)
    {
        if (!type.IsClass || type.IsAbstract) return false;

        foreach (var interfaceType in type.GetInterfaces())
        {
            if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IMqttMessageHandler<>)) return true;
        }

        return false;
    }
}