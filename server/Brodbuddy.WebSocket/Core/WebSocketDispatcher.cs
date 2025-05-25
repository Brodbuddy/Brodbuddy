using System.Reflection;
using System.Text.Json;
using Brodbuddy.WebSocket.Auth;
using Brodbuddy.WebSocket.State;
using Fleck;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Brodbuddy.WebSocket.Core;

public class WebSocketDispatcher(IServiceProvider serviceProvider,
                                 ISocketManager manager,
                                 IEnumerable<IWebSocketMiddleware> middlewares,
                                 IWebSocketAuthHandler? authHandler,
                                 IWebSocketExceptionHandler exceptionHandler,
                                 ILogger<WebSocketDispatcher> logger,
                                 AuthPolicy authPolicy = AuthPolicy.Blacklist)
{
    private readonly Dictionary<string, (Type HandlerType, IValidator? Validator, AuthorizeAttribute? AuthAttribute, bool HasAllowAnonymous)> _registry = new();
    
    public void RegisterHandlers(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var handlerTypes = assembly.GetTypes()
                                   .Where(t => t is { IsAbstract: false, IsInterface: false })
                                   .Where(t => t.GetInterfaces()
                                                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IWebSocketHandler<,>)));
        
        foreach (var handlerType in handlerTypes)
        {
            if (ActivatorUtilities.CreateInstance(serviceProvider, handlerType) is not IWebSocketHandler handler) continue;
            
            var handlerInterface = handlerType.GetInterfaces().First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IWebSocketHandler<,>));
            var requestType = handlerInterface.GetGenericArguments()[0];
            
            var validatorType = assembly.GetTypes()
                                        .FirstOrDefault(t => t.IsClass && t is { IsAbstract: false, BaseType.IsGenericType: true } && 
                                                             t.BaseType.GetGenericTypeDefinition() == typeof(AbstractValidator<>) &&
                                                             t.BaseType.GetGenericArguments()[0] == requestType);
                                                        
            var validator = validatorType != null ? ActivatorUtilities.CreateInstance(serviceProvider, validatorType) as IValidator : null;
            
            var authAttribute = handlerType.GetCustomAttribute<AuthorizeAttribute>();
            var hasAllowAnonymous = handlerType.GetCustomAttribute<AllowAnonymousAttribute>() != null;
            
            _registry[handler.MessageType.ToLowerInvariant()] = (handlerType, validator, authAttribute, hasAllowAnonymous);
            logger.LogInformation("Registered handler {HandlerType} for message type {MessageType}", handlerType.Name, handler.MessageType.ToLowerInvariant());
        }
    }
    
    public async Task DispatchAsync(IWebSocketConnection socket, string message)
    {
        try
        {
            // Parse
            var options = new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip
            };
            
            var root = JsonDocument.Parse(message, options).RootElement;

            var typeProperty = GetPropertyCaseInsensitive(root, "Type");
            var payloadProperty = GetPropertyCaseInsensitive(root, "Payload");
            var requestId = GetPropertyCaseInsensitive(root, "RequestId")?.GetString() ?? "NO_REQUEST_ID";
            var token = GetPropertyCaseInsensitive(root, "Token")?.GetString();

            // Validate
            (bool foundId, string? clientId) = await manager.TryGetClientIdAsync(socket);
            if (!foundId || clientId == null)
            {
                logger.LogError("Dispatch failed: Could not find ClientId for Socket {SocketId}", socket.ConnectionInfo.Id);
                await SendError(socket, new WebSocketError(WebSocketErrorCodes.ConnectionError, "Client session not found."), requestId);
                return;
            }
            
            var messageType = typeProperty?.GetString();
            if (string.IsNullOrEmpty(messageType))
            {
                logger.LogWarning("Received message without Type field");
                await SendError(socket, new WebSocketError(WebSocketErrorCodes.InvalidMessage,"Missing message type: The 'Type' field is required"), requestId);
                return;
            }
            
            if (!_registry.TryGetValue(messageType.ToLowerInvariant(), out var registration))
            {
                logger.LogWarning("No handler registered for message types: {MessageType}", messageType);
                await SendError(socket, new WebSocketError(WebSocketErrorCodes.UnknownMessage, $"No handler for message type: {messageType}"), requestId);
                return;
            }
            
            var payload = payloadProperty?.GetRawText();
            if (payload == null)
            {
                logger.LogWarning("Received message without Payload field");
                await SendError(socket, new WebSocketError(WebSocketErrorCodes.InvalidMessage, "Missing payload"), requestId);
                return;
            }
            
            var requestType = registration.HandlerType.GetInterfaces()
                                                      .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IWebSocketHandler<,>))
                                                      .GetGenericArguments()[0];
            var request = JsonSerializer.Deserialize(payload, requestType);
            
            if (registration.Validator != null)
            {
                var context = new ValidationContext<object>(request!);
                var validationResult = await registration.Validator.ValidateAsync(context);
                if (!validationResult.IsValid) {
                    var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)); 
                    logger.LogInformation("Validation failed for message type {MessageType}: {Errors}", messageType, errors);
                    await SendError(socket, new WebSocketError(WebSocketErrorCodes.ValidationError, errors), requestId);
                    return;
                }
            }

            // Auth
            bool requiresAuth = authPolicy == AuthPolicy.Blacklist;
            bool hasAuthAttribute = registration.AuthAttribute != null;
            bool hasAllowAnonymous = registration.HasAllowAnonymous;
            
            if (authHandler != null && ((requiresAuth && !hasAllowAnonymous) || (!requiresAuth && hasAuthAttribute)))
            {
                var authResult = await authHandler.AuthenticateAsync(socket, token, messageType);

                if (!authResult.IsAuthenticated)
                {
                    logger.LogWarning("Unauthorized access attempt for message type {MessageType}", messageType);
                    await SendError(socket, new WebSocketError(WebSocketErrorCodes.Unauthorized, "Authentication required"), requestId);
                    return;
                }

                if (hasAuthAttribute && registration.AuthAttribute!.GetRolesAsArray() is { Length: > 0 } requiredRoles && !authResult.HasAnyRole(requiredRoles))
                {
                    logger.LogWarning("Access denied due to insufficient roles for message type {MessageType}", messageType);
                    await SendError(socket, new WebSocketError(WebSocketErrorCodes.Forbidden, "You don't have the required permissions to perform this action"), requestId);
                    return;
                }
            }

            // Middleware pipeline
            var middlewareContext = new MiddlewareContext
            {
                Socket = socket,
                Message = message,
                MessageType = messageType,
                Request = request,
                ClientId = clientId,
                RequestId = requestId,
                Registration = registration
            };

            if (!await ExecutePipelineAsync(middlewareContext)) return;
            
            var response = middlewareContext.Response;
            var topicKey = middlewareContext.TopicKey;
            
            // Send response
            await socket.Send(JsonSerializer.Serialize(new
            {
                Type = response?.GetType().Name,
                RequestId = requestId,
                TopicKey = topicKey,
                Payload = response,
            }));
        }
        catch (Exception ex)
        {
            await SendError(socket, exceptionHandler.HandleException(ex), "REQUEST_ID_NOT_AVAILABLE");
        }
    }
    
    private static async Task SendError(IWebSocketConnection socket, WebSocketError error, string requestId)
    {
        await socket.Send(JsonSerializer.Serialize(new
        {
            Type = "Error",
            RequestId = requestId,
            Payload = error,
        }));
    }
    
    private static JsonElement? GetPropertyCaseInsensitive(JsonElement element, string propertyName)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (property.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase)) return property.Value;
        }
        return null;
    }
    
    private async Task<bool> ExecutePipelineAsync(MiddlewareContext context)
    {
        var pipeline = middlewares.Reverse().Aggregate((Func<MiddlewareContext, Task<bool>>)(async ctx => 
            {
                using var scope = serviceProvider.CreateScope();
                dynamic handler = ActivatorUtilities.CreateInstance(scope.ServiceProvider, ctx.Registration.HandlerType);
                ctx.Response = await handler.HandleAsync((dynamic)ctx.Request, ctx.ClientId, ctx.Socket);

                if (ctx.Registration.HandlerType.GetInterfaces().Any(i => i.IsGenericType && 
                                                                          (i.GetGenericTypeDefinition() == typeof(ISubscriptionHandler<,>) || 
                                                                           i.GetGenericTypeDefinition() == typeof(IUnsubscriptionHandler<,>))))
                {
                    var method = ctx.Registration.HandlerType.GetMethod("GetTopicKey");
                    if (method != null)
                    {
                        ctx.TopicKey = (string)method.Invoke(handler, new[] { ctx.Request!, ctx.ClientId });
                    }
                }
                
                return true;
            }), (next, middleware) => async ctx => await middleware.InvokeAsync(ctx.Socket, ctx.Message, () => next(ctx))
        );

        var success = await pipeline(context);
        return success;
    }
}